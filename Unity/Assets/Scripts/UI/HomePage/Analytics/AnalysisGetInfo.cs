using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class AnalysisGetInfo : MonoBehaviour
{
  [Header("Track Info Display")]
  [SerializeField] private TMP_Text trackNameText;
  [SerializeField] private TMP_Text trackLocationText;
  [SerializeField] private TMP_Text trackTypeText;
  [SerializeField] private TMP_Text trackDescriptionText;
  [SerializeField] private TMP_Text trackCityText;
  [SerializeField] private TMP_Text trackCountryText;
  [SerializeField] private Button racingLineButton;

  [Header("Session Info Display")]
  [SerializeField] private TMP_Text sessionFastestLapTimeText;
  [SerializeField] private TMP_Text sessionAverageSpeedText;
  [SerializeField] private TMP_Text sessionTopSpeedText;
  [SerializeField] private TMP_Text sessionVehicleUsedText;
  [SerializeField] private TMP_Text sessionDateUploadedText;
  [SerializeField] private TMP_Text sessionTotalLapsText;

  [Header("Session Scroll View")]
  [SerializeField] private GameObject defaultSessionPanel;
  [SerializeField] private Transform contentParent;
  [SerializeField] private GameObject loaderPanel;
  [SerializeField] private GameObject backupPanel;

  private ShowRacingLine racingLinePreview;
  private RacingData selectedSession;
  private List<RacingData> allSessions = new();
  private List<GameObject> instantiatedPanels = new();

  private HomePageNavigation homePageNavigation;
  private APIManager apiManager;

  private bool isLoading = false;
  private string trackName;
  private bool manualTrackRequested = false;

  private const string staticUserName = "Postman";

  public void Awake()
  {
    apiManager = APIManager.Instance;
    homePageNavigation = FindAnyObjectByType<HomePageNavigation>();
    racingLinePreview = GetComponentInChildren<ShowRacingLine>();

    if (defaultSessionPanel != null)
      defaultSessionPanel.SetActive(false);

    if (backupPanel != null)
      backupPanel.SetActive(false);

    if (loaderPanel != null)
      loaderPanel.SetActive(false);

    if (racingLinePreview != null)
      racingLinePreview.gameObject.SetActive(false);

    if (racingLineButton != null)
      racingLineButton.interactable = false;
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
      racingLineButton.interactable = false;

    if (racingLinePreview != null)
      racingLinePreview.gameObject.SetActive(false);

    trackName = "";
    isLoading = false;
  }

  public void OnEnable()
  {
    if (!manualTrackRequested && isActiveAndEnabled)
      StartCoroutine(DelayedAttemptToLoadSessions());
  }

  private IEnumerator DelayedAttemptToLoadSessions()
  {
    yield return new WaitForEndOfFrame();
    AttemptToLoadSessions();
  }

  public void OnDisable()
  {
    StopAllCoroutines();
    ResetValues();
    manualTrackRequested = false;
    if (racingLinePreview != null)
      racingLinePreview.gameObject.SetActive(false);
  }

  public void Start()
  {
    ResetValues();
    if (!manualTrackRequested)
      AttemptToLoadSessions();
  }

  public void OpenRacingLineForCurrentTrack()
  {
    if (homePageNavigation != null)
    {
      string trackName = GetCurrentTrackName();
      homePageNavigation.NavigateToRacingLineWithTrackAndSession(trackName, selectedSession);
    }
    else
    {
      Debug.Log("HomePageNavigation reference not set in AnalysisGetInfo");
    }
  }

  private async void AttemptToLoadSessions()
  {
    if (isLoading) return;
    isLoading = true;

    try
    {
      if (loaderPanel != null) loaderPanel.SetActive(true);

      var (success, message, sessions) = await apiManager.GetAllRacingDataAsync();
      isLoading = false;

      if (loaderPanel != null) loaderPanel.SetActive(false);

      OnSessionsLoaded(success, message, sessions);
    }
    catch (System.Exception e)
    {
      isLoading = false;
      Debug.LogError($"Error in AttemptToLoadSessions: {e.Message}");
      DisplayErrorMessage("Error loading sessions");
    }
  }

  private void OnSessionsLoaded(bool success, string message, List<RacingData> sessions)
  {
    if (!success || sessions == null || sessions.Count == 0)
    {
      Debug.LogWarning("No sessions found");
      if (backupPanel != null) backupPanel.SetActive(true);
      return;
    }

    allSessions = sessions.FindAll(s => s.userName == staticUserName);

    if (allSessions.Count == 0)
    {
      Debug.LogWarning($"No sessions found for {staticUserName}");
      DisplayErrorMessage("No sessions for user");
      return;
    }

    allSessions.Sort((a, b) =>
        System.DateTime.Parse(b.dateUploaded).CompareTo(System.DateTime.Parse(a.dateUploaded)));

    ClearAllPanels();

    foreach (var session in allSessions)
      CreateSessionPanel(session);

    selectedSession = allSessions[0];
    DisplaySessionInfo(selectedSession);
    LoadTrackForSelectedSession();
    EnsureCsvForSelectedSession();
  }

  private void CreateSessionPanel(RacingData session)
  {
    if (defaultSessionPanel == null || contentParent == null)
    {
      Debug.LogWarning("Default panel or content parent is missing.");
      return;
    }

    GameObject panelInstance = Instantiate(defaultSessionPanel, contentParent);
    panelInstance.SetActive(true);

    Button panelButton = panelInstance.GetComponent<Button>();
    if (panelButton != null)
    {
      panelButton.onClick.AddListener(() => OnSessionSelected(session));
    }

    TMP_Text infoText = panelInstance.GetComponentInChildren<TMP_Text>();
    if (infoText != null)
    {
      string date = !string.IsNullOrEmpty(session.dateUploaded)
          ? System.DateTime.Parse(session.dateUploaded).ToString("yyyy-MM-dd")
          : "Unknown Date";

      infoText.text = $"{session.trackName} | {date}";
    }

    instantiatedPanels.Add(panelInstance);
  }


  private void OnSessionSelected(RacingData session)
  {
    selectedSession = session;
    DisplaySessionInfo(session);
    LoadTrackForSelectedSession();
    EnsureCsvForSelectedSession();
  }

  private void DisplaySessionInfo(RacingData session)
  {
    if (trackNameText != null)
      trackNameText.text = session.trackName ?? "Unknown Track";

    if (sessionFastestLapTimeText != null)
    {
      sessionFastestLapTimeText.text = $"{session.fastestLapTime} s" ?? "ERROR";
    }

    if (sessionAverageSpeedText != null)
    {
      sessionAverageSpeedText.text = $"{session.averageSpeed} km/h" ?? "ERROR";
    }

    if (sessionTopSpeedText != null)
    {
      sessionTopSpeedText.text = $"{session.topSpeed} km/h" ?? "ERROR";
    }

    if (sessionVehicleUsedText != null)
    {
      sessionVehicleUsedText.text = $"{session.vehicleUsed}" ?? "ERROR";
    }

    if (sessionDateUploadedText != null)
      sessionDateUploadedText.text = !string.IsNullOrEmpty(session.dateUploaded)
          ? System.DateTime.Parse(session.dateUploaded).ToString("yyyy-MM-dd HH:mm")
          : "Unknown Date";

  }

  private async void EnsureCsvForSelectedSession()
  {
    if (selectedSession == null) return;

    if (string.IsNullOrEmpty(selectedSession.csvData) || selectedSession.csvData == "0")
    {
      var (success, message, csvBytes) = await apiManager.DownloadRacingDataCsvAsync(selectedSession._id);
      if (success)
        selectedSession.csvData = System.Convert.ToBase64String(csvBytes);
      else
        Debug.LogWarning($"Failed to download CSV for session {selectedSession._id}: {message}");
    }

    if (sessionTotalLapsText != null)
    {
      sessionTotalLapsText.text = EstimateLapsFromCsv(selectedSession.csvData).ToString();
    }
  }

  private int EstimateLapsFromCsv(string base64Csv)
  {
    try
    {
      if (string.IsNullOrEmpty(base64Csv)) return 0;

      byte[] csvBytes = System.Convert.FromBase64String(base64Csv);
      string csvText = System.Text.Encoding.UTF8.GetString(csvBytes);

      string[] lines = csvText.Split('\n');
      if (lines.Length <= 1) return 0; // no data or only header

      HashSet<string> lapNumbers = new HashSet<string>();

      // Skip header (line 0)
      for (int i = 1; i < lines.Length; i++)
      {
        string line = lines[i].Trim();
        if (string.IsNullOrEmpty(line)) continue;

        string[] columns = line.Split('\t'); // tab-separated CSV
        if (columns.Length < 2) continue;

        string lapNumber = columns[1]; // second column is lap_number
        lapNumbers.Add(lapNumber);
      }

      return lapNumbers.Count;
    }
    catch
    {
      return 0;
    }
  }



  public async void LoadTrackForSelectedSession()
  {
    if (selectedSession == null) return;

    string trackName = selectedSession.trackName;
    var (success, message, track) = await apiManager.GetTrackByNameAsync(trackName);

    if (success && track != null)
    {
      DisplayTrackInfo(track);
    }
    else
    {
      ClearTrackInfo();
      DisplayErrorMessage("Failed to load track data");
    }
  }

  private void DisplayTrackInfo(Track track)
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
      trackLocationText.text = track.location ?? "Location: Unknown";

    trackName = track.name;
    StartCoroutine(DelayedRacingLineInitialization());
  }

  private IEnumerator DelayedRacingLineInitialization()
  {
    yield return new WaitForEndOfFrame();
    LoadRacingLinePreview();
  }

  private void LoadRacingLinePreview()
  {
    if (string.IsNullOrEmpty(trackName)) return;

    if (racingLineButton != null)
      racingLineButton.interactable = true;

    if (racingLinePreview != null)
    {
      if (!racingLinePreview.gameObject.activeSelf)
        racingLinePreview.gameObject.SetActive(true);

      racingLinePreview.InitializeWithTrackAndSession(trackName, selectedSession);
    }
  }

  private void ClearAllPanels()
  {
    foreach (var panel in instantiatedPanels)
    {
      if (panel != null) Destroy(panel);
    }
    instantiatedPanels.Clear();
  }

  private void ClearTrackInfo()
  {
    if (trackNameText != null) trackNameText.text = "Unknown Track";
    if (trackTypeText != null) trackTypeText.text = "";
    if (trackCityText != null) trackCityText.text = "";
    if (trackCountryText != null) trackCountryText.text = "";
    if (trackDescriptionText != null) trackDescriptionText.text = "";
    if (trackLocationText != null) trackLocationText.text = "";

    trackName = "";

    if (racingLineButton != null)
      racingLineButton.interactable = false;

    if (racingLinePreview != null)
      racingLinePreview.gameObject.SetActive(false);
  }


  private void DisplayErrorMessage(string msg)
  {
    if (trackNameText != null)
      trackNameText.text = msg;
  }

  public string GetCurrentTrackName()
  {
    if (!string.IsNullOrEmpty(trackName))
      return trackName;

    if (selectedSession != null && !string.IsNullOrEmpty(selectedSession.trackName))
      return selectedSession.trackName;

    return "Unknown";
  }
}
