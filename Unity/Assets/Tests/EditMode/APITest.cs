using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class APITest
{
    private APIManager apiManager;
    private GameObject testGameObject;

    [SetUp]
    public void Setup()
    {
        // Create a test GameObject with APIManager
        testGameObject = new GameObject("TestAPIManager");
        apiManager = testGameObject.AddComponent<APIManager>();
        
        // Set a test base URL
        apiManager.baseURL = "http://localhost:3000";
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        if (testGameObject != null)
        {
            Object.DestroyImmediate(testGameObject);
        }
    }

    [Test]
    public void APIManager_Singleton_ReturnsSameInstance()
    {
        // Test that the singleton pattern works correctly
        APIManager instance1 = APIManager.Instance;
        APIManager instance2 = APIManager.Instance;
        
        Assert.IsNotNull(instance1);
        Assert.AreSame(instance1, instance2);
    }

    [Test]
    public void User_Serialization_WorksCorrectly()
    {
        // Test User class serialization
        User testUser = new User
        {
            username = "testuser",
            email = "test@example.com",
            _id = "12345",
            password = "testpassword"
        };

        string json = JsonUtility.ToJson(testUser);
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("testuser"));
        Assert.IsTrue(json.Contains("test@example.com"));

        User deserializedUser = JsonUtility.FromJson<User>(json);
        Assert.AreEqual(testUser.username, deserializedUser.username);
        Assert.AreEqual(testUser.email, deserializedUser.email);
        Assert.AreEqual(testUser._id, deserializedUser._id);
        Assert.AreEqual(testUser.password, deserializedUser.password);
    }

    [Test]
    public void ApiResponse_Serialization_WorksCorrectly()
    {
        // Test ApiResponse class serialization
        ApiResponse response = new ApiResponse
        {
            message = "Test message"
        };

        string json = JsonUtility.ToJson(response);
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("Test message"));

        ApiResponse deserializedResponse = JsonUtility.FromJson<ApiResponse>(json);
        Assert.AreEqual(response.message, deserializedResponse.message);
    }

    [UnityTest]
    public IEnumerator RegisterUser_ValidData_CallsCallback()
    {
        // Test user registration with valid data
        bool callbackCalled = false;
        bool success = false;
        string message = "";

        apiManager.RegisterUser("testuser", "test@example.com", "password123", (isSuccess, msg) =>
        {
            callbackCalled = true;
            success = isSuccess;
            message = msg;
        });

        // Wait for the coroutine to complete (simulating network delay)
        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(callbackCalled, "Callback should be called");
        // Note: Since we're testing without a real server, we expect this to fail
        // In a real test environment, you'd use a mock server or test server
    }

    [UnityTest]
    public IEnumerator LoginUser_ValidCredentials_CallsCallback()
    {
        // Test user login with valid credentials
        bool callbackCalled = false;
        bool success = false;
        string message = "";
        User returnedUser = null;

        apiManager.LoginUser("testuser", "password123", (isSuccess, msg, user) =>
        {
            callbackCalled = true;
            success = isSuccess;
            message = msg;
            returnedUser = user;
        });

        // Wait for the coroutine to complete
        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(callbackCalled, "Callback should be called");
        // Note: Since we're testing without a real server, we expect this to fail
        // In a real test environment, you'd use a mock server or test server
    }

    [UnityTest]
    public IEnumerator GetAllUsers_CallsCallback()
    {
        // Test getting all users
        bool callbackCalled = false;
        bool success = false;
        string message = "";
        List<User> users = null;

        apiManager.GetAllUsers((isSuccess, msg, userList) =>
        {
            callbackCalled = true;
            success = isSuccess;
            message = msg;
            users = userList;
        });

        // Wait for the coroutine to complete
        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(callbackCalled, "Callback should be called");
        // Note: Since we're testing without a real server, we expect this to fail
        // In a real test environment, you'd use a mock server or test server
    }

    [Test]
    public void APIManager_BaseURL_CanBeSet()
    {
        // Test that the base URL can be set correctly
        string testURL = "http://test.example.com:8080";
        apiManager.baseURL = testURL;
        
        Assert.AreEqual(testURL, apiManager.baseURL);
    }

    [Test]
    public void User_DefaultValues_AreNull()
    {
        // Test that a new User object has null values by default
        User user = new User();
        
        Assert.IsNull(user.username);
        Assert.IsNull(user.email);
        Assert.IsNull(user._id);
        Assert.IsNull(user.password);
    }

    [Test]
    public void ApiResponse_DefaultValues_AreNull()
    {
        // Test that a new ApiResponse object has null values by default
        ApiResponse response = new ApiResponse();
        
        Assert.IsNull(response.message);
    }

    [UnityTest]
    public IEnumerator APIManager_MultipleRequests_DontInterfere()
    {
        // Test that multiple simultaneous requests don't interfere with each other
        int callbackCount = 0;
        
        // Start multiple requests
        apiManager.RegisterUser("user1", "user1@test.com", "pass1", (success, msg) => callbackCount++);
        apiManager.RegisterUser("user2", "user2@test.com", "pass2", (success, msg) => callbackCount++);
        apiManager.LoginUser("user3", "pass3", (success, msg, user) => callbackCount++);

        // Wait for all coroutines to complete
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(3, callbackCount, "All callbacks should be called");
    }
}
