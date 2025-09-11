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

  [Header("Help Tooltips")]
  [SerializeField] private Tooltip[] tooltips;

  [Header("Sidebar")]
  public GameObject activePage;
  private int pageButtonHeight = 120;
  private int pageButtonGap = 30;
  private int activePageIndex = 0;

  [Header("Animation")]
  public float transitionSpeed = 5f;
  private float targetTopPosition;
  private float targetBottomPosition;
  private bool isTransitioning = false;


  private Coroutine tooltipCoroutine = null;

  void Awake()
  {
    NavigateToDashboard();
    if (tooltips == null || tooltips.Length == 0)
      tooltips = GetComponentsInChildren<Tooltip>(true);

    hideSupportPopups();
  }

  void Update()
  {
    if (isTransitioning && activePage != null)
    {
      RectTransform activePageRect = activePage.GetComponent<RectTransform>();
      if (activePageRect != null)
      {
        Vector2 currentOffsetMin = activePageRect.offsetMin;
        Vector2 currentOffsetMax = activePageRect.offsetMax;

        float currentTop = Mathf.Lerp(currentOffsetMax.y, targetTopPosition, transitionSpeed * Time.deltaTime);
        float currentBottom = Mathf.Lerp(currentOffsetMin.y, targetBottomPosition, transitionSpeed * Time.deltaTime);

        activePageRect.offsetMin = new Vector2(currentOffsetMin.x, currentBottom);
        activePageRect.offsetMax = new Vector2(currentOffsetMax.x, currentTop);

        if (Mathf.Abs(currentTop - targetTopPosition) < 0.1f && Mathf.Abs(currentBottom - targetBottomPosition) < 0.1f)
        {
          activePageRect.offsetMin = new Vector2(currentOffsetMin.x, targetBottomPosition);
          activePageRect.offsetMax = new Vector2(currentOffsetMax.x, targetTopPosition);
          isTransitioning = false;
        }
      }
    }
  }

  public void NavigateToDashboard()
  {
    dashboardPage.SetActive(true);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    activePageIndex = 0;
    UpdateActivePagePosition();
  }

  public void GoToScene(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
  }

  public void NavigateToUpload()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(true);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
  }

  public void NavigateToGallery()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(true);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    activePageIndex = 1;
    UpdateActivePagePosition();
  }

  public void NavigateToAnalysis()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(true);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    activePageIndex = 2;
    UpdateActivePagePosition();
  }

  public void NavigateToTeam()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(true);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    UpdateActivePagePosition();
  }

  public void NavigateToSupport()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(true);
    motoGPPage.SetActive(false);
    activePageIndex = 3;
    UpdateActivePagePosition();
  }

  public void NavigateToRacingLine()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(true);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(false);
    UpdateActivePagePosition();
  }

  public void NavigateToMotoGP()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    racingLinePage.SetActive(false);
    teamPage.SetActive(false);
    supportPage.SetActive(false);
    motoGPPage.SetActive(true);
    activePageIndex = 4;
    UpdateActivePagePosition();
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

  public void NavigateToRacingLineWithTrack(APIManager.Track track)
  {
    NavigateToRacingLine();

    if (racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = racingLinePage.GetComponentInChildren<ShowRacingLine>();
      if (racingLineComponent != null)
      {
        racingLineComponent.InitializeWithTrack(track.name);
      }
    }
  }

  public void NavigateToRacingLineFromAnalysis()
  {
    string currentTrackName = "Unknown Track";

    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        currentTrackName = analysisComponent.GetCurrentTrackName();
      }
    }

    NavigateToRacingLineWithTrack(currentTrackName);
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
      NavigateToAnalysisWithTrackIndex(0);
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

  public void NavigateToAnalysisWithTrack(string trackName)
  {
    NavigateToAnalysis();

    if (analysisPage != null)
    {
      // Wait for the analysis page to be fully activated
      StartCoroutine(InitializeAnalysisWithTrackDelayed(trackName));
    }
  }

  private IEnumerator InitializeAnalysisWithTrackDelayed(string trackName)
  {
    // Wait for the next frame to ensure the analysis page is fully active
    yield return null;

    AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>(true);
    if (analysisComponent != null)
    {
      analysisComponent.DisplayTrackByName(trackName);
    }
  }

  public void NavigateToAnalysisWithTrackIndex(int trackIndex)
  {
    NavigateToAnalysis();

    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        analysisComponent.DisplayTrackByIndex(trackIndex);
      }
    }
    activePageIndex = 2;
  }

  public void NavigateToAnalysisWithTrack(APIManager.Track track)
  {
    NavigateToAnalysis();

    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        analysisComponent.DisplaySpecificTrack(track);
      }
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

        isTransitioning = true;
      }
    }
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
      if (tooltips[i] == null)
      {
        continue;
      }
      tooltips[i].ShowTooltip();
      yield return new WaitForSeconds(3f);
      tooltips[i].HideTooltip();
    }
  }

  private (int start, int end) GetTooltipRangeForPage()
  {
    if (dashboardPage != null && dashboardPage.activeSelf) return (0, 2);
    if (galleryPage != null && galleryPage.activeSelf) return (3, 5);
    if (analysisPage != null && analysisPage.activeSelf) return (6, 8);
    if (teamPage != null && teamPage.activeSelf) return (9, 11);
    if (supportPage != null && supportPage.activeSelf) return (12, 14);
    if (motoGPPage != null && motoGPPage.activeSelf) return (15, 17);

    return (-1, -1);
  }

}
