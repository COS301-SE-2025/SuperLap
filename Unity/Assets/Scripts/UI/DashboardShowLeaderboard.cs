using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardShowLeaderboard : MonoBehaviour
{
  //Panel for leaderboard
  public GameObject leaderboard;
  private RectTransform rectTransform;
  private float leaderboardWidth;

  public TMP_Text leaderboardButtonText;

  private Vector3 newPosition;

  public float transitionSpeed = 1000f;
  
  private bool showLeaderboard = false;

  public void Start()
  {
    rectTransform = leaderboard.GetComponent<RectTransform>();
    leaderboardWidth = rectTransform.rect.width;
    Debug.Log("Leaderboard Width: " + leaderboardWidth);
    showLeaderboard = false;
    newPosition = new Vector3(-360 - leaderboardWidth, rectTransform.anchoredPosition.y, rectTransform.transform.position.z);
    rectTransform.anchoredPosition = newPosition;
  }

  void Update()
  {
    if(rectTransform.anchoredPosition.x != newPosition.x)
    {
      rectTransform.anchoredPosition = Vector3.MoveTowards(rectTransform.anchoredPosition, newPosition, transitionSpeed * Time.deltaTime);
    }
  }

  public void HideLeaderboard()
  {
    showLeaderboard = false;
    newPosition = new Vector3(-360 - leaderboardWidth, rectTransform.anchoredPosition.y, 0);
    rectTransform.anchoredPosition = new Vector3(-360, rectTransform.anchoredPosition.y, 0);
    leaderboardButtonText.text = ">";
  }

  public void transitionLeaderboard()
  {
    showLeaderboard = !showLeaderboard;
    if(showLeaderboard)
    {
      newPosition = new Vector3(-360, rectTransform.anchoredPosition.y, 0);
      leaderboardButtonText.text = "<";
    }
    else
    {
      newPosition = new Vector3(-360-leaderboardWidth, rectTransform.anchoredPosition.y, 0);
      leaderboardButtonText.text = ">";
    }
  }
  
}
