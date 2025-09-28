using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GalleryGetInfo : MonoBehaviour
{
  [Header("Getters")]
  [SerializeField] private GameObject defaultPanel;
  public GameObject scrollPanel;

  [Header("Layout Settings")]
  [SerializeField] private RectTransform contentPanel; // This should have GridLayoutGroup

  [Header("Panel Content References")]
  [SerializeField] private TextMeshProUGUI trackNameText;
  [SerializeField] private Image trackImage;

  [Header("Backup Panel")]
  [SerializeField] private GameObject backupPanel;

  [Header("Loader")]
  [SerializeField] private GameObject loaderPanel;

  private APIManager apiManager;
  private List<GameObject> instantiatedPanels = new List<GameObject>();

  public void Awake()
  {
    apiManager = APIManager.Instance;

    if (defaultPanel != null)
      defaultPanel.SetActive(false);

    if (backupPanel != null)
      backupPanel.SetActive(false);

    if (loaderPanel != null)
      loaderPanel.SetActive(false);
  }

  private void Start()
  {
    LoadAllTracks();
  }

  public async void LoadAllTracks()
  {
    if (apiManager == null)
    {
      Debug.Log("APIManager instance not found!");
      backupPanel.SetActive(true);
      return;
    }

    ClearAllPanels();

    if (loaderPanel != null)
      loaderPanel.SetActive(true);

    var (success, message, tracks) = await apiManager.GetAllTracksAsync();

    if (loaderPanel != null)
      loaderPanel.SetActive(false);

    OnTracksLoaded(success, message, tracks);
  }

  private void OnTracksLoaded(bool success, string message, List<Track> tracks)
  {
    if (!success)
    {
      Debug.Log($"Failed to load tracks: {message}");
      backupPanel.SetActive(true);
      scrollPanel.SetActive(false);
      return;
    }

    if (tracks == null || tracks.Count == 0)
    {
      Debug.LogWarning("No tracks found in the database");
      backupPanel.SetActive(true);
      scrollPanel.SetActive(false);
      return;
    }

    foreach (var track in tracks)
    {
      CreateTrackPanel(track);
    }
  }

  private void CreateTrackPanel(Track track)
  {
    if (defaultPanel == null)
    {
      Debug.Log("Default panel is not assigned!");
      return;
    }

    GameObject newPanel = Instantiate(defaultPanel, contentPanel); // 👈 simplified
    newPanel.SetActive(true);

    ConfigureTrackPanel(newPanel, track);
    instantiatedPanels.Add(newPanel);
  }

  private void ConfigureTrackPanel(GameObject panel, Track track)
  {
    Transform contentPanel = panel.transform.Find("Content");
    if (contentPanel == null)
    {
      contentPanel = panel.transform;
    }

    TextMeshProUGUI nameText = contentPanel.Find("TrackNameText")?.GetComponent<TextMeshProUGUI>();
    Image image = contentPanel.Find("TrackImage")?.GetComponent<Image>();
    Button button = panel.GetComponent<Button>() ?? panel.GetComponentInChildren<Button>();

    if (nameText != null)
    {
      nameText.text = track.name ?? "Unknown Track";
    }
    if (image != null && !string.IsNullOrEmpty(track.name))
    {
      LoadTrackImage(track.name, image);
    }

    if (button != null)
    {
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => OnTrackSelected(track));
    }

    panel.name = $"Track_{track.name}";
  }

  private void OnTrackSelected(Track track)
  {
    HomePageNavigation navigation = FindAnyObjectByType<HomePageNavigation>();
    if (navigation != null)
    {
      navigation.NavigateToRacingLineWithTrack(track.name);
    }
    else
    {
      Debug.Log("HomePageNavigation component not found!");
    }
  }

  private async void LoadTrackImage(string trackName, Image targetImage)
  {
    var (success, message, texture) = await apiManager.GetTrackImageAsync(trackName);

    if (success && texture != null)
    {
      Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
      targetImage.sprite = sprite;
    }
    else
    {
      Debug.LogWarning($"Failed to load image for track {trackName}: {message}");
    }
  }

  private void ClearAllPanels()
  {
    foreach (GameObject panel in instantiatedPanels)
    {
      if (panel != null)
      {
        DestroyImmediate(panel);
      }
    }

    instantiatedPanels.Clear();
  }

  public void RefreshGallery()
  {
    LoadAllTracks();
  }
}
