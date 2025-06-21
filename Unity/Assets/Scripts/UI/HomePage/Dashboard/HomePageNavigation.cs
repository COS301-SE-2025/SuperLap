using UnityEngine;
using System.Collections;

public class HomePageNavigation : MonoBehaviour
{
  [Header("Pages")]
  public GameObject dashboardPage;
  public GameObject uploadPage;
  public GameObject galleryPage;
  public GameObject analysisPage;

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
    activePageIndex = 0;
    UpdateActivePagePosition();
  }

  public void NavigateToUpload()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(true);
    galleryPage.SetActive(false);
    analysisPage.SetActive(false);
    UpdateActivePagePosition();
  }

  public void NavigateToGallery()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(true);
    analysisPage.SetActive(false);
    activePageIndex = 1;
    UpdateActivePagePosition();
  }

  public void NavigateToAnalysis()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(false);
    galleryPage.SetActive(false);
    analysisPage.SetActive(true);
    activePageIndex = 2;
    UpdateActivePagePosition();
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
  }

  // Method to navigate to analysis with specific track
  public void NavigateToAnalysisWithTrack(string trackName)
  {
    NavigateToAnalysis();

    // Get the AnalysisGetInfo component and display the specific track
    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        analysisComponent.DisplayTrackByName(trackName);
      }
    }
  }

  // Method to navigate to analysis with track by index
  public void NavigateToAnalysisWithTrackIndex(int trackIndex)
  {
    NavigateToAnalysis();

    // Get the AnalysisGetInfo component and display the specific track
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

  // Method to navigate to analysis with track object
  public void NavigateToAnalysisWithTrack(APIManager.Track track)
  {
    NavigateToAnalysis();

    // Get the AnalysisGetInfo component and display the specific track
    if (analysisPage != null)
    {
      AnalysisGetInfo analysisComponent = analysisPage.GetComponentInChildren<AnalysisGetInfo>();
      if (analysisComponent != null)
      {
        // We'll need to add this method to AnalysisGetInfo
        analysisComponent.DisplaySpecificTrack(track);
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
        // Set anchors to top-left (1,1) so offsets work from the top
        activePageRect.anchorMin = new Vector2(0, 1);
        activePageRect.anchorMax = new Vector2(1, 1);

        // Calculate target positions
        targetTopPosition = -(activePageIndex * (pageButtonHeight + pageButtonGap));
        targetBottomPosition = targetTopPosition - pageButtonHeight;

        // Start the transition
        isTransitioning = true;
      }
    }
  }
}
