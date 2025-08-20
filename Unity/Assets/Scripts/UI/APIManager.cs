using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

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
  public string baseURL = "https://superlap-api.online";

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

  public void LoginUser(string username, string password, System.Action<bool, string, User> callback)
  {
    Debug.Log($"Logging in user: {username} with password: {password}");
    StartCoroutine(LoginUserCoroutine(username, password, callback));
  }

  private IEnumerator LoginUserCoroutine(string username, string password, System.Action<bool, string, User> callback)
  {
    string loginUrl = $"{baseURL}/users/login";

    User Login = new User
    {
      username = username,
      password = password
    };

    string jsonData = JsonUtility.ToJson(Login);

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
          Debug.Log("Error parsing login response: " + e.Message);
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


  [System.Serializable]
  private class UserListWrapper
  {
    public List<User> users;
  }



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
    public string borders;
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

  public void GetTrackImage(string name, System.Action<bool, string, Texture2D> callback)
  {
    StartCoroutine(GetTrackImageCoroutine(name, callback));
  }

  private IEnumerator GetTrackImageCoroutine(string name, System.Action<bool, string, Texture2D> callback)
  {
    string url = $"{baseURL}/images/{name}";
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        string base64Data = request.downloadHandler.text.Trim();



        if (base64Data.StartsWith("\"") && base64Data.EndsWith("\""))
        {
          base64Data = base64Data.Substring(1, base64Data.Length - 2);
        }

        base64Data = CleanBase64String(base64Data);

        if (string.IsNullOrEmpty(base64Data))
        {
          callback?.Invoke(false, "Invalid image data after cleaning", null);
          yield break;
        }

        try
        {
          byte[] imageBytes = Convert.FromBase64String(base64Data);
          Texture2D texture = new Texture2D(2, 2);
          if (texture.LoadImage(imageBytes))
          {
            callback?.Invoke(true, "Image loaded successfully", texture);
          }
          else
          {
            callback?.Invoke(false, "Failed to load texture from bytes", null);
          }
        }
        catch (Exception ex)
        {
          Debug.LogError($"Base64 decode error: {ex.Message}\nCleaned data: {base64Data}");
          callback?.Invoke(false, $"Base64 decode error: {ex.Message}", null);
        }
      }
      else
      {
        string errorMsg = request.responseCode == 404
            ? $"Image not found for track: {name}"
            : $"Failed to load image: {request.error}";
        Debug.LogError(errorMsg);
        callback?.Invoke(false, errorMsg, null);
      }
    }
  }

  public void GetTrackBorder(string name, System.Action<bool, string, byte[]> callback)
  {
    StartCoroutine(GetTrackBorderCoroutine(name, callback));
  }

  private IEnumerator GetTrackBorderCoroutine(string name, System.Action<bool, string, byte[]> callback)
  {
    string url = $"{baseURL}/tracks/{name}";

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        string baseData = request.downloadHandler.text.Trim();

        Track track = JsonUtility.FromJson<Track>(baseData);

        if (string.IsNullOrEmpty(track.borders))
        {
          string errorMsg = $"No border data found for track: {name}";
          Debug.LogError(errorMsg);
          callback?.Invoke(false, errorMsg, null);
          yield break;
        }

        try
        {
          byte[] borderBytes = Convert.FromBase64String(track.borders);
          int trailingIndex = borderBytes.Length - 4;
          if (trailingIndex >= 0)
          {
            int trailingValue = BitConverter.ToInt32(borderBytes, trailingIndex);
            if (trailingValue == 0)
            {
              Array.Resize(ref borderBytes, trailingIndex);
            }
          }
          callback(true, "Success", borderBytes);
        }
        catch (FormatException ex)
        {
          string errorMsg = $"Failed to decode base64 border: {ex.Message}";
          Debug.LogError(errorMsg);
          callback?.Invoke(false, errorMsg, null);
        }
      }
      else
      {
        string errorMsg = request.responseCode == 404
            ? $"Image not found for track: {name}"
            : $"Failed to load image: {request.error}";
        Debug.LogError(errorMsg);
        callback?.Invoke(false, errorMsg, null);
      }
    }
  }


  private string CleanBase64String(string input)
  {
    input = Regex.Replace(input, @"[^\w\d+/=]", "");
    input = input.Replace('-', '+').Replace('_', '/');
    int mod4 = input.Length % 4;
    if (mod4 > 0)
    {
      input += new string('=', 4 - mod4);
    }

    return input;
  }
  public Vector2[] GetDataPoints()
  {
    return new Vector2[] { new Vector2(1, 0.5f), new Vector2(2, 1), new Vector2(3, 1.5f), new Vector2(4, 2.0f) };
  }
}
