using UnityEngine;

public class DashboardInit : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;
  public GameObject analysis;
  void Awake()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
    analysis.SetActive(false);
  }
}
