using UnityEngine;

public class MainMenuInit : MonoBehaviour
{
  public GameObject mainMenu;
  public GameObject loginMenu;
  void Awake()
  {
    mainMenu.SetActive(true);
    loginMenu.SetActive(false);
  }
}
