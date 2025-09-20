using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#region Models
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
public class RacingData
{
  public string _id;
  public string trackName;
  public string userName;
  public string fastestLapTime;
  public string averageSpeed;
  public string topSpeed;
  public string vehicleUsed;
  public string description;
  public string fileName;
  public long fileSize;
  public string dateUploaded;
  public string uploadedBy;
  public string csvData;
}

[System.Serializable]
public class RacingDataListWrapper
{
  public List<RacingData> Items;
}

[System.Serializable]
public class RacingStatsSummary
{
  public int totalRecords;
  public int uniqueTracksCount;
  public int uniqueUsersCount;
  public float avgFileSizeKB;
  public float totalDataSizeMB;
}


[System.Serializable]
public class TrackList
{
  public List<Track> tracks;
}

[System.Serializable]
public class UserListWrapper
{
  public List<User> users;
}
#endregion

public static class UnityWebRequestExtensions
{
  public static async Task<UnityWebRequest> SendWebRequestAsync(this UnityWebRequest request)
  {
    var tcs = new TaskCompletionSource<UnityWebRequest>();
    var operation = request.SendWebRequest();
    operation.completed += _ => tcs.TrySetResult(request);
    return await tcs.Task;
  }
}

public class APIManager : MonoBehaviour
{
  [Header("API Configuration")]
  //public string baseURL = "https://superlap-api.online";
  public string baseURL = "http://localhost:3000";

  private static APIManager _instance;

  private Dictionary<string, Track> trackCache = new Dictionary<string, Track>();
  private List<Track> allTracksCache = null;
  private Dictionary<string, Texture2D> trackImageCache = new Dictionary<string, Texture2D>();
  private Dictionary<string, byte[]> trackBorderCache = new Dictionary<string, byte[]>();

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

  #region User APIs
  public async Task<(bool success, string message)> RegisterUserAsync(string username, string email, string password)
  {
    User newUser = new User { username = username, email = email, password = password };
    string jsonData = JsonUtility.ToJson(newUser);

    using (UnityWebRequest request = new UnityWebRequest($"{baseURL}/users", "POST"))
    {
      request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");

      var response = await request.SendWebRequestAsync();

      if (response.result == UnityWebRequest.Result.Success)
      {
        return (true, "User registered successfully");
      }
      else
      {
        string errorMessage = response.error;
        try
        {
          ApiResponse res = JsonUtility.FromJson<ApiResponse>(response.downloadHandler.text);
          errorMessage = res.message;
        }
        catch { }
        return (false, errorMessage);
      }
    }
  }

  public async Task<(bool success, string message, User user)> LoginUserAsync(string username, string password)
  {
    string loginUrl = $"{baseURL}/users/login";
    User login = new User { username = username, password = password };
    string jsonData = JsonUtility.ToJson(login);

    using (UnityWebRequest request = new UnityWebRequest(loginUrl, "POST"))
    {
      request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");

      var response = await request.SendWebRequestAsync();

      if (response.result == UnityWebRequest.Result.Success)
      {
        try
        {
          User user = JsonUtility.FromJson<User>(response.downloadHandler.text);
          return (true, "Login successful", user);
        }
        catch (Exception e)
        {
          return (false, $"Error parsing server response: {e.Message}", null);
        }
      }
      else
      {
        string message = response.responseCode switch
        {
          404 => "User not found",
          401 => "Invalid password",
          _ => response.error
        };
        return (false, message, null);
      }
    }
  }

