import subprocess
import re
import sys
import os
import xml.etree.ElementTree as ET
import time
import signal
import atexit
import socket

# Global variable to track backend process
backend_process = None

def is_port_in_use(port):
    """Check if a port is currently in use"""
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        try:
            s.bind(('localhost', port))
            return False
        except socket.error:
            return True

def start_backend_server():
    """Start the backend server if port 3000 is not in use"""
    global backend_process
    
    if is_port_in_use(3000):
        print("Port 3000 is already in use, assuming backend is already running")
        return True
    
    # Find the backend directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    backend_path = os.path.join(script_dir, "..", "Backend", "API")
    backend_path = os.path.abspath(backend_path)
    
    if not os.path.exists(backend_path):
        print(f"Backend directory not found at: {backend_path}")
        return False
    
    if not os.path.exists(os.path.join(backend_path, "package.json")):
        print(f"package.json not found in backend directory: {backend_path}")
        return False
    
    print(f"Starting backend server from: {backend_path}")
    
    try:
        # Start the backend server
        backend_process = subprocess.Popen(
            ["npm", "start"],
            cwd=backend_path,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            preexec_fn=os.setsid  # Create new process group for easier cleanup
        )
        
        # Wait for server to start up
        print("Waiting for backend server to start...")
        for i in range(15):  # Wait up to 15 seconds
            if is_port_in_use(3000):
                print("Backend server started successfully!")
                return True
            time.sleep(1)
            print(f"  Waiting... ({i+1}/15)")
        
        print("Backend server failed to start within 15 seconds")
        if backend_process:
            stop_backend_server()
        return False
        
    except Exception as e:
        print(f"Error starting backend server: {e}")
        return False

def stop_backend_server():
    """Stop the backend server if we started it"""
    global backend_process
    
    if backend_process is None:
        return
    
    try:
        print("Stopping backend server...")
        
        # Try to terminate the entire process group
        os.killpg(os.getpgid(backend_process.pid), signal.SIGTERM)
        
        # Wait for graceful shutdown
        try:
            backend_process.wait(timeout=5)
            print("Backend server stopped gracefully")
        except subprocess.TimeoutExpired:
            # Force kill if it doesn't stop gracefully
            print("Force killing backend server...")
            os.killpg(os.getpgid(backend_process.pid), signal.SIGKILL)
            backend_process.wait()
            print("Backend server force stopped")
            
    except Exception as e:
        print(f"Error stopping backend server: {e}")
    finally:
        backend_process = None

def cleanup_backend():
    """Cleanup function to ensure backend is stopped on exit"""
    stop_backend_server()

# Register cleanup function to run on exit
atexit.register(cleanup_backend)

# Handle signals for graceful shutdown
def signal_handler(signum, frame):
    print(f"\nReceived signal {signum}, cleaning up...")
    cleanup_backend()
    sys.exit(0)

signal.signal(signal.SIGINT, signal_handler)
signal.signal(signal.SIGTERM, signal_handler)

def get_unity_editor_path():
    """Get the path to the first available Unity editor from Unity Hub"""
    try:
        # Run the Unity Hub command to list installed editors
        result = subprocess.run(
            ["unityhub", "--headless", "editors", "-i"],
            capture_output=True,
            text=True,
            check=True
        )
        
        # Parse the output to find the first editor with ", installed at "
        lines = result.stdout.strip().split('\n')
        for line in lines:
            if ", installed at " in line:
                # Extract the path after ", installed at "
                match = re.search(r', installed at (.+)', line)
                if match:
                    unity_path = match.group(1).strip()
                    print(f"Found Unity editor at: {unity_path}")
                    return unity_path
        
        print("No Unity editor found with installation path")
        return None
        
    except subprocess.CalledProcessError as e:
        print(f"Error running Unity Hub command: {e}")
        return None
    except FileNotFoundError:
        print("Unity Hub not found. Make sure it's installed and in your PATH")
        return None

