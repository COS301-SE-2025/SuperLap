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
        # Coverage data
        self.coverage_available = False
        self.line_coverage = 0.0
        self.method_coverage = 0.0
        self.test_class_count = 0

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
            # Force remove with retries for Windows compatibility
            max_retries = 3
            for attempt in range(max_retries):
                try:
                    shutil.rmtree(workspace_path)
                    print(f"Cleaned up workspace: {workspace_path}")
                    return
                except (OSError, PermissionError) as e:
                    if attempt < max_retries - 1:
                        print(f"Cleanup attempt {attempt + 1} failed, retrying in 2s: {e}")
                        time.sleep(2)
                    else:
                        print(f"Failed to cleanup workspace after {max_retries} attempts: {e}")
    except Exception as e:
        print(f"Error cleaning up workspace {workspace_path}: {e}")

def setup_workspace(test_result):
    """Set up a clean workspace directory for the test"""
    try:
        workspace_path = os.path.join(WORK_DIR, test_result.test_id)
        
        # Clean up any existing workspace
        if os.path.exists(workspace_path):
            cleanup_workspace(workspace_path)
        
        # Create fresh workspace
        os.makedirs(workspace_path, exist_ok=True)
        test_result.workspace_path = workspace_path
        
        log_message(test_result, f"Created workspace: {workspace_path}")
        return True
        
    except Exception as e:
        log_message(test_result, f"Failed to setup workspace: {str(e)}")
        return False

