using UnityEngine;

public class DashboardManager : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;

  public void showDashboard()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
  }

  public void showImportTrack()
  {
    dashboard.SetActive(false);
    importTrack.SetActive(true);
  }
  
  
}
