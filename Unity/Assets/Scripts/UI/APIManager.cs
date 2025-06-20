using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class User
{
    public string username;
    public string email;
    public string _id;
    public string password;
}

[System.Serializable]
public class ApiResponse
{
    public string message;
}

public class APIManager : MonoBehaviour
{
    [Header("API Configuration")]
    public string baseURL = "http://localhost:3000";
    
    private static APIManager _instance;
    public static APIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<APIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("APIManager");
                    _instance = go.AddComponent<APIManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Register a new user
    public void RegisterUser(string username, string email, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(RegisterUserCoroutine(username, email, password, callback));
    }

    private IEnumerator RegisterUserCoroutine(string username, string email, string password, System.Action<bool, string> callback)
    {
        User newUser = new User
        {
            username = username,
            email = email,
            password = password
        };

        string jsonData = JsonUtility.ToJson(newUser);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest($"{baseURL}/users", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("User registered successfully: " + request.downloadHandler.text);
                callback?.Invoke(true, "User registered successfully");
            }
            else
            {
                string errorMessage = "Registration failed";
                try
                {
                    ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                    errorMessage = response.message;
                }
                catch
                {
                    errorMessage = request.error;
                }
                callback?.Invoke(false, errorMessage);
            }
        }
    }

    // Login user (check if user exists)
    public void LoginUser(string username, string password, System.Action<bool, string, User> callback)
    {
        StartCoroutine(LoginUserCoroutine(username, password, callback));
    }

    private IEnumerator LoginUserCoroutine(string username, string password, System.Action<bool, string, User> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/users/{username}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                
                // Check if user exists (API returns null if user doesn't exist)
                if (responseText == "null" || string.IsNullOrEmpty(responseText))
                {
                    callback?.Invoke(false, "User not found", null);
                }
                else
                {
                    try
                    {
                        User user = JsonUtility.FromJson<User>(responseText);
                        if (user.password == password)
                        {
                            Debug.Log("User logged in successfully: " + user.username);
                            callback?.Invoke(true, "Login successful", user);
                        }
                        else
                        {
                            callback?.Invoke(false, "Invalid password", null);
                        }
                    }
                    catch (Exception e)
                    {
                        callback?.Invoke(false, "Error parsing user data", null);
                    }
                }
            }
            else
            {
                callback?.Invoke(false, request.error, null);
            }
        }
    }

    // Get all users (for testing purposes)
    public void GetAllUsers(System.Action<bool, string, List<User>> callback)
    {
        StartCoroutine(GetAllUsersCoroutine(callback));
    }

    private IEnumerator GetAllUsersCoroutine(System.Action<bool, string, List<User>> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/users"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    // Unity's JsonUtility doesn't handle arrays directly, so we need to wrap it
                    string wrappedJson = "{\"users\":" + jsonResponse + "}";
                    UserListWrapper wrapper = JsonUtility.FromJson<UserListWrapper>(wrappedJson);
                    
                    Debug.Log("Retrieved " + wrapper.users.Count + " users");
                    callback?.Invoke(true, "Users retrieved successfully", wrapper.users);
                }
                catch (Exception e)
                {
                    callback?.Invoke(false, "Error parsing users data", null);
                }
            }
            else
            {
                callback?.Invoke(false, request.error, null);
            }
        }
    }

    // Helper class for JSON array parsing
    [System.Serializable]
    private class UserListWrapper
    {
        public List<User> users;
    }

    //Track routes

    [System.Serializable]
    public class Track
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    public class TrackList
    {
        public List<Track> tracks;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrapped = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.Items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    private IEnumerator GetAllTracksCoroutine(System.Action<bool, string, List<Track>> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/tracks"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                Track[] tracks = JsonHelper.FromJson<Track>(json);
                callback?.Invoke(true, "Tracks loaded successfully", new List<Track>(tracks));
            }
            else
            {
                string errorMessage = request.error ?? "Unknown error occurred";
                callback?.Invoke(false, errorMessage, null);
            }
        }
    }

    public void GetAllTracks(System.Action<bool, string, List<Track>> callback)
    {
        StartCoroutine(GetAllTracksCoroutine(callback));
    }
} 
