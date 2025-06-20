using UnityEngine;
using System.Collections;

public class HomePageNavigation : MonoBehaviour
{
  [Header("Pages")]
  public GameObject dashboardPage;
  public GameObject uploadPage;

  [Header("Sidebar")]
  public GameObject activePage;

  private int sidebarHeight = 1235;
  private int pageButtonHeight = 115;
  private int pageButtonGap = 25;
  private int activePageIndex = 0;

  [Header("Animation")]
  public float transitionSpeed = 5f;
  private float targetTopPosition;
  private float targetBottomPosition;
  private bool isTransitioning = false;

  void Awake()
  {
    dashboardPage.SetActive(true);
    uploadPage.SetActive(false);
    activePageIndex = 0;
    UpdateActivePagePosition();
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
  }

  public void NavigateToUpload()
  {
    dashboardPage.SetActive(false);
    uploadPage.SetActive(true);
  }

  public void NavigateToPage(int pageIndex)
  {
    activePageIndex = pageIndex;
    UpdateActivePagePosition();
    if (pageIndex == 0)
    {
      NavigateToDashboard();
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
