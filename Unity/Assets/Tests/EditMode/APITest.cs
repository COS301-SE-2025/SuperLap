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
        
        // Set a test base URL - backend should already be running via Python script
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
}
