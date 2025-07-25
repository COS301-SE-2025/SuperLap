using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

  void Awake()
  {
    NavigateToDashboard();
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

        // Check if we're close enough to stop transitioning
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
    UpdateActivePagePosition();
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
    activePageIndex = 3;
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
    activePageIndex = 4;
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
    activePageIndex = 3;
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
  }

  public void NavigateToAnalysisWithTrack(string trackName)
  {
    NavigateToAnalysis();

    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        analysisComponent.DisplayTrackByName(trackName);
      }
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

  public void NavigateToRacingLineWithTrackIndex(int trackIndex)
  {
    NavigateToRacingLine();

    if (racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = racingLinePage.GetComponentInChildren<ShowRacingLine>();
      if (racingLineComponent != null)
      {
        racingLineComponent.InitializeWithTrackByIndex(trackIndex);
      }
    }
  }

  private void UpdateActivePagePosition()
  {
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
}
