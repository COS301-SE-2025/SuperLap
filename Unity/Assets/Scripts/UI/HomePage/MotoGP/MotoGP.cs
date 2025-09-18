using UnityEngine;
using UnityEngine.UI;
using SFB;
using RainbowArt.CleanFlatUI;
using System.IO;
using System.Collections.Generic;
using System;
using TMPro;
public class MotoGP : MonoBehaviour
{

  [Header("UI References")]
  [SerializeField] private GameObject previewImage;
  [SerializeField] private DropdownTransition dropdown;
  [SerializeField] private GameObject DropDownText;
  [SerializeField] private GameObject processButton;
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

    if (processButton != null)
    {
      processButton.SetActive(false);
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
    if (processButton != null)
    {
      processButton.SetActive(false);
    }

    if (dropdown != null)
    {
      dropdown.options.Clear();
      dropdown.gameObject.SetActive(false);
    }

    if (DropDownText != null)
    {
      DropDownText.SetActive(false);
    }

    if (!isRecording)
    {
      if (recordButtonText != null)
      {
        recordButtonText.text = "Recording game";
      }
      if (recorder != null && isRecording)
      {
          recorder.Stop();
      }
      recorder = new MotoGPTelemetry.TelemetryRecorder();
      recorder.Start();
      isRecording = true;
      return;
    }
    isRecording = false;

    if (recordButtonText != null)
    {
      recordButtonText.text = "Record game";
    }

    List<MotoGPTelemetry.RecordedData> recordedData = recorder.Stop();
    CSVToBinConverter.LoadCSV.PlayerLine playerline = CSVToBinConverter.LoadUDP.Convert(recordedData, 2);

    if (playerline == null)
    {
      Debug.Log("Didnt record nothing");
      return;
    }

    List<int> laps = recorder.getLaps();
    Debug.Log("Laps found: " + laps.Count);
    foreach (int lap in laps)
    {
      Debug.Log("Lap: " + lap);
    }
    Debug.Log("Raceline Count: " + playerline.Raceline.Count);
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
    controlPanel.SetActive(true);
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
    dropdown.options.Clear();
    foreach (int lapIndex in lapIndices)
    {
      dropdown.options.Add(new DropdownTransition.OptionData((lapIndex + 1).ToString()));
      lapIndexList.Add(lapIndex);
    }

    dropdown.value = 0;
    dropdown.gameObject.SetActive(true);
    dropdown.RefreshShownValue();

    if (processButton != null)
    {
      processButton.SetActive(true);
    }
    if (DropDownText != null)
    {
      DropDownText.SetActive(true);
    }
  }

  public void ProcessRacingLine()
  {
    int selectedLapIndex = lapIndexList[dropdown.value];
    CSVToBinConverter.LoadCSV.PlayerLine playerline = CSVToBinConverter.LoadCSV.Convert(filePath, selectedLapIndex);

    Debug.Log("Raceline Count: " + playerline.Raceline.Count);
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
}