using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardManager : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;

  public GameObject analysis;

  public void showDashboard()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
    analysis.SetActive(false);
  }

  public void showImportTrack()
  {
    dashboard.SetActive(false);
    importTrack.SetActive(true);
    analysis.SetActive(false);
  }

  public void showAnalysis()
  {
    dashboard.SetActive(false);
    importTrack.SetActive(false);
    analysis.SetActive(true);
  }
}
