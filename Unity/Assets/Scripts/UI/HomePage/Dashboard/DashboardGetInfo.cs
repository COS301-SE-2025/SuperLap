using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DashboardGetInfo : MonoBehaviour
{

  [Header("LastTrackPanel")]
  public TMP_Text lastTrackNameText;
  public TMP_Text lastTrackLapsText;
  public TMP_Text lastTrackTotalSpeedText;
  public TMP_Text lastTrackDistanceText;
  public Image lastTrackImage;


  private APIManager apiManager;

  void Awake()
  {
    apiManager = APIManager.Instance;
  }
}
