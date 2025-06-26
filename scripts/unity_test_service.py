#!/usr/bin/env python3
"""
Unity Test Service
A Flask-based service that handles Unity test requests from GitHub Actions.
Clones repositories, runs Unity tests, and reports results.
"""

import os
import sys
import json
import uuid
import time
import threading
import subprocess
import shutil
from datetime import datetime, timedelta
from pathlib import Path
from flask import Flask, request, jsonify
from flask_cors import CORS
import xml.etree.ElementTree as ET

app = Flask(__name__)
CORS(app)

# Configuration
SERVICE_PORT = 5001
WORK_DIR = os.path.expanduser("~/unity_test_workspace")
MAX_CONCURRENT_TESTS = 3
TEST_TIMEOUT_MINUTES = 10

# Global state
active_tests = {}
test_queue = []
running_tests = 0

class TestStatus:
    QUEUED = "queued"
    RUNNING = "running" 
    COMPLETED = "completed"
    FAILED = "failed"
    TIMEOUT = "timeout"

class TestResult:
    def __init__(self, test_id):
        self.test_id = test_id
        self.status = TestStatus.QUEUED
        self.start_time = None
        self.end_time = None
        self.total_tests = 0
        self.passed_tests = 0
        self.failed_tests = 0
        self.success = False
        self.error_message = None
        self.log_output = []
        self.workspace_path = None

def log_message(test_result, message):
    """Add a timestamped log message to test result"""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"[{timestamp}] {message}"
    test_result.log_output.append(log_entry)
    print(f"[{test_result.test_id}] {message}")

def cleanup_workspace(workspace_path):
    """Clean up test workspace directory"""
    try:
        if os.path.exists(workspace_path):
            shutil.rmtree(workspace_path)
            print(f"Cleaned up workspace: {workspace_path}")
    except Exception as e:
        print(f"Error cleaning up workspace {workspace_path}: {e}")

