using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.IO;
using System.Linq;

public class UnityTestRunner
{
    private static TestRunnerApi testRunnerApi;
    private static bool testsCompleted = false;
    private static int exitCode = 0;
    
    [MenuItem("Tools/Run All Tests")]
    public static void RunAllTests()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        
        var filter = new Filter()
        {
            testMode = TestMode.EditMode | TestMode.PlayMode
        };
        
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }
    
    public static void RunTestsFromCommandLine()
    {
        Debug.Log("Starting test execution from command line...");
        
        testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        
        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };
        
        var executionSettings = new ExecutionSettings(filter);
        
        // Set up callbacks to handle test results
        var handler = new TestResultHandler();
        testRunnerApi.RegisterCallbacks(handler);
        
        Debug.Log("About to execute tests...");
        
        // Start the tests
        testRunnerApi.Execute(executionSettings);
        
        Debug.Log("Tests execution started, setting up completion check...");
        
        // Set up update loop to wait for completion with timeout
        float timeoutSeconds = 300f; // 5 minutes
        float startTime = UnityEngine.Time.realtimeSinceStartup;
        
        EditorApplication.CallbackFunction updateCallback = null;
        updateCallback = () =>
        {
            float elapsedTime = UnityEngine.Time.realtimeSinceStartup - startTime;
            
            if (testsCompleted)
            {
                EditorApplication.update -= updateCallback;
                Debug.Log($"Tests completed after {elapsedTime:F2} seconds, exiting Unity...");
                EditorApplication.Exit(exitCode);
            }
            else if (elapsedTime > timeoutSeconds)
            {
                EditorApplication.update -= updateCallback;
                Debug.LogError($"Tests timed out after {timeoutSeconds} seconds, exiting Unity...");
                EditorApplication.Exit(1);
            }
            
            // Log every 30 seconds to show we're still waiting
            if (((int)elapsedTime) % 30 == 0 && ((int)elapsedTime) > 0 && ((int)elapsedTime) != ((int)(elapsedTime - 1)))
            {
                Debug.Log($"Still waiting for tests to complete... ({elapsedTime:F0}s elapsed)");
            }
        };
        EditorApplication.update += updateCallback;
        
        Debug.Log("Test execution initiated, waiting for completion...");
    }
    
    public static void MarkTestsCompleted(int code)
    {
        exitCode = code;
        testsCompleted = true;
    }
}

public class TestResultHandler : ICallbacks
{
    public void RunStarted(ITestAdaptor testsToRun)
    {
        Debug.Log($"Test run started - {testsToRun.Children.Count()} test(s) to run");
    }

    public void RunFinished(ITestResultAdaptor result)
    {
        Debug.Log($"Test run finished. Passed: {result.PassCount}, Failed: {result.FailCount}, Skipped: {result.SkipCount}");
        
        // Write results to file
        string resultsPath = Path.Combine(Application.dataPath, "..", "TestResults", "results.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(resultsPath));
        
        // Create NUnit-style XML results
        string xml = $@"<?xml version='1.0' encoding='UTF-8'?>
<test-results total='{result.PassCount + result.FailCount + result.SkipCount}' errors='0' failures='{result.FailCount}' not-run='{result.SkipCount}' inconclusive='0' ignored='0' skipped='{result.SkipCount}' invalid='0' date='{System.DateTime.Now:yyyy-MM-dd}' time='{System.DateTime.Now:HH:mm:ss}'>
    <test-suite type='Assembly' name='Unity Tests' executed='True' result='{(result.FailCount > 0 ? "Failure" : "Success")}' success='{(result.FailCount == 0 ? "True" : "False")}' time='0' asserts='{result.PassCount + result.FailCount}'>
        <results>
            <test-case name='TestSummary' executed='True' result='{(result.FailCount > 0 ? "Failure" : "Success")}' success='{(result.FailCount == 0 ? "True" : "False")}' time='0' asserts='0'>
                <description>Passed: {result.PassCount}, Failed: {result.FailCount}, Skipped: {result.SkipCount}</description>
            </test-case>
        </results>
    </test-suite>
</test-results>";
        
        File.WriteAllText(resultsPath, xml);
        Debug.Log($"Results written to: {resultsPath}");
        
        // Signal completion
        UnityTestRunner.MarkTestsCompleted(result.FailCount > 0 ? 1 : 0);
    }

    public void TestStarted(ITestAdaptor test)
    {
        Debug.Log($"Test started: {test.Name}");
    }

    public void TestFinished(ITestResultAdaptor result)
    {
        Debug.Log($"Test finished: {result.Test.Name} - {result.TestStatus}");
    }
}