def clone_repository(test_result, repo_info):
    """Clone the repository and checkout the specified commit"""
    try:
        if not setup_workspace(test_result):
            return False
            
        repo_url = f"https://github.com/{repo_info['repository']}.git"
        workspace_path = test_result.workspace_path
        commit_sha = repo_info['commit_sha']
        branch = repo_info.get('branch', 'main')
        
        log_message(test_result, f"Cloning repository: {repo_info['repository']}")
        log_message(test_result, f"Target commit: {commit_sha}")
        log_message(test_result, f"Branch context: {branch}")
        
        # Store original directory
        original_cwd = os.getcwd()
        
        try:
            # Step 1: Initialize empty repo
            log_message(test_result, "Initializing repository")
            init_result = subprocess.run(
                ["git", "init"],
                cwd=workspace_path,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if init_result.returncode != 0:
                raise Exception(f"Git init failed: {init_result.stderr}")
            
            # Step 2: Add remote origin
            log_message(test_result, "Adding remote origin")
            remote_result = subprocess.run(
                ["git", "remote", "add", "origin", repo_url],
                cwd=workspace_path,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if remote_result.returncode != 0:
                raise Exception(f"Failed to add remote: {remote_result.stderr}")
            
            # Step 3: Fetch the specific commit directly
            log_message(test_result, f"Fetching commit {commit_sha[:8]}")
            fetch_result = subprocess.run(
                ["git", "fetch", "origin", commit_sha],
                cwd=workspace_path,
                capture_output=True,
                text=True,
                timeout=300
            )
            
            if fetch_result.returncode != 0:
                # If direct fetch fails, try fetching all pull request refs
                log_message(test_result, "Direct fetch failed, fetching all PR refs")
                fetch_pr_result = subprocess.run(
                    ["git", "fetch", "origin", "+refs/pull/*/head:refs/remotes/origin/pr/*"],
                    cwd=workspace_path,
                    capture_output=True,
                    text=True,
                    timeout=300
                )
                
                if fetch_pr_result.returncode != 0:
                    # Final fallback: fetch everything
                    log_message(test_result, "PR fetch failed, performing full fetch")
                    fetch_all_result = subprocess.run(
                        ["git", "fetch", "origin"],
                        cwd=workspace_path,
                        capture_output=True,
                        text=True,
                        timeout=300
                    )
                    
                    if fetch_all_result.returncode != 0:
                        raise Exception(f"All fetch attempts failed. Last error: {fetch_all_result.stderr}")
            
            # Step 4: Checkout the specific commit
            log_message(test_result, f"Checking out commit {commit_sha[:8]}")
            checkout_result = subprocess.run(
                ["git", "checkout", commit_sha],
                cwd=workspace_path,
                capture_output=True,
                text=True,
                timeout=60
            )
            
            if checkout_result.returncode != 0:
                raise Exception(f"Failed to checkout commit {commit_sha}: {checkout_result.stderr}")
            
            # Step 5: Verify we're on the correct commit
            verify_result = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=workspace_path,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if verify_result.returncode != 0:
                raise Exception(f"Failed to verify commit: {verify_result.stderr}")
            
            current_commit = verify_result.stdout.strip()
            if not current_commit.startswith(commit_sha[:8]):
                raise Exception(f"Commit verification failed. Expected {commit_sha[:8]}, got {current_commit[:8]}")
            
            log_message(test_result, f"Successfully checked out commit {current_commit[:8]}")
            
            # Step 6: Get commit info for logging
            try:
                info_result = subprocess.run(
                    ["git", "log", "-1", "--pretty=format:%H %s %an <%ae> %ci"],
                    cwd=workspace_path,
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                if info_result.returncode == 0:
                    log_message(test_result, f"Commit info: {info_result.stdout}")
            except Exception:
                pass  # Non-critical, continue if this fails
            
            return True
            
        except subprocess.TimeoutExpired as e:
            raise Exception(f"Git operation timed out: {e}")
        except Exception as e:
            raise e
        finally:
            os.chdir(original_cwd)
        
    except Exception as e:
        log_message(test_result, f"Failed to clone repository: {str(e)}")
        # Clean up failed workspace
        if test_result.workspace_path and os.path.exists(test_result.workspace_path):
            cleanup_workspace(test_result.workspace_path)
            test_result.workspace_path = None
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
            
            # Calculate coverage if available
            calculate_coverage_results(test_result)
            
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

def calculate_coverage_results(test_result):
    """Calculate Unity test coverage using the coverage script"""
    try:
        if not test_result.workspace_path:
            log_message(test_result, "No workspace path available for coverage calculation")
            return
        
        coverage_script = os.path.join(test_result.workspace_path, "scripts", "calculate-coverage.py")
        coverage_file = os.path.join(test_result.workspace_path, "Unity", "CodeCoverage", "Report", "Summary.json")
        
        log_message(test_result, f"Looking for coverage script at: {coverage_script}")
        log_message(test_result, f"Looking for coverage file at: {coverage_file}")
        
        if not os.path.exists(coverage_script):
            log_message(test_result, "Coverage calculation script not found")
            return
        
        # Check if there's already a Summary.json file from the repository
        if os.path.exists(coverage_file):
            log_message(test_result, "Found existing coverage file, will use it for calculation")
        else:
            log_message(test_result, "No existing coverage file found, checking if one was generated by tests")
            
        if not os.path.exists(coverage_file):
            log_message(test_result, "No coverage results file found at expected location")
            # Try to list what files are available
            coverage_dir = os.path.join(test_result.workspace_path, "Unity", "CodeCoverage")
            if os.path.exists(coverage_dir):
                try:
                    files = []
                    for root, dirs, filenames in os.walk(coverage_dir):
                        for filename in filenames:
                            files.append(os.path.relpath(os.path.join(root, filename), coverage_dir))
                    log_message(test_result, f"Available files in CodeCoverage: {files}")
                    
                    # Try to find Summary.json in any subdirectory
                    for root, dirs, filenames in os.walk(coverage_dir):
                        if "Summary.json" in filenames:
                            alternative_coverage_file = os.path.join(root, "Summary.json")
                            log_message(test_result, f"Found alternative coverage file at: {alternative_coverage_file}")
                            coverage_file = alternative_coverage_file
                            break
                    else:
                        log_message(test_result, "No Summary.json file found in any CodeCoverage subdirectory")
                        return
                except Exception as e:
                    log_message(test_result, f"Error listing CodeCoverage files: {e}")
                    return
            else:
                log_message(test_result, "CodeCoverage directory does not exist")
                return
        
        # Run coverage calculation
        original_cwd = os.getcwd()
        os.chdir(test_result.workspace_path)
        
        try:
            result = subprocess.run(
                [sys.executable, coverage_script, "--json", coverage_file],
                capture_output=True,
                text=True,
                timeout=30
            )
            
            log_message(test_result, f"Coverage script exit code: {result.returncode}")
            if result.stdout:
                log_message(test_result, f"Coverage script stdout: {result.stdout[:200]}...")
            if result.stderr:
                log_message(test_result, f"Coverage script stderr: {result.stderr}")
            
            if result.returncode == 0 and result.stdout.strip():
                import json
                try:
                    coverage_data = json.loads(result.stdout)
                    
                    if coverage_data.get('editmode_tests_found', False):
                        test_result.coverage_available = True
                        test_result.line_coverage = coverage_data['overall_line_coverage']
                        test_result.method_coverage = coverage_data['overall_method_coverage']
                        test_result.test_class_count = coverage_data['test_count']
                        
                        log_message(test_result, f"Coverage calculated: {test_result.line_coverage}% lines, {test_result.method_coverage}% methods, {test_result.test_class_count} test classes")
                    else:
                        log_message(test_result, "No coverage data found for specified test classes")
                except json.JSONDecodeError as e:
                    log_message(test_result, f"Failed to parse coverage JSON: {e}")
            else:
                log_message(test_result, f"Coverage calculation failed with return code {result.returncode}: {result.stderr}")
                
        finally:
            os.chdir(original_cwd)
            
    except Exception as e:
        log_message(test_result, f"Error calculating coverage: {str(e)}")

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
        
        # Schedule workspace cleanup after a delay
        if test_result.workspace_path:
            def delayed_cleanup():
                time.sleep(300)  # Wait 5 minutes before cleanup
                cleanup_workspace(test_result.workspace_path)
            
            threading.Thread(target=delayed_cleanup, daemon=True).start()
        
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
            'logs': test_result.log_output[-20:],  # Return last 20 log entries
            'coverage': {
                'available': test_result.coverage_available,
                'lineCoverage': test_result.line_coverage,
                'methodCoverage': test_result.method_coverage,
                'testClassCount': test_result.test_class_count
            }
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

@app.route('/api/coverage/<test_id>', methods=['GET'])
def get_coverage_info(test_id):
    """Get code coverage information for a test run"""
    try:
        if test_id not in active_tests:
            return jsonify({'error': 'Test not found'}), 404
        
        test_result = active_tests[test_id]
        
        if not test_result.workspace_path:
            return jsonify({'error': 'No workspace path available'}), 404
        
        coverage_dir = os.path.join(test_result.workspace_path, "Unity", "CodeCoverage")
        
        if not os.path.exists(coverage_dir):
            return jsonify({'error': 'No code coverage results found'}), 404
        
        # List coverage files
        coverage_files = []
        try:
            for root, dirs, files in os.walk(coverage_dir):
                for file in files:
                    rel_path = os.path.relpath(os.path.join(root, file), coverage_dir)
                    coverage_files.append(rel_path)
        except Exception as e:
            return jsonify({'error': f'Error listing coverage files: {str(e)}'}), 500
        
        return jsonify({
            'testId': test_id,
            'coverageAvailable': len(coverage_files) > 0,
            'coverageFiles': coverage_files,
            'coveragePath': coverage_dir
        })
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/coverage/<test_id>/calculate', methods=['GET'])
def calculate_coverage_metrics(test_id):
    """Calculate coverage metrics using the calculate-coverage.py script"""
    try:
        if test_id not in active_tests:
            return jsonify({'error': 'Test not found'}), 404
        
        test_result = active_tests[test_id]
        
        if not test_result.workspace_path:
            return jsonify({'error': 'No workspace path available'}), 404
        
        coverage_file = os.path.join(test_result.workspace_path, "Unity", "CodeCoverage", "Report", "Summary.json")
        
        if not os.path.exists(coverage_file):
            return jsonify({'error': 'No coverage summary file found'}), 404
        
        # Import and use the coverage calculation function
        import sys
        script_dir = os.path.dirname(__file__)
        calculate_script_path = os.path.join(script_dir, "calculate-coverage.py")
        
        # Run the coverage calculation script
        try:
            result = subprocess.run(
                [sys.executable, calculate_script_path, "--json", coverage_file],
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if result.returncode == 0:
                import json
                coverage_data = json.loads(result.stdout)
                return jsonify(coverage_data)
            else:
                return jsonify({
                    'error': 'Coverage calculation failed', 
                    'details': result.stderr
                }), 500
                
        except subprocess.TimeoutExpired:
            return jsonify({'error': 'Coverage calculation timed out'}), 500
        except json.JSONDecodeError:
            return jsonify({'error': 'Invalid JSON from coverage calculation'}), 500
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/coverage/<test_id>/download', methods=['GET'])
def download_coverage(test_id):
    """Download code coverage results as a zip file"""
    try:
        if test_id not in active_tests:
            return jsonify({'error': 'Test not found'}), 404
        
        test_result = active_tests[test_id]
        
        if not test_result.workspace_path:
            return jsonify({'error': 'No workspace path available'}), 404
        
        coverage_dir = os.path.join(test_result.workspace_path, "Unity", "CodeCoverage")
        
        if not os.path.exists(coverage_dir):
            return jsonify({'error': 'No code coverage results found'}), 404
        
        # Create a temporary zip file
        import zipfile
        import tempfile
        from flask import send_file
        
        temp_zip = tempfile.NamedTemporaryFile(delete=False, suffix='.zip')
        temp_zip.close()
        
        try:
            with zipfile.ZipFile(temp_zip.name, 'w', zipfile.ZIP_DEFLATED) as zipf:
                for root, dirs, files in os.walk(coverage_dir):
                    for file in files:
                        file_path = os.path.join(root, file)
                        arc_name = os.path.relpath(file_path, coverage_dir)
                        zipf.write(file_path, arc_name)
            
            return send_file(
                temp_zip.name,
                as_attachment=True,
                download_name=f'coverage-{test_id}.zip',
                mimetype='application/zip'
            )
            
        except Exception as e:
            # Clean up temp file on error
            try:
                os.unlink(temp_zip.name)
            except:
                pass
            raise e
        
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