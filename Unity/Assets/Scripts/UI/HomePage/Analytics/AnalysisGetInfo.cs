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

  public Image trackImage;

  private APIManager apiManager;
  private int trackIndex = 0; // Which track to display (0 = first track)

  public void Awake()
  {
    apiManager = APIManager.Instance;
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

    // Display the first track (or specific track based on trackIndex)
    if (trackIndex < tracks.Count)
    {
      DisplayTrackInfo(tracks[trackIndex]);
    }
    else
    {
      // If trackIndex is out of range, display the first track
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
        // Parse latitude and longitude from location string
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
            trackLocationText.text = track.location; // Show raw location if parsing fails
          }
        }
        else
        {
          trackLocationText.text = track.location; // Show raw location if format is unexpected
        }
      }
      else
      {
        trackLocationText.text = "Location: Unknown";
      }
    }

    if (trackImage != null && !string.IsNullOrEmpty(track.name))
    {
      LoadTrackImage(track.name);
    }

    Debug.Log($"Displaying track info for: {track.name}");
  }

  private void LoadTrackImage(string trackName)
  {
    apiManager.GetTrackImage(trackName, (success, message, texture) =>
    {
      if (success && texture != null)
      {
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        trackImage.sprite = sprite;
        Debug.Log($"Successfully loaded image for track: {trackName}");
      }
      else
      {
        Debug.LogWarning($"Failed to load image for track {trackName}: {message}");
      }
    });
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

  // Method to display a specific track object directly
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
    // Display error in the track name field as fallback
    if (trackNameText != null)
      trackNameText.text = errorMsg;

    if (trackTypeText != null) trackTypeText.text = "";
    if (trackLocationText != null) trackLocationText.text = "";
    if (trackDescriptionText != null) trackDescriptionText.text = "";
    if (trackCityText != null) trackCityText.text = "";
    if (trackCountryText != null) trackCountryText.text = "";
  }

  // Public method to refresh the displayed track
  public void RefreshTrackInfo()
  {
    apiManager.GetAllTracks(OnTracksLoaded);
  }
}
