using UnityEngine;
using TMPro;

public class DashboardInit : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;
  public GameObject analysis;
  public TMP_Dropdown trackDropdown;
  public string trackName;

  void Awake()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
    analysis.SetActive(false);
  }

  public void OnTrackDropdownChanged(int index)
  {
    if (trackDropdown != null && index >= 0 && index < trackDropdown.options.Count)
    {
      string selectedTrackName = trackDropdown.options[index].text;
      trackName = selectedTrackName;
    }
  }
}
