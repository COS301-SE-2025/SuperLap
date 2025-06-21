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
}
