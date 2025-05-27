using UnityEngine;
using UnityEngine.SceneManagement;
public class init : MonoBehaviour
{
  void Awake()
  {
    Application.targetFrameRate = 60;
    SceneManager.LoadScene("MainMenu");
  }
}
