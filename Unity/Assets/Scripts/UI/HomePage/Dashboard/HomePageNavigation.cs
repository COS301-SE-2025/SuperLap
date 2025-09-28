using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

using RainbowArt.CleanFlatUI;
public class HomePageNavigation : MonoBehaviour
{
  [Header("Pages")]
  public GameObject dashboardPage;
  public GameObject uploadPage;
  public GameObject galleryPage;
  public GameObject analysisPage;
  public GameObject racingLinePage;
  public GameObject teamPage;
  public GameObject supportPage;
  public GameObject motoGPPage;
  public GameObject AISettingsPage;

  [Header("Loader")]
  [SerializeField] private GameObject LoaderPanel;

  [Header("Help Tooltips")]
  [SerializeField] private Tooltip[] tooltips;

  [Header("Sidebar")]
  public GameObject activePage;
  private int pageButtonHeight = 120;
  private int pageButtonGap = 30;
  private int activePageIndex = 0;

  [Header("Animation")]
  public float transitionDuration = 0.3f;
  private float targetTopPosition;
  private float targetBottomPosition;
  private Coroutine sidebarCoroutine = null;
  private Coroutine tooltipCoroutine = null;

  void Awake()
  {
    NavigateToDashboard();
    if (tooltips == null || tooltips.Length == 0)
      tooltips = GetComponentsInChildren<Tooltip>(true);

    if (LoaderPanel != null)
    {
      LoaderPanel.SetActive(false);
    }

    hideSupportPopups();
  }

  public void NavigateToDashboard()
  {
    activePageIndex = 0;
    UpdateActivePagePosition();
    dashboardPage.SetActive(true);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void GoToScene(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
  }

  public void NavigateToUpload()
  {
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(true);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToGallery()
  {
    activePageIndex = 1;
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(true);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToAnalysis()
  {
    activePageIndex = 2;
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(true);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToTeam()
  {
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(true);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToSupport()
  {
    activePageIndex = 3;
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(true);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToRacingLine()
  {
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(true);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToMotoGP()
  {
    activePageIndex = 4;
    UpdateActivePagePosition();
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(true);
    AISettingsPage.SetActive(false);
  }

  public void NavigateToAISettingsPage()
  {
    activePageIndex = 5;
    UpdateActivePagePosition();

    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    AISettingsPage.SetActive(true);
  }

  public void NavigateToRacingLineWithTrack(string trackName)
  {
    NavigateToRacingLine();

    if (racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = racingLinePage.GetComponentInChildren<ShowRacingLine>();
      if (racingLineComponent != null)
      {
        racingLineComponent.InitializeWithTrack(trackName);
      }
    }
  }

  public void InitializeWithSession(RacingData selectedSession)
  {
    NavigateToRacingLine();

    if (racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = racingLinePage.GetComponentInChildren<ShowRacingLine>();
      if (racingLineComponent != null)
      {
        racingLineComponent.InitializeWithSession(selectedSession);
      }
    }
  }

  public void NavigateToPage(int pageIndex)
  {
    activePageIndex = pageIndex;
    UpdateActivePagePosition();
    if (pageIndex == 0)
    {
      NavigateToDashboard();
    }
    else if (pageIndex == 1)
    {
      NavigateToGallery();
    }
    else if (pageIndex == 2)
    {
      NavigateToAnalysis();
    }
    else if (pageIndex == 3)
    {
      NavigateToTeam();
    }
    else if (pageIndex == 4)
    {
      NavigateToSupport();
    }
    else if (pageIndex == 5)
    {
      NavigateToMotoGP();
    }
  }

  private void UpdateActivePagePosition()
  {
    hideSupportPopups();
    if (activePage != null)
    {
      RectTransform activePageRect = activePage.GetComponent<RectTransform>();
      if (activePageRect != null)
      {
        activePageRect.anchorMin = new Vector2(0, 1);
        activePageRect.anchorMax = new Vector2(1, 1);

        targetTopPosition = -(activePageIndex * (pageButtonHeight + pageButtonGap));
        targetBottomPosition = targetTopPosition - pageButtonHeight;

        if (sidebarCoroutine != null)
          StopCoroutine(sidebarCoroutine);

        sidebarCoroutine = StartCoroutine(MoveActivePage(activePageRect, targetTopPosition, targetBottomPosition));
      }
    }
  }

  private IEnumerator MoveActivePage(RectTransform rect, float targetTop, float targetBottom)
  {
    float elapsed = 0f;

    Vector2 startMin = rect.offsetMin;
    Vector2 startMax = rect.offsetMax;

    Vector2 endMin = new Vector2(startMin.x, targetBottom);
    Vector2 endMax = new Vector2(startMax.x, targetTop);

    while (elapsed < transitionDuration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed / transitionDuration);

      rect.offsetMin = Vector2.Lerp(startMin, endMin, t);
      rect.offsetMax = Vector2.Lerp(startMax, endMax, t);

      yield return null;
    }

    rect.offsetMin = endMin;
    rect.offsetMax = endMax;
  }

  private void hideSupportPopups()
  {
    if (tooltipCoroutine != null)
    {
      StopCoroutine(tooltipCoroutine);
      tooltipCoroutine = null;
    }

    foreach (var tip in tooltips)
    {
      if (tip != null)
      {
        tip.HideTooltip();
      }
    }
  }

  public void ShowSupportPopups()
  {
    if (tooltipCoroutine != null)
    {
      StopCoroutine(tooltipCoroutine);
      tooltipCoroutine = null;
    }
    tooltipCoroutine = StartCoroutine(ShowTooltipsForActivePage());
  }

  private IEnumerator ShowTooltipsForActivePage()
  {
    if (tooltips == null || tooltips.Length == 0)
      yield break;

    (int start, int end) = GetTooltipRangeForPage();

    if (start == -1 || end == -1) yield break;
    if (start > tooltips.Length || end > tooltips.Length) yield break;

    hideSupportPopups();
    for (int i = start; i <= end; i++)
    {
      if (tooltips[i] == null) continue;
      tooltips[i].ShowTooltip();
      yield return new WaitForSeconds(3f);
      tooltips[i].HideTooltip();
    }
  }

  private (int start, int end) GetTooltipRangeForPage()
  {
    bool IsBackupPanelActive(GameObject page)
    {
      if (page == null) return false;
      Transform backup = page.transform.Find("BackupPanel");
      return backup != null && backup.gameObject.activeSelf;
    }

    bool ShouldReturnTooltip(GameObject page)
    {
      return page != null && page.activeSelf && !IsBackupPanelActive(page);
    }

    Transform FindDeepChild(Transform parent, string name)
    {
      foreach (Transform child in parent)
      {
        if (child.name == name)
          return child;

        var result = FindDeepChild(child, name);
        if (result != null)
          return result;
      }
      return null;
    }

    bool IsChildActiveDeep(GameObject parent, string childName)
    {
      if (parent == null) return false;
      var child = FindDeepChild(parent.transform, childName);
      return child != null && child.gameObject.activeSelf;
    }



    if (dashboardPage && dashboardPage.gameObject.activeSelf) return (0, 3);
    if (ShouldReturnTooltip(galleryPage)) return (4, 4);
    if (ShouldReturnTooltip(analysisPage)) return (5, 8);
    if (ShouldReturnTooltip(motoGPPage)) return (9, 10);
    if (IsChildActiveDeep(uploadPage, "UploadImage")) return (11, 15);
    else if (IsChildActiveDeep(uploadPage, "TraceWidthSlider")) return (11, 14);
    else if (uploadPage && uploadPage.activeSelf) return (11, 11);
    return (-1, -1);
  }

}
