using UnityEngine;
using UnityEngine.UI;
using SFB;
using RainbowArt.CleanFlatUI;
using System.IO;
using System.Collections.Generic;
using System;
using TMPro;
using System.Linq;
public class MotoGP : MonoBehaviour
{

  [Header("UI References")]
  [SerializeField] private GameObject previewImage;
  [SerializeField] private DropdownTransition dropdown;
  [SerializeField] private GameObject DropDownText;
  [SerializeField] private GameObject uploadButton;
  [SerializeField] private GameObject controlPanel;
  [SerializeField] private TextMeshProUGUI recordButtonText;
  [SerializeField] private GameObject confirmationPanel;
  [SerializeField] private TMP_InputField trackNameInput;
  [SerializeField] private GameObject SaveOptionsDialog;
  bool isRecording = false;
  private MotoGPTelemetry.TelemetryRecorder recorder;
  List<int> lapIndexList = new List<int>();
  void Start()
  {
    recorder = new MotoGPTelemetry.TelemetryRecorder();
    if (dropdown != null)
    {
      dropdown.options.Clear();
      dropdown.gameObject.SetActive(false);
    }

    if (DropDownText != null)
    {
      DropDownText.SetActive(false);
    }

    if (uploadButton != null)
    {
      uploadButton.SetActive(false);
    }

    if (controlPanel != null)
    {
      controlPanel.SetActive(false);
    }

    if (confirmationPanel != null) confirmationPanel.SetActive(false);
    if (SaveOptionsDialog != null) SaveOptionsDialog.SetActive(false);
  }
  private ExtensionFilter[] extensionFilters = new ExtensionFilter[]
  {
        new ExtensionFilter("csv Files", "csv")
  };

  private string filePath;

  public void OpenImageDialog()
  {
    var paths = StandaloneFileBrowser.OpenFilePanel("Select csv file", "", extensionFilters, false);

    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
    {
      if (recorder != null)
      {
        recorder.Dispose();
        recorder = new MotoGPTelemetry.TelemetryRecorder();
      }
      filePath = paths[0];
      controlPanel.SetActive(false);
      LoadCSV(filePath);
    }
  }

  public void RecordMotoGP()
  {
    if (uploadButton != null) uploadButton.SetActive(false);
    if (dropdown != null)
    {
      dropdown.options.Clear();
      dropdown.gameObject.SetActive(false);
    }
    if (DropDownText != null) DropDownText.SetActive(false);

    if (!isRecording)
    {
      if (recordButtonText != null) recordButtonText.text = "Recording game";

      // Start recorder
      recorder?.Dispose(); // dispose previous session
      recorder = new MotoGPTelemetry.TelemetryRecorder();
      recorder.Start();
      isRecording = true;
      return;
    }

    // Stop recording
    isRecording = false;
    if (recordButtonText != null) recordButtonText.text = "Record game";

    List<MotoGPTelemetry.RecordedData> recordedData = recorder.Stop();
    List<int> laps = recorder.getLaps();

    if (laps.Count == 0)
    {
      Debug.Log("No laps recorded.");
      return;
    }
    filePath = null;
    lapIndexList.Clear();
    if (dropdown != null)
    {
      dropdown.options.Clear();
      foreach (int lapIndex in laps)
      {
        dropdown.options.Add(new DropdownTransition.OptionData($"Lap {lapIndex}"));
        lapIndexList.Add(lapIndex);
      }

      dropdown.value = 0;
      dropdown.gameObject.SetActive(true);
      dropdown.RefreshShownValue();

      if (DropDownText != null) DropDownText.SetActive(true);
      if (uploadButton != null) uploadButton.SetActive(true);
    }

    ProcessTelemetryLap(dropdown.value);
  }

