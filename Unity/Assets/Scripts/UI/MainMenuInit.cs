using UnityEngine;

public class MainMenuInit : MonoBehaviour
{
  public GameObject mainMenu;
  public GameObject loginMenu;
  public GameObject registerMenu;
  void Awake()
  {
    mainMenu.SetActive(true);
    loginMenu.SetActive(false);
    registerMenu.SetActive(false);
  }
}
