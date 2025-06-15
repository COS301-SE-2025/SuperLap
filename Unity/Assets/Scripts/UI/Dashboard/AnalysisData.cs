using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AnalysisData : MonoBehaviour
{
  public TMP_Text trackDataText;
  public Image trackImage;
  public DashboardInit dashboardInit; // Assign in Inspector

  void OnEnable()
  {
    if (dashboardInit == null)
    {
      Debug.LogError("DashboardInit reference not set in AnalysisData.");
      return;
    }
    string trackName = dashboardInit.trackName;
    if (string.IsNullOrEmpty(trackName))
    {
      trackDataText.text = "No track selected.";
      if (trackImage != null) trackImage.sprite = null;
      return;
    }

    // Fetch track data
    APIManager.Instance.GetTrackByName(trackName, (success, message, track) =>
    {
      if (success && track != null)
      {
        trackDataText.text = $"Track ID: {track._id}\nTrack Name: {track.name}\nTrack Type: {track.type}\nTrack City: {track.city}\nTrack Country: {track.country}\nTrack Location: {track.location}\nTrack Uploaded By: {track.uploadedBy}\nTrack Description: {track.description}\nTrack Date Uploaded: {track.dateUploaded}";
      }
      else
      {
        trackDataText.text = $"Failed to load track data: {message}";
      }
    });

    // Fetch track image
    APIManager.Instance.GetTrackImage(trackName, (success, message, texture) =>
    {
      if (success && texture != null)
      {
        trackImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
      }
      else
      {
        trackImage.sprite = null;
      }
    });
  }
}
