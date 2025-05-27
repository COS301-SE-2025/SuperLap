using UnityEngine;

public class DashboardInit : MonoBehaviour
{
  public GameObject dashboard;
  public GameObject importTrack;
  void Awake()
  {
    dashboard.SetActive(true);
    importTrack.SetActive(false);
  }
}
