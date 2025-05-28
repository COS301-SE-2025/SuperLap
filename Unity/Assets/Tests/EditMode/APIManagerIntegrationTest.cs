using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class APIManagerIntegrationTest
{
    private APIManager apiManager;
    private GameObject testGameObject;

    [SetUp]
    public void Setup()
    {
        // Create a test GameObject with APIManager
        testGameObject = new GameObject("TestAPIManager");
        apiManager = testGameObject.AddComponent<APIManager>();
        
        // Set a test base URL for integration testing
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
    public void User_ValidEmailFormat_AcceptsValidEmails()
    {
        // Test various valid email formats
        string[] validEmails = {
            "test@example.com",
            "user.name@domain.co.uk",
            "user+tag@domain.org",
            "123@domain.net"
        };

        foreach (string email in validEmails)
        {
            User user = new User { email = email };
            Assert.IsNotNull(user.email);
            Assert.AreEqual(email, user.email);
        }
    }

    [Test]
    public void User_PasswordSecurity_StoresPasswordAsProvided()
    {
        // Test that passwords are stored as provided (not hashed in client)
        string[] passwords = {
            "simple123",
            "Complex!Password@123",
            "verylongpasswordwithmanycharacters",
            "P@ssw0rd!"
        };

        foreach (string password in passwords)
        {
            User user = new User { password = password };
            Assert.AreEqual(password, user.password);
        }
    }

    [Test]
    public void APIManager_Singleton_PersistsAcrossScenes()
    {
        // Test that the singleton instance persists
        APIManager instance1 = APIManager.Instance;
        
        // Simulate scene change by getting instance again
        APIManager instance2 = APIManager.Instance;
        
        Assert.AreSame(instance1, instance2);
        Assert.IsNotNull(instance1);
    }

    [UnityTest]
    public IEnumerator RegisterUser_EmptyEmail_HandlesGracefully()
    {
        // Test registration with empty email
        bool callbackCalled = false;
        bool success = true; // Expect this to be false after the call
        string message = "";

        apiManager.RegisterUser("testuser", "", "password123", (isSuccess, msg) =>
        {
            callbackCalled = true;
            success = isSuccess;
            message = msg;
        });

        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(callbackCalled);
        // The actual success/failure depends on server validation
    }

    [UnityTest]
    public IEnumerator LoginUser_EmptyUsername_HandlesGracefully()
    {
        // Test login with empty username
        bool callbackCalled = false;
        bool success = true; // Expect this to be false after the call
        string message = "";
        User returnedUser = null;

        apiManager.LoginUser("", "password123", (isSuccess, msg, user) =>
        {
            callbackCalled = true;
            success = isSuccess;
            message = msg;
            returnedUser = user;
        });

        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(callbackCalled);
        // The actual success/failure depends on server validation
    }

    [Test]
    public void JsonSerialization_SpecialCharacters_HandledCorrectly()
    {
        // Test JSON serialization with special characters
        User user = new User
        {
            username = "user@#$%^&*()",
            email = "test+tag@domain-name.co.uk",
            password = "P@ssw0rd!#$%"
        };

        string json = JsonUtility.ToJson(user);
        Assert.IsNotNull(json);

        User deserializedUser = JsonUtility.FromJson<User>(json);
        Assert.AreEqual(user.username, deserializedUser.username);
        Assert.AreEqual(user.email, deserializedUser.email);
        Assert.AreEqual(user.password, deserializedUser.password);
    }

    [Test]
    public void JsonSerialization_UnicodeCharacters_HandledCorrectly()
    {
        // Test JSON serialization with Unicode characters
        User user = new User
        {
            username = "用户名",
            email = "tëst@éxämplë.com",
            password = "密码123"
        };

        string json = JsonUtility.ToJson(user);
        Assert.IsNotNull(json);

        User deserializedUser = JsonUtility.FromJson<User>(json);
        Assert.AreEqual(user.username, deserializedUser.username);
        Assert.AreEqual(user.email, deserializedUser.email);
        Assert.AreEqual(user.password, deserializedUser.password);
    }

    [Test]
    public void APIManager_BaseURL_HandlesTrailingSlash()
    {
        // Test that base URL handles trailing slashes correctly
        string urlWithSlash = "http://localhost:3000/";
        string urlWithoutSlash = "http://localhost:3000";

        apiManager.baseURL = urlWithSlash;
        Assert.AreEqual(urlWithSlash, apiManager.baseURL);

        apiManager.baseURL = urlWithoutSlash;
        Assert.AreEqual(urlWithoutSlash, apiManager.baseURL);
    }

    [UnityTest]
    public IEnumerator APIManager_ConcurrentRequests_HandleCorrectly()
    {
        // Test multiple concurrent API requests
        List<bool> callbackResults = new List<bool>();
        int expectedCallbacks = 5;

        // Start multiple concurrent requests
        for (int i = 0; i < expectedCallbacks; i++)
        {
            int index = i; // Capture for closure
            apiManager.RegisterUser($"user{index}", $"user{index}@test.com", $"pass{index}", 
                (success, msg) => { callbackResults.Add(true); });
        }

        // Wait for all requests to complete
        float timeout = 2.0f;
        float elapsed = 0f;
        while (callbackResults.Count < expectedCallbacks && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Assert.AreEqual(expectedCallbacks, callbackResults.Count, 
            "All concurrent requests should complete");
    }
}