def run_unity_tests(unity_path, project_path="Unity"):
    """Run Unity tests using the specified Unity editor"""
    if not unity_path:
        print("No Unity editor path provided")
        return False
    
    if not os.path.exists(unity_path):
        print(f"Unity editor not found at: {unity_path}")
        return False
    
    # Create results directory if it doesn't exist
    results_dir = os.path.join(project_path, "TestResults")
    os.makedirs(results_dir, exist_ok=True)
    
    # Construct the Unity test command using executeMethod
    test_command = [
        unity_path,
        "-batchmode",
        "-projectPath", project_path,
        "-runTests",
        "-logFile", f"{results_dir}/unity_test_log.txt",
        "-testResults", "TestResults/results.xml"
    ]
    
    print(f"Running Unity tests with command:")
    print(" ".join(test_command))
    
    try:
        # Run the Unity test command
        result = subprocess.run(test_command, capture_output=True, text=True)
        
        # Check if log file was created and contains results
        log_file = f"{results_dir}/unity_test_log.txt"
        if os.path.exists(log_file):
            print(f"Test log created at: {log_file}")
            with open(log_file, 'r') as f:
                log_content = f.read()
                print("Test output:")
                print(log_content[-1000:])  # Print last 1000 characters
        
        if result.returncode == 0:
            print("Unity tests completed successfully")
            return True
        else:
            print(f"Unity tests failed with return code: {result.returncode}")
            if result.stderr:
                print(f"Error output: {result.stderr}")
            return False
        
    except subprocess.CalledProcessError as e:
        print(f"Unity tests failed with return code: {e.returncode}")
        return False

def parse_test_results(results_file):
    """Parse the Unity test results XML file and extract statistics"""
    try:
        tree = ET.parse(results_file)
        root = tree.getroot()
        
        # Get overall statistics from the root test-run element
        total_tests = int(root.get('total', 0))
        passed_tests = int(root.get('passed', 0))
        failed_tests = int(root.get('failed', 0))
        
        print(f"\n{'='*60}")
        print(f"TEST RESULTS SUMMARY")
        print(f"{'='*60}")
        print(f"Total Tests: {total_tests}")
        print(f"Passed: {passed_tests}")
        print(f"Failed: {failed_tests}")
        
        if failed_tests > 0:
            print(f"\n{'='*60}")
            print(f"FAILED TESTS ({failed_tests}):")
            print(f"{'='*60}")
            
            # Find all failed test cases
            failed_test_cases = root.findall(".//test-case[@result='Failed']")
            for i, test_case in enumerate(failed_test_cases, 1):
                test_name = test_case.get('name', 'Unknown')
                class_name = test_case.get('classname', 'Unknown')
                
                print(f"{i}. {class_name}.{test_name}")
                
                # Try to get the failure message
                failure_element = test_case.find('failure')
                if failure_element is not None:
                    message_element = failure_element.find('message')
                    if message_element is not None and message_element.text:
                        # Clean up the failure message
                        failure_msg = message_element.text.strip()
                        # Remove CDATA wrapper if present
                        if failure_msg.startswith('[[') and failure_msg.endswith(']]'):
                            failure_msg = failure_msg[2:-2]
                        print(f"   Error: {failure_msg}")
                print()
        
        if passed_tests > 0:
            print(f"{'='*60}")
            print(f"PASSED TESTS ({passed_tests}):")
            print(f"{'='*60}")
            
            # Find all passed test cases
            passed_test_cases = root.findall(".//test-case[@result='Passed']")
            for i, test_case in enumerate(passed_test_cases, 1):
                test_name = test_case.get('name', 'Unknown')
                class_name = test_case.get('classname', 'Unknown')
                print(f"{i}. {class_name}.{test_name}")
        
        print(f"{'='*60}")
        
        return passed_tests, failed_tests, total_tests
        
    except ET.ParseError as e:
        print(f"Error parsing XML results file: {e}")
        return 0, 0, 0
    except Exception as e:
        print(f"Error reading test results: {e}")
        return 0, 0, 0

