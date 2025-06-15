using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackDropdownManager : MonoBehaviour
{
  [SerializeField] private TMP_Dropdown trackDropdown;

  public Image trackImage;

  public GameObject trackLoading;
  public GameObject trackLoadingRacingLine;

  private List<string> trackNames = new List<string>();

  private void Start()
  {
    if (trackLoading != null) trackLoading.SetActive(true);
    if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(true);
    if (trackImage != null) trackImage.gameObject.SetActive(false);
    APIManager.Instance.GetAllTracks((success, message, tracks) =>
    {
      if (success && tracks != null)
      {
        trackDropdown.ClearOptions();

        trackNames.Clear();
        List<string> options = new List<string>();
        foreach (var track in tracks)
        {
          options.Add(track.name);
          trackNames.Add(track.name);
        }

        trackDropdown.AddOptions(options);

        // Show loading indicators and hide the image before loading the first track image
        if (trackLoading != null) trackLoading.SetActive(true);
        if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(true);
        if (trackImage != null) trackImage.gameObject.SetActive(false);

        // Automatically load the image for the first track
        if (trackNames.Count > 0)
          LoadTrackImage(trackNames[0]);
      }
      else
      {
        if (trackLoading != null) trackLoading.SetActive(true);
        if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(true);
        if (trackImage != null) trackImage.gameObject.SetActive(false);
        Debug.Log("Failed to load tracks: " + message);
      }
    });

    trackDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
  }

  private void OnDropdownValueChanged(int index)
  {
    if (index >= 0 && index < trackNames.Count)
    {
      LoadTrackImage(trackNames[index]);
    }
  }

  private void LoadTrackImage(string trackName)
  {
    // Show loading indicators and hide the image while loading
    if (trackLoading != null) trackLoading.SetActive(true);
    if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(true);
    if (trackImage != null) trackImage.gameObject.SetActive(false);

    APIManager.Instance.GetTrackImage(trackName, (success, message, texture) =>
    {
      if (success && texture != null)
      {
        trackImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        if (trackImage != null) trackImage.gameObject.SetActive(true);
        if (trackLoading != null) trackLoading.SetActive(false);
        if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(false);
      }
      else
      {
        Debug.Log("Failed to load track image: " + message);
        if (trackImage != null) trackImage.gameObject.SetActive(false);
        if (trackLoading != null) trackLoading.SetActive(true);
        if (trackLoadingRacingLine != null) trackLoadingRacingLine.SetActive(true);
      }
    });
  }
}
