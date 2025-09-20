using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SidebarResize : MonoBehaviour, IPointerClickHandler
{
  [Header("Sidebar Settings")]
  public RectTransform sidebar;
  public RectTransform content;

  [Header("Resize Settings")]
  [SerializeField] private float expandedRightValue = -2100f;
  [SerializeField] private float collapsedRightValue = -2390f;
  [SerializeField] private float animationDuration = 0.3f;

  [Header("Button Content Settings")]
  [SerializeField] private bool autoFindButtonTexts = true;
  [SerializeField] private List<GameObject> buttonTextElements = new List<GameObject>();

  private bool isExpanded = true;
  private float lastClickTime = 0f;
  private float doubleClickTimeThreshold = 0.3f;
  private bool isAnimating = false;

  private void Start()
  {
    if (sidebar == null)
      sidebar = GetComponent<RectTransform>();

    if (autoFindButtonTexts)
    {
      FindButtonTextElements();
    }
    SetSidebarAndContentPosition(expandedRightValue);
    SetTextVisibility(isExpanded);
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    float timeSinceLastClick = Time.time - lastClickTime;

    if (timeSinceLastClick <= doubleClickTimeThreshold && !isAnimating)
    {
      ToggleSidebar();
    }

    lastClickTime = Time.time;
  }

  private void ToggleSidebar()
  {
    if (isAnimating) return;

    float targetRightValue = isExpanded ? collapsedRightValue : expandedRightValue;
    StartCoroutine(AnimateSidebarResize(targetRightValue));

    isExpanded = !isExpanded;

    SetTextVisibility(isExpanded);
  }

  private IEnumerator AnimateSidebarResize(float targetRight)
  {
    isAnimating = true;

    float startRight = sidebar.offsetMax.x;
    float startContentLeft = content != null ? content.offsetMin.x : 0f;
    float elapsedTime = 0f;

    float changeAmount = targetRight - startRight;
    float targetContentLeft = startContentLeft + changeAmount;

    while (elapsedTime < animationDuration)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / animationDuration;

      progress = Mathf.SmoothStep(0f, 1f, progress);

      float currentRight = Mathf.Lerp(startRight, targetRight, progress);
      float currentContentLeft = Mathf.Lerp(startContentLeft, targetContentLeft, progress);

      SetSidebarAndContentPosition(currentRight, currentContentLeft);

      yield return null;
    }

    SetSidebarAndContentPosition(targetRight, targetContentLeft);
    isAnimating = false;
  }

  private void SetSidebarAndContentPosition(float sidebarRightValue)
  {
    if (sidebar != null)
    {
      Vector2 offsetMax = sidebar.offsetMax;
      float changeAmount = sidebarRightValue - offsetMax.x;
      offsetMax.x = sidebarRightValue;
      sidebar.offsetMax = offsetMax;

      if (content != null)
      {
        Vector2 contentOffsetMin = content.offsetMin;
        contentOffsetMin.x += changeAmount;
        content.offsetMin = contentOffsetMin;
      }
    }
  }

  private void SetSidebarAndContentPosition(float sidebarRightValue, float contentLeftValue)
  {
    if (sidebar != null)
    {
      Vector2 offsetMax = sidebar.offsetMax;
      offsetMax.x = sidebarRightValue;
      sidebar.offsetMax = offsetMax;
    }

    if (content != null)
    {
      Vector2 contentOffsetMin = content.offsetMin;
      contentOffsetMin.x = contentLeftValue;
      content.offsetMin = contentOffsetMin;
    }
  }

  public void ExpandSidebar()
  {
    if (!isExpanded && !isAnimating)
    {
      ToggleSidebar();
    }
  }

  public void CollapseSidebar()
  {
    if (isExpanded && !isAnimating)
    {
      ToggleSidebar();
    }
  }

  public bool IsExpanded => isExpanded;

  private void FindButtonTextElements()
  {
    buttonTextElements.Clear();
    Text[] textComponents = sidebar.GetComponentsInChildren<Text>(true);
    foreach (Text text in textComponents)
    {
      buttonTextElements.Add(text.gameObject);
    }

    TextMeshProUGUI[] tmpComponents = sidebar.GetComponentsInChildren<TextMeshProUGUI>(true);
    foreach (TextMeshProUGUI tmp in tmpComponents)
    {
      if (!buttonTextElements.Contains(tmp.gameObject))
      {
        buttonTextElements.Add(tmp.gameObject);
      }
    }
  }

  private void SetTextVisibility(bool visible)
  {
    foreach (GameObject textElement in buttonTextElements)
    {
      if (textElement != null)
      {
        textElement.SetActive(visible);
      }
    }
  }
  public void AddTextElement(GameObject textElement)
  {
    if (textElement != null && !buttonTextElements.Contains(textElement))
    {
      buttonTextElements.Add(textElement);
    }
  }
  public void RemoveTextElement(GameObject textElement)
  {
    if (buttonTextElements.Contains(textElement))
    {
      buttonTextElements.Remove(textElement);
    }
  }
  public void RefreshTextElements()
  {
    if (autoFindButtonTexts)
    {
      FindButtonTextElements();
    }
  }
}