def clone_repository(test_result, repo_info):
    """Clone the repository at specified commit/branch"""
    try:
        repo_url = f"https://github.com/{repo_info['repository']}.git"
        workspace_path = os.path.join(WORK_DIR, test_result.test_id)
        
        log_message(test_result, f"Cloning repository: {repo_url}")
        log_message(test_result, f"Target branch/commit: {repo_info['branch']} ({repo_info['commit_sha'][:8]})")
        
        # Create workspace directory
        os.makedirs(workspace_path, exist_ok=True)
        test_result.workspace_path = workspace_path
        
        # Clone the full repository (needed to access all commits, including PR merge commits)
        clone_cmd = [
            "git", "clone", 
            repo_url, workspace_path
        ]
        
        log_message(test_result, "Cloning full repository to access specific commit")
        result = subprocess.run(
            clone_cmd, 
            capture_output=True, 
            text=True, 
            timeout=300  # 5 minute timeout for clone
        )
        
        if result.returncode != 0:
            raise Exception(f"Git clone failed: {result.stderr}")
        
        # Change to repository directory
        original_cwd = os.getcwd()
        os.chdir(workspace_path)
        
        try:
            # STRICT: Must checkout the exact commit - this is critical for PR testing
            log_message(test_result, f"STRICT: Checking out exact commit {repo_info['commit_sha'][:8]} (no fallbacks)")
            
            checkout_result = subprocess.run(
                ["git", "checkout", repo_info['commit_sha']],
                capture_output=True,
                text=True,
                timeout=60
            )
            
            if checkout_result.returncode != 0:
                # If direct checkout fails, this might be a GitHub Actions merge commit
                # Detach HEAD first to avoid fetch conflicts
                log_message(test_result, f"Direct checkout failed, detaching HEAD and fetching PR refs for commit {repo_info['commit_sha'][:8]}")
                
                # Detach HEAD to avoid "refusing to fetch into branch" error
                detach_result = subprocess.run(
                    ["git", "checkout", "--detach", "HEAD"],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                if detach_result.returncode != 0:
                    log_message(test_result, f"Warning: Could not detach HEAD: {detach_result.stderr}")
                
                # Fetch all PR refs to get GitHub Actions merge commits
                fetch_result = subprocess.run(
                    ["git", "fetch", "origin", "+refs/pull/*/merge:refs/remotes/origin/pr/*/merge"],
                    capture_output=True,
                    text=True,
                    timeout=120
                )
                
                if fetch_result.returncode == 0:
                    log_message(test_result, "Successfully fetched PR merge refs")
                    
                    # Try checkout again after fetching PR refs
                    checkout_retry = subprocess.run(
                        ["git", "checkout", repo_info['commit_sha']],
                        capture_output=True,
                        text=True,
                        timeout=60
                    )
                    
                    if checkout_retry.returncode != 0:
                        # Final attempt: fetch all pull request refs
                        log_message(test_result, "PR merge refs checkout failed, fetching all PR refs")
                        
                        fetch_all_prs = subprocess.run(
                            ["git", "fetch", "origin", "+refs/pull/*:refs/remotes/origin/pull/*"],
                            capture_output=True,
                            text=True,
                            timeout=180
                        )
                        
                        if fetch_all_prs.returncode == 0:
                            final_checkout = subprocess.run(
                                ["git", "checkout", repo_info['commit_sha']],
                                capture_output=True,
                                text=True,
                                timeout=60
                            )
                            
                            if final_checkout.returncode != 0:
                                error_msg = f"FAILED: Cannot checkout required commit {repo_info['commit_sha'][:8]} after all fetch attempts. Error: {final_checkout.stderr.strip()}"
                                log_message(test_result, error_msg)
                                raise Exception(error_msg)
                        else:
                            error_msg = f"FAILED: Cannot fetch PR refs for commit {repo_info['commit_sha'][:8]}. Error: {fetch_all_prs.stderr.strip()}"
                            log_message(test_result, error_msg)
                            raise Exception(error_msg)
                else:
                    error_msg = f"FAILED: Cannot fetch PR merge refs for commit {repo_info['commit_sha'][:8]}. Error: {fetch_result.stderr.strip()}"
                    log_message(test_result, error_msg)
                    raise Exception(error_msg)
            
            log_message(test_result, f"SUCCESS: Checked out exact commit {repo_info['commit_sha'][:8]}")
            
            # Verify we're on the correct commit
            verify_result = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if verify_result.returncode == 0:
                current_commit = verify_result.stdout.strip()
                if current_commit.startswith(repo_info['commit_sha']):
                    log_message(test_result, f"VERIFIED: On correct commit {current_commit[:8]}")
                else:
                    error_msg = f"VERIFICATION FAILED: Expected {repo_info['commit_sha'][:8]} but got {current_commit[:8]}"
                    log_message(test_result, error_msg)
                    raise Exception(error_msg)
            
        finally:
            os.chdir(original_cwd)
        
        log_message(test_result, "Repository cloned successfully")
        return True
        
    except Exception as e:
        log_message(test_result, f"Failed to clone repository: {str(e)}")
        return False

def run_unity_tests(test_result):
    """Run Unity tests using the unity.py script"""
    try:
        if not test_result.workspace_path:
            raise Exception("No workspace path available")
        
        unity_script = os.path.join(test_result.workspace_path, "scripts", "unity.py")
        
        if not os.path.exists(unity_script):
            raise Exception(f"Unity script not found at: {unity_script}")
        
        log_message(test_result, "Starting Unity tests...")
        
        # Change to workspace directory
        original_cwd = os.getcwd()
        os.chdir(test_result.workspace_path)
        
        try:
            # Run the unity.py script
            env = os.environ.copy()
            env['MONGO_URI'] = os.getenv('MONGO_URI', 'mongodb://localhost:27017/superlap')
            env['PORT'] = '3000'
            
            result = subprocess.run(
                [sys.executable, unity_script],
                capture_output=True,
                text=True,
                timeout=TEST_TIMEOUT_MINUTES * 60,  # Convert to seconds
                env=env
            )
            
            # Capture output
            if result.stdout:
                for line in result.stdout.split('\n'):
                    if line.strip():
                        log_message(test_result, f"STDOUT: {line}")
            
            if result.stderr:
                for line in result.stderr.split('\n'):
                    if line.strip():
                        log_message(test_result, f"STDERR: {line}")
            
            # Parse test results
            parse_test_results_xml(test_result)
            
            if result.returncode == 0:
                log_message(test_result, "Unity tests completed successfully")
                test_result.success = test_result.failed_tests == 0
            else:
                log_message(test_result, f"Unity tests failed with return code: {result.returncode}")
                test_result.success = False
                
        finally:
            os.chdir(original_cwd)
            
    except subprocess.TimeoutExpired:
        log_message(test_result, f"Unity tests timed out after {TEST_TIMEOUT_MINUTES} minutes")
        test_result.status = TestStatus.TIMEOUT
        test_result.success = False
    except Exception as e:
        log_message(test_result, f"Error running Unity tests: {str(e)}")
        test_result.error_message = str(e)
        test_result.success = False

def parse_test_results_xml(test_result):
    """Parse Unity test results from XML file"""
    try:
        results_file = os.path.join(test_result.workspace_path, "Unity", "TestResults", "results.xml")
        
        if not os.path.exists(results_file):
            log_message(test_result, "No test results XML file found")
            return
        
        tree = ET.parse(results_file)
        root = tree.getroot()
        
        test_result.total_tests = int(root.get('total', 0))
        test_result.passed_tests = int(root.get('passed', 0))
        test_result.failed_tests = int(root.get('failed', 0))
        
        log_message(test_result, f"Test results: {test_result.total_tests} total, {test_result.passed_tests} passed, {test_result.failed_tests} failed")
        
        # Log failed tests
        if test_result.failed_tests > 0:
            failed_test_cases = root.findall(".//test-case[@result='Failed']")
            for test_case in failed_test_cases:
                test_name = test_case.get('name', 'Unknown')
                class_name = test_case.get('classname', 'Unknown')
                log_message(test_result, f"FAILED: {class_name}.{test_name}")
                
                failure_element = test_case.find('failure')
                if failure_element is not None:
                    message_element = failure_element.find('message')
                    if message_element is not None and message_element.text:
                        failure_msg = message_element.text.strip()
                        log_message(test_result, f"  Error: {failure_msg}")
        
    except Exception as e:
        log_message(test_result, f"Error parsing test results: {str(e)}")

def execute_test(test_result, repo_info):
    """Execute a Unity test job"""
    global running_tests
    
    try:
        running_tests += 1
        test_result.status = TestStatus.RUNNING
        test_result.start_time = datetime.now()
        
        log_message(test_result, "Starting Unity test execution")
        
        # Clone repository
        if not clone_repository(test_result, repo_info):
            test_result.status = TestStatus.FAILED
            test_result.error_message = "Failed to clone repository"
            return
        
        # Run Unity tests
        run_unity_tests(test_result)
        
        # Set final status
        if test_result.status != TestStatus.TIMEOUT:
            test_result.status = TestStatus.COMPLETED
        
    except Exception as e:
        log_message(test_result, f"Unexpected error: {str(e)}")
        test_result.status = TestStatus.FAILED
        test_result.error_message = str(e)
    finally:
        test_result.end_time = datetime.now()
        running_tests -= 1
        
        # Clean up workspace after a delay
        if test_result.workspace_path:
            def cleanup_later():
                time.sleep(300)  # Wait 5 minutes before cleanup
                cleanup_workspace(test_result.workspace_path)
            
            threading.Thread(target=cleanup_later, daemon=True).start()
        
        log_message(test_result, f"Test execution completed with status: {test_result.status}")

def process_test_queue():
    """Process queued tests when resources are available"""
    global running_tests, test_queue
    
    while True:
        try:
            if running_tests < MAX_CONCURRENT_TESTS and test_queue:
                test_id, repo_info = test_queue.pop(0)
                
                if test_id in active_tests:
                    test_result = active_tests[test_id]
                    thread = threading.Thread(
                        target=execute_test,
                        args=(test_result, repo_info),
                        daemon=True
                    )
                    thread.start()
            
            time.sleep(1)  # Check every second
            
        except Exception as e:
            print(f"Error in test queue processor: {e}")
            time.sleep(5)

# Start the test queue processor
queue_processor = threading.Thread(target=process_test_queue, daemon=True)
queue_processor.start()

@app.route('/api/run-tests', methods=['POST'])
def run_tests():
    """Start a new Unity test run"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({'error': 'No JSON data provided'}), 400
        
        # Validate required fields
        required_fields = ['repository', 'branch', 'commit_sha']
        for field in required_fields:
            if field not in data:
                return jsonify({'error': f'Missing required field: {field}'}), 400
        
        # Generate unique test ID
        test_id = str(uuid.uuid4())
        
        # Create test result
        test_result = TestResult(test_id)
        active_tests[test_id] = test_result
        
        # Add to queue
        test_queue.append((test_id, data))
        
        log_message(test_result, f"Test queued for repository: {data['repository']}")
        log_message(test_result, f"Branch: {data['branch']}, Commit: {data['commit_sha'][:8]}")
        
        return jsonify({
            'testId': test_id,
            'status': TestStatus.QUEUED,
            'message': 'Test queued successfully'
        })
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/test-status/<test_id>', methods=['GET'])
def get_test_status(test_id):
    """Get the status of a test run"""
    try:
        if test_id not in active_tests:
            return jsonify({'error': 'Test not found'}), 404
        
        test_result = active_tests[test_id]
        
        response = {
            'testId': test_id,
            'status': test_result.status,
            'totalTests': test_result.total_tests,
            'passedTests': test_result.passed_tests,
            'failedTests': test_result.failed_tests,
            'success': test_result.success,
            'startTime': test_result.start_time.isoformat() if test_result.start_time else None,
            'endTime': test_result.end_time.isoformat() if test_result.end_time else None,
            'logs': test_result.log_output[-20:]  # Return last 20 log entries
        }
        
        if test_result.error_message:
            response['error'] = test_result.error_message
        
        # Clean up completed tests after 1 hour
        if (test_result.status in [TestStatus.COMPLETED, TestStatus.FAILED, TestStatus.TIMEOUT] and 
            test_result.end_time and 
            datetime.now() - test_result.end_time > timedelta(hours=1)):
            del active_tests[test_id]
        
        return jsonify(response)
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'activeTests': len(active_tests),
        'runningTests': running_tests,
        'queuedTests': len(test_queue),
        'timestamp': datetime.now().isoformat()
    })

@app.route('/api/logs/<test_id>', methods=['GET'])
def get_test_logs(test_id):
    """Get detailed logs for a test run"""
    try:
        if test_id not in active_tests:
            return jsonify({'error': 'Test not found'}), 404
        
        test_result = active_tests[test_id]
        
        return jsonify({
            'testId': test_id,
            'logs': test_result.log_output
        })
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    # Create work directory
    os.makedirs(WORK_DIR, exist_ok=True)
    
    print(f"Unity Test Service starting on port {SERVICE_PORT}")
    print(f"Work directory: {WORK_DIR}")
    print(f"Max concurrent tests: {MAX_CONCURRENT_TESTS}")
    print(f"Test timeout: {TEST_TIMEOUT_MINUTES} minutes")
    
    app.run(host='0.0.0.0', port=SERVICE_PORT, debug=False, threaded=True)
