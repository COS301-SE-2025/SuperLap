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

[System.Serializable]
public class TrackImageResponse
{
  public string filepath;
  public string contentType;
  public string data;
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
        _instance = FindAnyObjectByType<APIManager>();
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
    Debug.Log($"Registering user: {username}, Email: {email} with password: {password}");
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
        Debug.LogWarning("Registration error: " + errorMessage);
        callback?.Invoke(false, errorMessage);
      }
    }
  }

  // Login user (check if user exists)
  public void LoginUser(string username, string password, System.Action<bool, string, User> callback)
  {
    Debug.Log($"Logging in user: {username} with password: {password}");
    StartCoroutine(LoginUserCoroutine(username, password, callback));
  }

  private IEnumerator LoginUserCoroutine(string username, string password, System.Action<bool, string, User> callback)
  {
    string loginUrl = $"{baseURL}/users/login";

    // Construct the login payload
    User Login = new User
    {
      username = username,
      password = password
    };

    string jsonData = JsonUtility.ToJson(Login);

    // Set up the POST request
    using (UnityWebRequest request = new UnityWebRequest(loginUrl, "POST"))
    {
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");

      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        string responseText = request.downloadHandler.text;

        try
        {
          User user = JsonUtility.FromJson<User>(responseText);
          Debug.Log("User logged in successfully: " + user.username);
          callback?.Invoke(true, "Login successful", user);
        }
        catch (Exception e)
        {
          Debug.LogError("Error parsing login response: " + e.Message);
          callback?.Invoke(false, "Error parsing server response", null);
        }
      }
      else
      {
        string message = "Login failed";

        if (request.responseCode == 404)
          message = "User not found";
        else if (request.responseCode == 401)
          message = "Invalid password";
        else if (!string.IsNullOrEmpty(request.error))
          message = request.error;

        Debug.LogWarning("Login error: " + message);
        callback?.Invoke(false, message, null);
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
        catch
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
    public string type;
    public string city;
    public string country;
    public string location;
    public string uploadedBy;
    public string description;
    public string dateUploaded;
    public string _id;
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

  // Fetch a track by name
  public void GetTrackByName(string name, System.Action<bool, string, Track> callback)
  {
    StartCoroutine(GetTrackByNameCoroutine(name, callback));
  }

  private IEnumerator GetTrackByNameCoroutine(string name, System.Action<bool, string, Track> callback)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/tracks/{name}"))
    {
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        try
        {
          string json = request.downloadHandler.text;
          Track track = JsonUtility.FromJson<Track>(json);
          callback?.Invoke(true, "Track fetched successfully", track);
        }
        catch
        {
          callback?.Invoke(false, "Error parsing track data", null);
        }
      }
      else
      {
        callback?.Invoke(false, request.error, null);
      }
    }
  }

  // Fetch a track image (base64 or binary)
  public void GetTrackImage(string name, System.Action<bool, string, Texture2D> callback)
  {
    StartCoroutine(GetTrackImageCoroutine(name, callback));
  }

  private IEnumerator GetTrackImageCoroutine(string name, System.Action<bool, string, Texture2D> callback)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/images/{name}"))
    {
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        try
        {
          // Print the response data for debugging
          Debug.Log($"Track image response for {name}: {request.downloadHandler.text}");
          // Parse JSON and extract base64 data
          string text = request.downloadHandler.text;
          Texture2D texture = new Texture2D(2, 2);
          bool loaded = false;
          try
          {
            TrackImageResponse imgResp = JsonUtility.FromJson<TrackImageResponse>(text);
            if (imgResp != null && !string.IsNullOrEmpty(imgResp.data))
            {
              byte[] imageBytes = Convert.FromBase64String(imgResp.data);
              loaded = texture.LoadImage(imageBytes);
            }
          }
          catch
          {
            // If not base64, try as binary
            byte[] imageBytes = request.downloadHandler.data;
            loaded = texture.LoadImage(imageBytes);
          }
          if (loaded)
            callback?.Invoke(true, "Image fetched successfully", texture);
          else
            callback?.Invoke(false, "Failed to load image data", null);
        }
        catch
        {
          callback?.Invoke(false, "Error parsing image data", null);
        }
      }
      else
      {
        callback?.Invoke(false, request.error, null);
      }
    }
  }

  public Vector2[] GetDataPoints()
  {
    return new Vector2[] { new Vector2(1, 1), new Vector2(2, 2), new Vector2(3, 3) };
  }
}
