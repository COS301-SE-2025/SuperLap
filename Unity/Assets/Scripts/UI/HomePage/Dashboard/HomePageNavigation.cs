using UnityEngine;

public class HomePageNavigation : MonoBehaviour
{

  [Header("Pages")]
  public GameObject dashboardPage;
  public GameObject uploadPage;

  void Awake()
  {
    dashboardPage.SetActive(true);
    uploadPage.SetActive(false);
  }

  public void NavigateToDashboard()
  {
    dashboardPage.SetActive(true);
    uploadPage.SetActive(false);
  }

  public void NavigateToUpload()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(true);
  }
}