  public void ProcessTelemetryLap(int dropdownValue)
  {
    if (recorder == null || recorder.PlayerPath.Count == 0) return;

    int selectedLapIndex = lapIndexList[dropdownValue];

    List<MotoGPTelemetry.RecordedData> lapData = recorder.PlayerPath
        .Where(r => r.CurrentLap == selectedLapIndex)
        .ToList();

    CSVToBinConverter.LoadCSV.PlayerLine playerline = CSVToBinConverter.LoadUDP.Convert(lapData, selectedLapIndex);

    if (playerline == null)
    {
      Debug.LogWarning("PlayerLine data could not be generated for selected lap.");
      return;
    }

    ShowMotoGP showMotoGP = previewImage.GetComponent<ShowMotoGP>();
    if (showMotoGP != null)
    {
      showMotoGP.DisplayPlayerLineData(playerline);
    }
    else
    {
      Debug.LogWarning("ShowMotoGP component not found on previewImage GameObject");
    }

    if (controlPanel != null) controlPanel.SetActive(true);
  }


  public void LoadCSV(string filePath)
  {
    if (!File.Exists(filePath))
    {
      Debug.LogWarning("File does not exist: " + filePath);
      return;
    }

    string[] lines = File.ReadAllLines(filePath);

    if (lines.Length < 2)
    {
      Debug.LogWarning("CSV file is empty or missing data rows.");
      return;
    }

    string[] headers = lines[0].Split('\t');
    int lapIndexCol = System.Array.IndexOf(headers, "lap_number");

    if (lapIndexCol == -1)
    {
      Debug.LogWarning("CSV does not contain a 'lapIndex' column.");
      return;
    }

    HashSet<int> lapIndices = new HashSet<int>();

    for (int i = 1; i < lines.Length; i++)
    {
      if (string.IsNullOrWhiteSpace(lines[i]))
        continue;

      string[] values = lines[i].Split('\t');

      if (lapIndexCol >= values.Length)
      {
        Debug.LogWarning($"Skipping line {i + 1}, not enough columns: {lines[i]}");
        continue;
      }

      string rawValue = values[lapIndexCol].Trim();

      if (int.TryParse(rawValue, out int lapVal))
      {
        if (lapVal != -1)
          lapIndices.Add(lapVal);
      }
    }

    if (lapIndices.Count > 0)
    {
      int lastLap = Mathf.Max(new List<int>(lapIndices).ToArray());
      // lapIndices.Remove(lastLap);
    }

    if (dropdown == null)
    {
      return;
    }

    if (lapIndices.Count == 0)
    {
      return;
    }

    dropdown.options.Clear();
    foreach (int lapIndex in lapIndices)
    {
      dropdown.options.Add(new DropdownTransition.OptionData((lapIndex).ToString()));
      lapIndexList.Add(lapIndex);
    }

    dropdown.value = 0;
    dropdown.gameObject.SetActive(true);
    dropdown.RefreshShownValue();

    if (DropDownText != null)
    {
      DropDownText.SetActive(true);
    }
    ProcessRacingLine();
  }

  public void ProcessRacingLine()
  {
    int selectedLapIndex = lapIndexList[dropdown.value];
    CSVToBinConverter.LoadCSV.PlayerLine playerline = CSVToBinConverter.LoadCSV.Convert(filePath, selectedLapIndex);

    if (playerline == null)
    {
      Debug.LogWarning("PlayerLine data could not be generated.");
      return;
    }

    ShowMotoGP showMotoGP = previewImage.GetComponent<ShowMotoGP>();
    if (showMotoGP != null)
    {
      showMotoGP.DisplayPlayerLineData(playerline);
    }
    else
    {
      Debug.LogWarning("ShowMotoGP component not found on previewImage GameObject");
    }
    if (controlPanel != null)
    {
      controlPanel.SetActive(true);
    }
  }

  public void SaveSession()
  {
    if (recorder == null || recorder.PlayerPath.Count == 0)
    {
      Debug.LogWarning("No recorded session to save.");
      return;
    }

    if (SaveOptionsDialog != null)
      SaveOptionsDialog.SetActive(true);
  }

  public void OnSaveLocally()
  {
    if (SaveOptionsDialog != null)
      SaveOptionsDialog.SetActive(false);

    // Open a save dialog letting the user choose any location
    string filePath = StandaloneFileBrowser.SaveFilePanel(
        "Save CSV",          // Dialog title
        "",                  // Default path
        "MotoGPSession.csv", // Default file name
        "csv"                // Extension
    );

    if (!string.IsNullOrEmpty(filePath))
    {
      recorder.SaveToCSV(filePath);
      Debug.Log("Session saved locally at: " + filePath);
    }
    else
    {
      Debug.Log("Save canceled by user.");
      CloseAllConfirmationPanels();
    }
  }

