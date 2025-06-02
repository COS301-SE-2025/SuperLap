using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardShowLeaderboard : MonoBehaviour
{
  //Panel for leaderboard
  public GameObject leaderboard;

  public TMP_Text leaderboardButtonText;

  private Vector3 newPosition;

  public float transitionSpeed = 1000f;
  
  private bool showLeaderboard = false;

  public void Start()
  {
    showLeaderboard = false;
    newPosition = new Vector3(106, leaderboard.transform.position.y, leaderboard.transform.position.z);
    leaderboard.transform.position = new Vector3(106, leaderboard.transform.position.y, leaderboard.transform.position.z);
  }

  void Update()
  {
    if(leaderboard.transform.position.x != newPosition.x)
    {
      leaderboard.transform.position = Vector3.MoveTowards(leaderboard.transform.position, newPosition, transitionSpeed * Time.deltaTime);
    }
  }

  public void HideLeaderboard()
  {
    showLeaderboard = false;
    newPosition = new Vector3(106, leaderboard.transform.position.y, leaderboard.transform.position.z);
    leaderboard.transform.position = new Vector3(106, leaderboard.transform.position.y, leaderboard.transform.position.z);
    leaderboardButtonText.text = ">";
  }

  public void transitionLeaderboard()
  {
    showLeaderboard = !showLeaderboard;
    if(showLeaderboard)
    {
      newPosition = new Vector3(460, leaderboard.transform.position.y, leaderboard.transform.position.z);
      leaderboardButtonText.text = "<";
    }
    else
    {
      newPosition = new Vector3(106, leaderboard.transform.position.y, leaderboard.transform.position.z);
      leaderboardButtonText.text = ">";
    }
  }
  
}
