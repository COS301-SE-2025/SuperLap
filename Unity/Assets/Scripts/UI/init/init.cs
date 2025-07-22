using UnityEngine;
using UnityEngine.SceneManagement;
public class init : MonoBehaviour
{
  void Awake()
  {
    Application.targetFrameRate = 60;
    SceneManager.LoadScene("MainMenu");
    Debug.Log("called");
    var pso = GetComponent<GPUPSO>();
    if (pso != null)
    {
        Debug.Log("Manually calling GPUPSO.Start()");
        pso.ManualRun();  // You'll define this next
    }
  }
}
