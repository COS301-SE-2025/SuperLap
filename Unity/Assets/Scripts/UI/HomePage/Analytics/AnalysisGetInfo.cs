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

  [Header("Track Data")]
  public GameObject trackDataPanel;

  [Header("Racing Line Preview")]
  public Image trackPreviewImage;
  private ShowRacingLine racingLinePreview;

  private HomePageNavigation homePageNavigation;

  private APIManager apiManager;
  private int trackIndex = 0;

  public void Awake()
  {
    apiManager = APIManager.Instance;
    homePageNavigation = FindAnyObjectByType<HomePageNavigation>();
    racingLinePreview = FindAnyObjectByType<ShowRacingLine>();
  }

  public void Start()
  {
    apiManager.GetAllTracks(OnTracksLoaded);
  }



  private void OnTracksLoaded(bool success, string message, List<APIManager.Track> tracks)
  {
    if (!success)
    {
      Debug.LogError($"Failed to load tracks: {message}");
      DisplayErrorMessage("Failed to load track data");
      return;
    }

    if (tracks == null || tracks.Count == 0)
    {
      Debug.LogWarning("No tracks found in the database");
      DisplayErrorMessage("No tracks available");
      return;
    }

    Debug.Log($"Successfully loaded {tracks.Count} tracks");
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

    LoadRacingLinePreview();
  }

  public void DisplayTrackByIndex(int index)
  {
    trackIndex = index;
    apiManager.GetAllTracks(OnTracksLoaded);
  }
  public void DisplayTrackByName(string trackName)
  {
    apiManager.GetTrackByName(trackName, (success, message, track) =>
    {
      if (success && track != null)
      {
        DisplayTrackInfo(track);
      }
      else
      {
        Debug.LogError($"Failed to load track '{trackName}': {message}");
        DisplayErrorMessage($"Track '{trackName}' not found");
      }
    });
  }
  public void DisplaySpecificTrack(APIManager.Track track)
  {
    if (track != null)
    {
      DisplayTrackInfo(track);
    }
    else
    {
      Debug.LogError("Track object is null");
      DisplayErrorMessage("Invalid track data");
    }
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
    apiManager.GetAllTracks(OnTracksLoaded);
  }

  public string GetCurrentTrackName()
  {
    if (trackNameText != null && !string.IsNullOrEmpty(trackNameText.text))
    {
      return trackNameText.text;
    }

    return "Unknown Track";
  }

  public void OpenRacingLineForCurrentTrack()
  {
    if (homePageNavigation != null)
    {
      string trackName = "test1";
      homePageNavigation.NavigateToRacingLineWithTrack(trackName);
      Debug.Log($"Opening racing line for test track: {trackName}");
    }
    else
    {
      Debug.LogError("HomePageNavigation reference not set in AnalysisGetInfo");
    }
  }

  public void OpenRacingLineForCurrentTrackAuto()
  {
    if (homePageNavigation != null)
    {
      string trackName = "test1";
      homePageNavigation.NavigateToRacingLineWithTrack(trackName);
      Debug.Log($"Opening racing line for test track: {trackName}");
    }
    else
    {
      Debug.LogError("HomePageNavigation component not found in scene");
    }
  }

  private void LoadRacingLinePreview()
  {
    if (racingLinePreview != null)
    {
      string trackName = "test1";
      racingLinePreview.InitializeWithTrack(trackName);
      Debug.Log($"Loading racing line preview for: {trackName}");
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
}
