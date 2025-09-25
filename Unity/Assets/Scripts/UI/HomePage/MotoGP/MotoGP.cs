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

    lapIndexList.Clear();
    if (dropdown != null)
    {
      dropdown.options.Clear();
      foreach (int lapIndex in laps)
      {
        dropdown.options.Add(new DropdownTransition.OptionData((lapIndex + 1).ToString()));
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
      Debug.LogError("PlayerLine data could not be generated for selected lap.");
      return;
    }

    ShowMotoGP showMotoGP = previewImage.GetComponent<ShowMotoGP>();
    if (showMotoGP != null)
    {
      showMotoGP.DisplayPlayerLineData(playerline);
    }
    else
    {
      Debug.LogError("ShowMotoGP component not found on previewImage GameObject");
    }

    if (controlPanel != null) controlPanel.SetActive(true);
  }


  public void LoadCSV(string filePath)
  {
    if (!File.Exists(filePath))
    {
      Debug.LogError("File does not exist: " + filePath);
      return;
    }

    string[] lines = File.ReadAllLines(filePath);

    if (lines.Length < 2)
    {
      Debug.LogError("CSV file is empty or missing data rows.");
      return;
    }

    string[] headers = lines[0].Split('\t');
    int lapIndexCol = System.Array.IndexOf(headers, "lap_number");

    if (lapIndexCol == -1)
    {
      Debug.LogError("CSV does not contain a 'lapIndex' column.");
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
      dropdown.options.Add(new DropdownTransition.OptionData((lapIndex + 1).ToString()));
      lapIndexList.Add(lapIndex);
    }

    dropdown.value = 0;
    dropdown.gameObject.SetActive(true);
    dropdown.RefreshShownValue();

    if (uploadButton != null)
    {
      uploadButton.SetActive(true);
    }
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
      Debug.LogError("PlayerLine data could not be generated.");
      return;
    }

    ShowMotoGP showMotoGP = previewImage.GetComponent<ShowMotoGP>();
    if (showMotoGP != null)
    {
      showMotoGP.DisplayPlayerLineData(playerline);
    }
    else
    {
      Debug.LogError("ShowMotoGP component not found on previewImage GameObject");
    }
    if (controlPanel != null)
    {
      controlPanel.SetActive(true);
    }
  }
public async void UploadData()
{
    if (string.IsNullOrEmpty(filePath))
    {
        Debug.LogError("No CSV file selected to upload.");
        return;
    }

    // Example telemetry info; you can replace these with actual data from your recorder
    string trackName = "SampleTrack"; // Replace with selected track
    string userName = !string.IsNullOrEmpty(UserManager.Instance.Username) 
    ? UserManager.Instance.Username 
    : "Postman";

    Debug.Log(userName);
     // Replace with actual logged-in user
    string fastestLapTime = "1:45.32"; // Replace with actual data if available
    string averageSpeed = "180";      // Replace with actual data
    string topSpeed = "320";          // Replace with actual data
    string vehicleUsed = "MotoGP Bike"; // Replace if needed
    string description = "Recorded telemetry data from Unity";

    // Call APIManager
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
        Debug.LogError($"Upload failed: {message}");
    }
}

}