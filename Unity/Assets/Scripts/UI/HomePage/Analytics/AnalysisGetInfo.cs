using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class AnalysisGetInfo : MonoBehaviour
{
  [Header("Track Info Display")]
  public TMP_Text trackNameText;
  public TMP_Text trackLocationText;
  public TMP_Text trackTypeText;
  public TMP_Text trackDescriptionText;
  public TMP_Text trackCityText;
  public TMP_Text trackCountryText;
  public Button racingLineButton;
  private ShowRacingLine racingLinePreview;
  private HomePageNavigation homePageNavigation;

  private APIManager apiManager;
  private bool isLoading = false;
  private int trackIndex = 0;
  private string trackName;
  private bool manualTrackRequested = false;

  private int retryCount = 0;
  private const int maxRetries = 5;
  private const float retryInterval = 2.0f;
  private float retryTimer = 0.0f;
  private bool isWaitingForRetry = false;

  public void Awake()
  {
    try
    {
      apiManager = APIManager.Instance;
      homePageNavigation = FindAnyObjectByType<HomePageNavigation>();
      racingLinePreview = GetComponentInChildren<ShowRacingLine>();

      if (racingLinePreview != null)
      {
        racingLinePreview.gameObject.SetActive(false);
      }
      if (racingLineButton != null)
      {
        racingLineButton.interactable = false;
      }
    }
    catch (System.Exception e)
    {
      Debug.Log($"Failed to initialize components: {e.Message}");
      SetDefaultValues();
    }
  }

  private void ResetValues()
  {
    if (trackNameText != null) trackNameText.text = "Loading...";
    if (trackTypeText != null) trackTypeText.text = "";
    if (trackCityText != null) trackCityText.text = "";
    if (trackCountryText != null) trackCountryText.text = "";
    if (trackDescriptionText != null) trackDescriptionText.text = "";
    if (trackLocationText != null) trackLocationText.text = "";

    if (racingLineButton != null)
    {
      racingLineButton.interactable = false;
    }

    if (racingLinePreview != null)
    {
      racingLinePreview.gameObject.SetActive(false);
    }

    trackName = "";
    retryCount = 0;
    isWaitingForRetry = false;
    retryTimer = 0.0f;
    isLoading = false;
  }

  public void OnEnable()
  {
    if (!manualTrackRequested)
      AttemptToLoadTracks();
  }

  public void OnDisable()
  {
    ResetValues();

    if (racingLinePreview != null)
    {
      racingLinePreview.gameObject.SetActive(false);
    }
  }

  public void Start()
  {
    ResetValues();
    if (!manualTrackRequested)
      AttemptToLoadTracks();
  }
  private void AttemptToLoadTracks()
  {
    if (isLoading) return;
    isLoading = true;

    try
    {
      if (apiManager != null)
      {
        apiManager.GetAllTracks((success, message, tracks) =>
        {
          isLoading = false;
          OnTracksLoaded(success, message, tracks);
        });
      }
      else
      {
        isLoading = false;
        Debug.Log("APIManager is null");
        HandleLoadFailure();
      }
    }
    catch (System.Exception e)
    {
      isLoading = false;
      Debug.Log($"Error in AttemptToLoadTracks: {e.Message}");
      HandleLoadFailure();
    }
  }

  private void HandleLoadFailure()
  {
    retryCount++;

    if (retryCount <= maxRetries)
    {
      Debug.Log($"API load failed. Retry {retryCount}/{maxRetries} in {retryInterval} seconds...");
      isWaitingForRetry = true;
      retryTimer = 0.0f;

      if (trackNameText != null)
        trackNameText.text = $"Loading... (Retry {retryCount}/{maxRetries})";
    }
    else
    {
      Debug.Log("Maximum retry attempts reached. Giving up.");
      DisplayErrorMessage("Failed to load track data after multiple attempts");
      isWaitingForRetry = false;
    }
  }

  private void OnTracksLoaded(bool success, string message, List<APIManager.Track> tracks)
  {
    if (!success)
    {
      Debug.Log($"Failed to load tracks: {message}");
      HandleLoadFailure();
      return;
    }

    if (tracks == null || tracks.Count == 0)
    {
      Debug.LogWarning("No tracks found in the database");
      DisplayErrorMessage("No tracks available");
      return;
    }
    retryCount = 0;
    isWaitingForRetry = false;

    if (trackIndex < tracks.Count)
    {
      DisplayTrackInfo(tracks[trackIndex]);
    }
    else
    {
      DisplayTrackInfo(tracks[0]);
    }
  }

  private void DisplayTrackInfo(APIManager.Track track)
  {
    if (trackNameText != null)
      trackNameText.text = track.name ?? "Unknown Track";

    if (trackTypeText != null)
      trackTypeText.text = track.type ?? "Unknown Type";

    if (trackCityText != null)
      trackCityText.text = track.city ?? "Unknown City";

    if (trackCountryText != null)
      trackCountryText.text = track.country ?? "Unknown Country";

    if (trackDescriptionText != null)
      trackDescriptionText.text = track.description ?? "No description available";

    if (trackLocationText != null)
    {
      if (!string.IsNullOrEmpty(track.location))
      {
        string[] coordinates = track.location.Split(',');
        if (coordinates.Length >= 2)
        {
          if (float.TryParse(coordinates[0].Trim(), out float latitude) &&
              float.TryParse(coordinates[1].Trim(), out float longitude))
          {
            trackLocationText.text = $"Lat: {latitude:F6}, Lon: {longitude:F6}";
          }
          else
          {
            trackLocationText.text = track.location;
          }
        }
        else
        {
          trackLocationText.text = track.location;
        }
      }
      else
      {
        trackLocationText.text = "Location: Unknown";
      }
    }

    trackName = track.name;
    if (racingLinePreview != null)
    {
      racingLinePreview.gameObject.SetActive(true);
      racingLinePreview.InitializeWithTrack(trackName);
    }
    else
    {
      Debug.LogWarning("Racing line preview component not assigned");
    }

    LoadRacingLinePreview();
  }

  public void DisplayTrackByIndex(int index)
  {
    try
    {
      if (apiManager != null)
      {
        trackIndex = index;
        apiManager.GetAllTracks(OnTracksLoaded);
      }
      else
      {
        Debug.Log("APIManager is null in DisplayTrackByIndex");
        SetDefaultValues();
      }
    }
    catch (System.Exception e)
    {
      Debug.Log($"Error in DisplayTrackByIndex: {e.Message}");
      SetDefaultValues();
    }
  }

  public void DisplayTrackByName(string trackName)
  {
    manualTrackRequested = true;
    this.trackName = trackName;
    try
    {
      if (apiManager != null)
      {
        apiManager.GetTrackByName(trackName, (success, message, track) =>
        {
          if (success && track != null)
          {
            DisplayTrackInfo(track);
          }
          else
          {
            Debug.Log($"Failed to load track '{trackName}': {message}");
            SetDefaultValues();
          }
        });
      }
      else
      {
        Debug.Log("APIManager is null in DisplayTrackByName");
        SetDefaultValues();
      }
    }
    catch (System.Exception e)
    {
      Debug.Log($"Error in DisplayTrackByName: {e.Message}");
      SetDefaultValues();
    }
  }

  public void DisplaySpecificTrack(APIManager.Track track)
  {
    if (track != null)
    {
      DisplayTrackInfo(track);
    }
    else
    {
      Debug.Log("Track object is null");
      DisplayErrorMessage("Invalid track data");
    }
  }

  private void SetDefaultValues()
  {
    if (trackNameText != null) trackNameText.text = "Default Track";
    if (trackTypeText != null) trackTypeText.text = "Default Type";
    if (trackCityText != null) trackCityText.text = "Default City";
    if (trackCountryText != null) trackCountryText.text = "Default Country";
    if (trackDescriptionText != null) trackDescriptionText.text = "Default track description";
    if (trackLocationText != null) trackLocationText.text = "Lat: 0.000000, Lon: 0.000000";
  }

  private void DisplayErrorMessage(string errorMsg)
  {
    if (trackNameText != null)
      trackNameText.text = errorMsg;

    if (trackTypeText != null) trackTypeText.text = "";
    if (trackLocationText != null) trackLocationText.text = "";
    if (trackDescriptionText != null) trackDescriptionText.text = "";
    if (trackCityText != null) trackCityText.text = "";
    if (trackCountryText != null) trackCountryText.text = "";
  }

  public void RefreshTrackInfo()
  {
    try
    {
      if (apiManager != null)
      {
        apiManager.GetAllTracks(OnTracksLoaded);
      }
      else
      {
        Debug.Log("APIManager is null in RefreshTrackInfo");
        SetDefaultValues();
      }
    }
    catch (System.Exception e)
    {
      Debug.Log($"Error in RefreshTrackInfo: {e.Message}");
      SetDefaultValues();
    }
  }

  public string GetCurrentTrackName()
  {
    return trackName;
  }

  public void OpenRacingLineForCurrentTrack()
  {
    if (homePageNavigation != null)
    {
      string trackName = GetCurrentTrackName();
      homePageNavigation.NavigateToRacingLineWithTrack(trackName);
    }
    else
    {
      Debug.Log("HomePageNavigation reference not set in AnalysisGetInfo");
    }
  }

  public void OpenRacingLineForCurrentTrackAuto()
  {
    if (homePageNavigation != null)
    {
      string trackName = GetCurrentTrackName();
      homePageNavigation.NavigateToRacingLineWithTrack(trackName);
    }
    else
    {
      Debug.Log("HomePageNavigation component not found in scene");
    }
  }

  private void LoadRacingLinePreview()
  {
    if (racingLineButton != null)
    {
      racingLineButton.interactable = true;
    }
    if (racingLinePreview != null)
    {
      racingLinePreview.gameObject.SetActive(true);
      racingLinePreview.InitializeWithTrack(trackName);
    }
    else
    {
      Debug.LogWarning("Racing line preview component not assigned");
    }
  }

  public void RefreshRacingLinePreview()
  {
    LoadRacingLinePreview();
  }

  void Update()
  {
    if (isWaitingForRetry)
    {
      retryTimer += Time.deltaTime;

      if (retryTimer >= retryInterval)
      {
        isWaitingForRetry = false;
        AttemptToLoadTracks();
      }
    }
  }
}