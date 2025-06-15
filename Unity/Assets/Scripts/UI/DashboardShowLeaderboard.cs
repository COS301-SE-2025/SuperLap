using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class DashboardShowLeaderboard : MonoBehaviour
{
  //Panel for leaderboard
  public GameObject leaderboard;
  private RectTransform rectTransform;
  private float leaderboardWidth;

  public TMP_Text leaderboardButtonText;
  public Button leaderboardButton;

  private Vector3 newPosition;

  public float transitionSpeed = 1000f;

  private bool showLeaderboard = false;

  public Vector3 HiddenPosition;
  public Vector3 VisiblePosition;

  public void Start()
  {
    rectTransform = leaderboard.GetComponent<RectTransform>();
    leaderboardWidth = rectTransform.rect.width;
    Debug.Log("Leaderboard Width: " + leaderboardWidth);
    showLeaderboard = false;
    newPosition = HiddenPosition;
    rectTransform.anchoredPosition = newPosition;
  }

  void Update()
  {
    if (rectTransform.anchoredPosition.x != newPosition.x)
    {
      rectTransform.anchoredPosition = Vector3.MoveTowards(rectTransform.anchoredPosition, newPosition, transitionSpeed * Time.deltaTime);
      if (Mathf.Abs(rectTransform.anchoredPosition.x - newPosition.x) <= 0.01f)
      {
        rectTransform.anchoredPosition = newPosition;
        RefreshUIUnderPointer();
      }
    }
  }

  public void HideLeaderboard()
  {
    showLeaderboard = false;
    newPosition = HiddenPosition;
    rectTransform.anchoredPosition = newPosition;
    leaderboardButtonText.text = ">";
  }

  public void transitionLeaderboard()
  {
    showLeaderboard = !showLeaderboard;
    if (showLeaderboard)
    {
      newPosition = VisiblePosition;
      leaderboardButtonText.text = "<";
    }
    else
    {
      newPosition = HiddenPosition;
      leaderboardButtonText.text = ">";
    }
  }

  private void RefreshUIUnderPointer()
  {
    EventSystem eventSystem = EventSystem.current;
    eventSystem.SetSelectedGameObject(null);
    PointerEventData pointerData = new PointerEventData(eventSystem)
    {
      position = Input.mousePosition
    };
    var raycastResults = new List<RaycastResult>();
    eventSystem.RaycastAll(pointerData, raycastResults);
    foreach (var result in raycastResults)
    {
      ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
      ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
    }
  }
}
