using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManagement : MonoBehaviour
{
  public GameObject MainMenuCanvas;
  public GameObject LoginRegisterCanvas;
  public GameObject MainCamera;

  public float transitionSpeed = 5.0f;

  private bool isTransitioningToLogin = false;
  private bool isTransitioningToMainMenu = false;
  void Update()
  {
    if (isTransitioningToLogin)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-10, 4, 4), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(-10, 4, 4))
      {
        isTransitioningToLogin = false;
      }
    }
    if (isTransitioningToMainMenu)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-7, 4, 18), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(-7, 4, 18))
      {
        isTransitioningToMainMenu = false;
      }
    }
  }

  public void goToScene(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
    Debug.Log("Scene loaded: " + sceneName);
  }

  

  public void transitionToLogin()
  {
    MainMenuCanvas.SetActive(false);
    LoginRegisterCanvas.SetActive(true);
    isTransitioningToLogin = true;
  }

  public void transitionToMainMenu()
  {
    MainMenuCanvas.SetActive(true);
    LoginRegisterCanvas.SetActive(false);
    isTransitioningToMainMenu = true;
  }
  
  public void exitGame()
  {
    Application.Quit();
    Debug.Log("Game exited");
  }

  public void Login(TMP_InputField usernameInputField)
  {
    if (usernameInputField != null)
    {
      string username = usernameInputField.text;
      
      // Check if username is not empty
      if (!string.IsNullOrEmpty(username.Trim()))
      {
        Debug.Log("Login attempt with username: " + username);
        // Add your login logic here
        // For example, you might want to:
        // - Validate the username
        // - Save it to PlayerPrefs
        // - Load a new scene
        // - Call an authentication service
        
        // Example: Save username and go to main game scene
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        goToScene("Dashboard");
        
        // Optionally load a scene after successful login
        // goToScene("MainGame");
      }
      else
      {
        Debug.LogWarning("Username cannot be empty!");
        // You might want to show an error message to the user here
      }
    }
    else
    {
      Debug.LogError("Username input field is not assigned!");
    }
  }
}