def check_test_runner_exists(project_path="Unity"):
    """Check if the Unity test runner script exists in the project"""
    script_path = os.path.join(project_path, "Assets", "Editor", "UnityTestRunner.cs")
    if os.path.exists(script_path):
        print(f"Unity test runner found at: {script_path}")
        return True
    else:
        print(f"Unity test runner not found at: {script_path}")
        print("Please ensure UnityTestRunner.cs exists in Unity/Assets/Editor/")
        return False

def run_unity_tests_with_custom_script(unity_path, project_path="Unity"):
    """Run Unity tests using the existing test runner script"""
    if not unity_path:
        print("No Unity editor path provided")
        return False
    
    if not os.path.exists(unity_path):
        print(f"Unity editor not found at: {unity_path}")
        return False
    
    # Check if the test runner script exists
    if not check_test_runner_exists(project_path):
        print("UnityTestRunner.cs not found. Please ensure it exists in Unity/Assets/Editor/")
        return False
    
    # Create results directory
    results_dir = os.path.join(project_path, "TestResults")
    os.makedirs(results_dir, exist_ok=True)
    
    # Construct the Unity test command
    test_command = [
        unity_path,
        "-batchmode",
        "-projectPath", project_path,
        "-runTests",
        "-logFile", f"{results_dir}/unity_test_log.txt",
        "-testResults", "TestResults/results.xml"
    ]
    
    print(f"Running Unity tests with command:")
    print(" ".join(test_command))
    
    try:
        # Run the Unity test command with a timeout
        print("Running tests with 5-minute timeout...")
        result = subprocess.run(test_command, capture_output=True, text=True, timeout=300)
        
        # Check results
        results_file = os.path.join(results_dir, "results.xml")
        log_file = f"{results_dir}/unity_test_log.txt"
        
        print(f"Unity process exit code: {result.returncode}")
        
        # Parse and display test results
        passed_count = 0
        failed_count = 0
        total_count = 0
        
        if os.path.exists(results_file):
            print(f"Test results created at: {results_file}")
            passed_count, failed_count, total_count = parse_test_results(results_file)
        else:
            print("No results.xml file was created")
        
        if os.path.exists(log_file):
            print(f"Test log created at: {log_file}")
            # Only print log details if there were failures or no results file
            if failed_count > 0 or not os.path.exists(results_file):
                with open(log_file, 'r') as f:
                    lines = f.readlines()
                    print("Last 20 lines of log:")
                    print(''.join(lines[-20:]))
        
        # Return True only if all tests passed
        return failed_count == 0 and total_count > 0
        
    except subprocess.TimeoutExpired:
        print("Unity tests timed out after 5 minutes")
        return False
    except Exception as e:
        print(f"Error running Unity tests: {e}")
        return False

def main():
    print("="*60)
    print("UNITY TEST RUNNER WITH BACKEND MANAGEMENT")
    print("="*60)
    
    # Start backend server first
    print("Step 1: Starting backend server...")
    if not start_backend_server():
        print("Failed to start backend server. Tests may fail.")
        # Continue anyway - tests might still be useful
    
    try:
        print("\nStep 2: Getting Unity editor path...")
        unity_path = get_unity_editor_path()
        
        if not unity_path:
            print("Could not find Unity editor path")
            return False
        
        print("\nStep 3: Running Unity tests...")
        # Try the custom script approach first
        success = run_unity_tests_with_custom_script(unity_path)
        
        if success:
            print("\n" + "="*60)
            print("TESTS COMPLETED SUCCESSFULLY!")
            print("="*60)
            return True
        else:
            print("\n" + "="*60)
            print("TESTS FAILED!")
            print("="*60)
            return False
            
    finally:
        # Always stop the backend server when done
        print("\nStep 4: Cleaning up backend server...")
        stop_backend_server()

if __name__ == "__main__":
    try:
        success = main()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\nInterrupted by user")
        cleanup_backend()
        sys.exit(1)
    except Exception as e:
        print(f"Unexpected error: {e}")
        cleanup_backend()
        sys.exit(1)