  public async Task<(bool success, string message, List<User> users)> GetAllUsersAsync()
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/users"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        try
        {
          string wrapped = "{\"users\":" + response.downloadHandler.text + "}";
          UserListWrapper wrapper = JsonUtility.FromJson<UserListWrapper>(wrapped);
          return (true, "Users retrieved successfully", wrapper.users);
        }
        catch
        {
          return (false, "Error parsing users data", null);
        }
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }
  #endregion

  #region Track APIs
  public async Task<(bool success, string message, List<Track> tracks)> GetAllTracksAsync(bool forceRefresh = false)
  {
    if (!forceRefresh && allTracksCache != null)
      return (true, "Tracks loaded from cache", new List<Track>(allTracksCache));

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/tracks"))
    {
      var networkStart = stopwatch.ElapsedMilliseconds;
      var response = await request.SendWebRequestAsync();
      var networkEnd = stopwatch.ElapsedMilliseconds;

      if (response.result == UnityWebRequest.Result.Success)
      {
        var parseStart = stopwatch.ElapsedMilliseconds;
        Track[] tracks = JsonHelper.FromJson<Track>(response.downloadHandler.text);
        allTracksCache = new List<Track>(tracks);
        var parseEnd = stopwatch.ElapsedMilliseconds;

        return (true, "Tracks loaded successfully", allTracksCache);
      }
      else
      {
        Debug.Log($"[APIManager] Request failed after {stopwatch.ElapsedMilliseconds} ms: {response.error}");
        return (false, response.error ?? "Unknown error", null);
      }
    }
  }



  public async Task<(bool success, string message, Track track)> GetTrackByNameAsync(string name, bool forceRefresh = false)
  {
    if (!forceRefresh && trackCache.TryGetValue(name, out Track cached))
      return (true, "Track loaded from cache", cached);

    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/tracks/{name}"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        try
        {
          Track track = JsonUtility.FromJson<Track>(response.downloadHandler.text);
          trackCache[name] = track;
          return (true, "Track fetched successfully", track);
        }
        catch
        {
          return (false, "Error parsing track data", null);
        }
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, Texture2D image)> GetTrackImageAsync(string name, bool forceRefresh = false)
  {
    if (!forceRefresh && trackImageCache.TryGetValue(name, out Texture2D cached))
      return (true, "Image loaded from cache", cached);

    string url = $"{baseURL}/images/{name}";
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        string base64Data = response.downloadHandler.text.Trim();
        if (base64Data.StartsWith("\"") && base64Data.EndsWith("\""))
          base64Data = base64Data.Substring(1, base64Data.Length - 2);

        base64Data = CleanBase64String(base64Data);

        if (string.IsNullOrEmpty(base64Data))
          return (false, "Invalid image data after cleaning", null);

        try
        {
          byte[] bytes = Convert.FromBase64String(base64Data);
          Texture2D texture = new Texture2D(2, 2);
          if (texture.LoadImage(bytes))
          {
            trackImageCache[name] = texture;
            return (true, "Image loaded successfully", texture);
          }
          else
            return (false, "Failed to load texture", null);
        }
        catch (Exception ex)
        {
          return (false, $"Base64 decode error: {ex.Message}", null);
        }
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, byte[] border)> GetTrackBorderAsync(string name, bool forceRefresh = false)
  {
    if (!forceRefresh && trackBorderCache.TryGetValue(name, out byte[] cached))
      return (true, "Border loaded from cache", cached);

    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/tracks/{name}"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        Track track = JsonUtility.FromJson<Track>(response.downloadHandler.text);
        if (string.IsNullOrEmpty(track.borders))
          return (false, $"No border data found for track: {name}", null);

        try
        {
          byte[] borderBytes = Convert.FromBase64String(track.borders);
          // Trim trailing 0
          int trailingIndex = borderBytes.Length - 4;
          if (trailingIndex >= 0)
          {
            int trailingValue = BitConverter.ToInt32(borderBytes, trailingIndex);
            if (trailingValue == 0)
              Array.Resize(ref borderBytes, trailingIndex);
          }

          trackBorderCache[name] = borderBytes;
          return (true, "Success", borderBytes);
        }
        catch (FormatException ex)
        {
          return (false, $"Failed to decode base64 border: {ex.Message}", null);
        }
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }
  #endregion

  #region Racing Data APIs

  public async Task<(bool success, string message, List<RacingData> data)> GetAllRacingDataAsync()
  {
    string url = $"{baseURL}/racing-data";

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
      var response = await request.SendWebRequestAsync();

      if (response.result == UnityWebRequest.Result.Success)
      {
        string responseText = response.downloadHandler.text;

        try
        {
          string wrapped = "{\"Items\":" + responseText + "}";
          RacingDataListWrapper wrapper = JsonUtility.FromJson<RacingDataListWrapper>(wrapped);
          return (true, "Racing data retrieved successfully", wrapper.Items);
        }
        catch (Exception ex)
        {
          Debug.LogError($"[APIManager] JSON parse error: {ex.Message}");
          return (false, $"Error parsing racing data: {ex.Message}", null);
        }
      }
      else
      {
        Debug.LogError($"[APIManager] Request failed: {response.error}");
        return (false, response.error, null);
      }
    }
  }


  public async Task<(bool success, string message, RacingData data)> GetRacingDataByIdAsync(string id)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/racing-data/{id}"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        RacingData data = JsonUtility.FromJson<RacingData>(response.downloadHandler.text);
        return (true, "Racing data retrieved successfully", data);
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, List<RacingData> data)> GetRacingDataByTrackAsync(string trackName)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/racing-data/track/{trackName}"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        string wrapped = "{\"Items\":" + response.downloadHandler.text + "}";
        RacingDataListWrapper wrapper = JsonUtility.FromJson<RacingDataListWrapper>(wrapped);
        return (true, "Track racing data retrieved successfully", wrapper.Items);
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, List<RacingData> data)> GetRacingDataByUserAsync(string userName)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/racing-data/user/{userName}"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        string wrapped = "{\"Items\":" + response.downloadHandler.text + "}";
        RacingDataListWrapper wrapper = JsonUtility.FromJson<RacingDataListWrapper>(wrapped);
        return (true, "User racing data retrieved successfully", wrapper.Items);
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, RacingStatsSummary stats)> GetRacingDataStatsAsync()
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/racing-data/stats/summary"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        RacingStatsSummary stats = JsonUtility.FromJson<RacingStatsSummary>(response.downloadHandler.text);
        return (true, "Stats retrieved successfully", stats);
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  public async Task<(bool success, string message, byte[] csvBytes)> DownloadRacingDataCsvAsync(string id)
  {
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/racing-data/{id}/download"))
    {
      var response = await request.SendWebRequestAsync();
      if (response.result == UnityWebRequest.Result.Success)
      {
        return (true, "CSV downloaded successfully", response.downloadHandler.data);
      }
      else
      {
        return (false, response.error, null);
      }
    }
  }

  #endregion


  private string CleanBase64String(string input)
  {
    input = Regex.Replace(input, @"[^\w\d+/=]", "");
    input = input.Replace('-', '+').Replace('_', '/');
    int mod4 = input.Length % 4;
    if (mod4 > 0)
      input += new string('=', 4 - mod4);

    return input;
  }

  public void ClearCache()
  {
    trackCache.Clear();
    allTracksCache = null;
    trackImageCache.Clear();
    trackBorderCache.Clear();
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

  public Vector2[] GetDataPoints()
  {
    // Example static data
    return new Vector2[]
    {
        new Vector2(0,0),
        new Vector2(1,2),
        new Vector2(2,1),
        new Vector2(3,3)
    };
  }
}
