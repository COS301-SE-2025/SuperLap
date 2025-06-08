using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardManager : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;

  public GameObject Analysis;

  public void showDashboard()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
    Analysis.SetActive(false);
  }

  public void showImportTrack()
  {
    dashboard.SetActive(false);
    importTrack.SetActive(true);
    Analysis.SetActive(false);
  }

  public void showAnalysis()
  {
    dashboard.SetActive(false);
    importTrack.SetActive(false);
    Analysis.SetActive(true);
  }
}
