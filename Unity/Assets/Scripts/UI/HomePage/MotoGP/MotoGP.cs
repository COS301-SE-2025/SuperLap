using UnityEngine;
using UnityEngine.UI;
using SFB;
using RainbowArt.CleanFlatUI;
using System.IO;
using System.Collections.Generic;
public class MotoGP : MonoBehaviour
{

  [Header("UI References")]
  [SerializeField] private Image previewImage;
  [SerializeField] private DropdownTransition dropdown;

  [SerializeField] private GameObject processButton;

  void Start()
  {
    dropdown.options.Clear();
    dropdown.gameObject.SetActive(false);
    processButton.SetActive(false);
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
      LoadCSV(filePath);
    }
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

    foreach (string header in headers)
    {
      Debug.Log("Header: " + header);
    }
    int lapIndexCol = System.Array.IndexOf(headers, "lapIndex");

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
        lapIndices.Add(lapVal);
      }
    }

    dropdown.options.Clear();
    foreach (int lapIndex in lapIndices)
    {
      dropdown.options.Add(new DropdownTransition.OptionData(lapIndex.ToString()));
    }

    if (dropdown.options.Count > 0)
    {
      dropdown.gameObject.SetActive(true);
      dropdown.value = 0;
      processButton.SetActive(true);
    }
  }

  public void ProcessRacingLine()
  {
    PlayerLineFromCSV.Convert(filePath, dropdown.value);
  }


  // private IEnumerator LoadCSV(string filePath)
  // {
  //   //send to integration
  // }

}