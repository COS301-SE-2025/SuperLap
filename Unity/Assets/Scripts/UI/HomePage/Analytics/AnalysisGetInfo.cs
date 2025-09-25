using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

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

  [Header("Dashboard settings")]
  [SerializeField] bool isDashboard = false;

  private ShowRacingLine racingLinePreview;
  private RacingData selectedSession;
  private List<RacingData> allSessions = new();
  private List<GameObject> instantiatedPanels = new();

  private HomePageNavigation homePageNavigation;
  private APIManager apiManager;

  private bool isLoading = false;
  private string trackName;
  private string staticUserName = "Postman";


  private void Awake()
  {
    apiManager = APIManager.Instance;
    homePageNavigation = FindAnyObjectByType<HomePageNavigation>();
    racingLinePreview = GetComponentInChildren<ShowRacingLine>();

    if (defaultSessionPanel != null) defaultSessionPanel.SetActive(false);
    if (backupPanel != null) backupPanel.SetActive(false);
    if (loaderPanel != null) loaderPanel.SetActive(false);
    if (racingLinePreview != null) racingLinePreview.gameObject.SetActive(false);
    if (racingLineButton != null) racingLineButton.interactable = false;
  }


  private void ResetValues()
  {
    if (trackNameText != null) trackNameText.text = "Loading...";
    if (trackTypeText != null) trackTypeText.text = "";
    if (trackCityText != null) trackCityText.text = "";
    if (trackCountryText != null) trackCountryText.text = "";
    if (trackDescriptionText != null) trackDescriptionText.text = "";
    if (trackLocationText != null) trackLocationText.text = "";

    if (racingLineButton != null) racingLineButton.interactable = false;
    if (racingLinePreview != null) racingLinePreview.gameObject.SetActive(false);

    trackName = "";
    isLoading = false;
  }

  private void OnEnable()
  {
    if (!isLoading)
    {
      if (string.IsNullOrEmpty(UserManager.Instance.Username))
      {
        StartCoroutine(WaitForUsernameAndLoadSessions());
      }
      else
      {
        staticUserName = UserManager.Instance.Username;
        StartCoroutine(LoadSessionsSequentially());
      }
    }
  }

  private IEnumerator WaitForUsernameAndLoadSessions()
  {
    float timeout = 0.5f; // 0.5 seconds
    float timer = 0f;

    while (string.IsNullOrEmpty(UserManager.Instance.Username) && timer < timeout)
    {
      timer += Time.deltaTime;
      yield return null; // wait one frame
    }

    staticUserName = !string.IsNullOrEmpty(UserManager.Instance.Username)
        ? UserManager.Instance.Username
        : "Postman";

    Debug.Log($"Using username: {staticUserName}");

    StartCoroutine(LoadSessionsSequentially());
  }

  private void OnDisable()
  {
    StopAllCoroutines();
    ResetValues();
    if (racingLinePreview != null)
      racingLinePreview.gameObject.SetActive(false);
  }

  private void Start()
  {
    ResetValues();
    if (!isLoading)
      StartCoroutine(LoadSessionsSequentially());
  }

  #region Session Loading

  private IEnumerator LoadSessionsSequentially()
  {
    isLoading = true;
    if (loaderPanel != null) loaderPanel.SetActive(true);

    var task = apiManager.GetAllRacingDataAsync();
    yield return new WaitUntil(() => task.IsCompleted);
    var result = task.Result;

    if (!result.success || result.data == null || result.data.Count == 0)
    {
      if (loaderPanel != null) loaderPanel.SetActive(false);
      if (backupPanel != null) backupPanel.SetActive(true);
      Debug.LogWarning("No racing sessions found.");
      isLoading = false;
      yield break;
    }
    allSessions = result.data.FindAll(s => s.userName == staticUserName);
    if (allSessions.Count == 0)
    {
      if (loaderPanel != null) loaderPanel.SetActive(false);
      if (backupPanel != null) backupPanel.SetActive(true);
      isLoading = false;
      yield break;
    }

    allSessions.Sort((a, b) => DateTime.Parse(b.dateUploaded).CompareTo(DateTime.Parse(a.dateUploaded)));

    if (loaderPanel != null) loaderPanel.SetActive(false);
    if (backupPanel != null) backupPanel.SetActive(false);
    ClearAllPanels();

    if (!isDashboard)
    {
      foreach (var session in allSessions)
        CreateSessionPanel(session);
    }

    selectedSession = allSessions[0];
    DisplaySessionInfo(selectedSession);
    StartCoroutine(LoadRacingDataForSession(selectedSession));

    isLoading = false;
  }

  private void CreateSessionPanel(RacingData session)
  {
    if (defaultSessionPanel == null || contentParent == null) return;

    GameObject panelInstance = Instantiate(defaultSessionPanel, contentParent);
    panelInstance.SetActive(true);

    Button panelButton = panelInstance.GetComponent<Button>();
    if (panelButton != null)
      panelButton.onClick.AddListener(() => OnSessionSelected(session));

    TMP_Text infoText = panelInstance.GetComponentInChildren<TMP_Text>();
    if (infoText != null)
    {
      infoText.text = $"{session.trackName}";
    }

    instantiatedPanels.Add(panelInstance);
  }

  private void OnSessionSelected(RacingData session)
  {
    selectedSession = session;

    if (racingLinePreview != null)
      racingLinePreview.ClearPreview(); // Reset previous session

    DisplaySessionInfo(session);
    StartCoroutine(LoadRacingDataForSessionDelayed(session));
  }

  #endregion

  #region Session Display

  private void DisplaySessionInfo(RacingData session)
  {
    if (sessionFastestLapTimeText != null) sessionFastestLapTimeText.text = $"{session.fastestLapTime} s";
    if (sessionAverageSpeedText != null) sessionAverageSpeedText.text = $"{session.averageSpeed} km/h";
    if (sessionTopSpeedText != null) sessionTopSpeedText.text = $"{session.topSpeed} km/h";
    if (sessionVehicleUsedText != null) sessionVehicleUsedText.text = session.vehicleUsed ?? "N/A";
    if (sessionDateUploadedText != null)
      sessionDateUploadedText.text = !string.IsNullOrEmpty(session.dateUploaded)
          ? DateTime.Parse(session.dateUploaded).ToString("yyyy-MM-dd HH:mm")
          : "Unknown Date";
  }

  #endregion

  #region Track & Raceline

  private IEnumerator LoadRacingDataForSessionDelayed(RacingData session)
  {
    yield return null; // wait one frame for layout to update
    yield return LoadRacingDataForSession(session); // call your original coroutine
  }
  private IEnumerator LoadRacingDataForSession(RacingData session)
  {
    if (session == null)
    {
      Debug.LogWarning("No session available.");
      yield break;
    }

    if (string.IsNullOrEmpty(session.csvData) || session.csvData == "0")
    {
      var task = apiManager.DownloadRacingDataCsvAsync(session._id);
      yield return new WaitUntil(() => task.IsCompleted);

      var result = task.Result;
      if (result.success)
      {
        session.csvData = System.Convert.ToBase64String(result.csvBytes);
      }
      else
      {
        Debug.LogWarning($"Failed to download CSV for session {session._id}: {result.message}");
        yield break;
      }
    }

    string trackId = ExtractTrackIdFromCsv(session.csvData);
    if (string.IsNullOrEmpty(trackId))
    {
      Debug.LogWarning("TrackId not found in CSV");
      yield break;
    }

    yield return LoadTrackMetaDataCoroutine(trackId);

    string binPath = Path.Combine(Application.streamingAssetsPath, $"MotoGPTracks/{trackId}.bin");
    if (!File.Exists(binPath))
    {
      Debug.LogWarning($"Bin file not found: {binPath}");
      yield break;
    }

    RacelineDisplayData binRacelineData = RacelineDisplayImporter.LoadFromBinary(binPath);

    RacelineDisplayData playerlineData = ConvertCsvToPlayerline(session.csvData);

    RacelineDisplayData combinedData = CombineRacelineData(binRacelineData, playerlineData);

    if (racingLinePreview != null)
    {
      racingLinePreview.gameObject.SetActive(true);
      racingLinePreview.InitializeWithRacelineData(combinedData);
    }

    if (sessionTotalLapsText != null)
      sessionTotalLapsText.text = EstimateLapsFromCsv(session.csvData).ToString();
  }

  private IEnumerator LoadTrackMetaDataCoroutine(string trackId)
  {
    var task = LoadTrackMetaData(trackId);
    yield return new WaitUntil(() => task.IsCompleted);
  }

  private async Task LoadTrackMetaData(string trackId)
  {
    if (string.IsNullOrEmpty(trackId)) return;

    var (successAll, messageAll, tracks) = await apiManager.GetAllTracksAsync();
    if (!successAll || tracks == null || tracks.Count == 0)
    {
      Debug.LogWarning("No tracks available from API");
      return;
    }

    Track matchedTrack = tracks.Find(t => !string.IsNullOrEmpty(t.name) && t.name.Contains(trackId));
    if (matchedTrack == null)
    {
      Debug.LogWarning($"No track found matching trackId {trackId}");
      return;
    }

    var (successByName, messageByName, fullTrack) = await apiManager.GetTrackByNameAsync(matchedTrack.name);
    Track trackToDisplay = successByName && fullTrack != null ? fullTrack : matchedTrack;

    DisplayTrackInfo(trackToDisplay);
  }


  private string ExtractTrackIdFromCsv(string base64Csv)
  {
    if (string.IsNullOrEmpty(base64Csv)) return null;

    try
    {
      byte[] bytes = Convert.FromBase64String(base64Csv);
      string text = Encoding.UTF8.GetString(bytes);
      string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
      if (lines.Length < 2) return null;

      string[] columns = lines[1].Split('\t');
      return columns.Length > 0 ? columns[0] : null;
    }
    catch { return null; }
  }

  private RacelineDisplayData ConvertCsvToPlayerline(string base64Csv)
  {
    var racelineData = new RacelineDisplayData();
    if (string.IsNullOrEmpty(base64Csv)) return racelineData;

    try
    {
      byte[] csvBytes = System.Convert.FromBase64String(base64Csv);
      string csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
      string[] lines = csvText.Split('\n');
      if (lines.Length <= 1) return racelineData;

      var playerLine = new List<Vector2>();

      for (int i = 1; i < lines.Length; i++)
      {
        string line = lines[i].Trim();
        if (string.IsNullOrEmpty(line)) continue;

        string[] columns = line.Split('\t');
        if (columns.Length < 4) continue;

        if (float.TryParse(columns[2], out float x) &&
            float.TryParse(columns[3], out float y))
        {
          playerLine.Add(new Vector2(x, y));
        }
      }

      racelineData.PlayerLine = playerLine;
    }
    catch (System.Exception ex)
    {
      Debug.LogError($"Error parsing CSV to playerline: {ex.Message}");
    }

    return racelineData;
  }


  private RacelineDisplayData CombineRacelineData(RacelineDisplayData binData, RacelineDisplayData playerline)
  {
    binData.PlayerLine = playerline.PlayerLine;
    return binData;
  }

  private int EstimateLapsFromCsv(string base64Csv)
  {
    if (string.IsNullOrEmpty(base64Csv)) return 0;

    byte[] bytes = Convert.FromBase64String(base64Csv);
    string text = Encoding.UTF8.GetString(bytes);
    string[] lines = text.Split('\n');
    HashSet<string> laps = new HashSet<string>();

    for (int i = 1; i < lines.Length; i++)
    {
      string line = lines[i].Trim();
      if (string.IsNullOrEmpty(line)) continue;
      string[] cols = line.Split('\t');
      if (cols.Length < 2) continue;
      laps.Add(cols[1]);
    }

    return laps.Count;
  }

  private void DisplayTrackInfo(Track track)
  {
    if (trackNameText != null) trackNameText.text = track.name ?? "Unknown Track";
    if (trackTypeText != null) trackTypeText.text = track.type ?? "Unknown Type";
    if (trackCityText != null) trackCityText.text = track.city ?? "Unknown City";
    if (trackCountryText != null) trackCountryText.text = track.country ?? "Unknown Country";
    if (trackDescriptionText != null) trackDescriptionText.text = track.description ?? "No description available";
    if (trackLocationText != null) trackLocationText.text = track.location ?? "Location: Unknown";

    trackName = track.name;

    if (racingLineButton != null) racingLineButton.interactable = true;
  }

  #endregion

  #region Utility

  private void ClearAllPanels()
  {
    foreach (var panel in instantiatedPanels)
      if (panel != null) Destroy(panel);
    instantiatedPanels.Clear();
  }

  public string GetCurrentTrackName()
  {
    if (!string.IsNullOrEmpty(trackName)) return trackName;
    return selectedSession?.trackName ?? "Unknown";
  }

  public void OpenRacingLineForCurrentTrack()
  {
    if (homePageNavigation != null)
      homePageNavigation.NavigateToRacingLineWithTrackAndSession(GetCurrentTrackName(), selectedSession);
  }

  #endregion
}