  public void CloseAllConfirmationPanels()
  {
    if (confirmationPanel != null)
      confirmationPanel.SetActive(false);

    if (SaveOptionsDialog != null)
      SaveOptionsDialog.SetActive(false);

    Debug.Log("All confirmation panels closed.");
  }


  public void OnSaveToCloud()
  {
    if (SaveOptionsDialog != null)
      SaveOptionsDialog.SetActive(false);

    UploadData();
  }

  public void UploadData()
  {
    if (recorder == null || recorder.PlayerPath.Count == 0 || !string.IsNullOrEmpty(filePath))
    {
      if (!string.IsNullOrEmpty(filePath))
      {
        Debug.LogWarning("CSV files cannot be uploaded to the cloud because they contain limited data.");
        return;
      }

      Debug.LogWarning("No recorded session available to upload.");
      return;
    }

    if (confirmationPanel != null)
    {
      confirmationPanel.SetActive(true);

      if (trackNameInput != null)
      {
        string defaultName = GetDefaultTrackName();
        var placeholder = trackNameInput.placeholder as TMP_Text;
        if (placeholder != null) placeholder.text = defaultName;
        trackNameInput.text = "";
      }
    }
  }

  public async void OnConfirmUpload()
  {
    confirmationPanel.SetActive(false);
    string trackName;
    if (!string.IsNullOrEmpty(trackNameInput.text))
    {
      trackName = trackNameInput.text;
    }
    else
    {
      var placeholder = trackNameInput.placeholder as TMP_Text;
      trackName = placeholder != null ? placeholder.text : "Unnamed Track";
    }

    string userName = !string.IsNullOrEmpty(UserManager.Instance.Username)
        ? UserManager.Instance.Username
        : "Postman";

    string fastestLapTime;
    string averageSpeed;
    string topSpeed;
    string vehicleUsed;
    string description = "Recorded telemetry data from Unity";
    if (recorder != null && recorder.PlayerPath.Count > 0)
    {
      int selectedLapIndex = lapIndexList[dropdown.value];
      fastestLapTime = recorder.getFastestLapTime().ToString();
      averageSpeed = recorder.getAverageSpeed(selectedLapIndex - 1).ToString();
      topSpeed = recorder.getTopSpeed(selectedLapIndex - 1).ToString();
      vehicleUsed = recorder.getModel();
    }
    else
    {
      fastestLapTime = "1:45.32";
      averageSpeed = "180";
      topSpeed = "320";
      vehicleUsed = "MotoGP Bike";
    }

    if (recorder != null && recorder.PlayerPath.Count > 0)
    {
      filePath = Path.Combine(Application.streamingAssetsPath, "lastSession.csv");
      recorder.SaveToCSV();
    }
    else
    {
      Debug.LogWarning("Somehow recorder is null not saving");
      return;
    }
    

    var (success, message, data) = await APIManager.Instance.UploadRacingDataAsync(
        filePath,
        trackName,
        userName,
        fastestLapTime,
        averageSpeed,
        topSpeed,
        vehicleUsed,
        description
    );

    if (success)
    {
      Debug.Log($"Upload successful! Server message: {message}");
    }
    else
    {
      Debug.LogWarning($"Upload failed: {message}");
    }
  }

  public void OnCancelUpload()
  {
    confirmationPanel.SetActive(false);
    Debug.Log("Upload cancelled by user.");
  }

  private string GetDefaultTrackName()
  {
    string trackId = "UnknownTrack";

    if (recorder != null && recorder.PlayerPath.Count > 0)
    {
      trackId = recorder.PlayerPath[0].TrackId;
    }
    else if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
    {
      string[] lines = File.ReadAllLines(filePath);
      if (lines.Length > 1)
      {
        Debug.Log(lines[1]);
        string[] values = lines[1].Split('\t');
        int trackIdCol = Array.IndexOf(lines[0].Split('\t'), "trackId");
        if (trackIdCol >= 0 && trackIdCol < values.Length)
        {
          trackId = values[trackIdCol];
        }
      }
    }

    string dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    return $"{trackId}_{dateTime}";
  }
  //Comments have
}