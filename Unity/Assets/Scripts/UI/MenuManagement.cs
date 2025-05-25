using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManagement : MonoBehaviour
{
  public GameObject MainMenuCanvas;
  public GameObject LoginCanvas;
  public GameObject RegisterCanvas;
  public GameObject MainCamera;

  public float transitionSpeed = 5.0f;

  private bool isTransitioningToLogin = false;
  private bool isTransitioningToMainMenu = false;
  private bool isTransitioningToRegister = false;
  void Update()
  {
    if (isTransitioningToLogin)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-10, 4, 4), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(-10, 4, 4))
      {
        isTransitioningToLogin = false;
        isTransitioningToMainMenu = false;
        isTransitioningToRegister = false;
      }
    }
    if (isTransitioningToMainMenu)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-7, 4, 18), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(-7, 4, 18))
      {
        isTransitioningToMainMenu = false;
        isTransitioningToLogin = false;
        isTransitioningToRegister = false;
      }
    }
    if (isTransitioningToRegister)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(10, 8, 18), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(10, 8, 18))
      {
        isTransitioningToRegister = false;
        isTransitioningToLogin = false;
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
    isTransitioningToLogin = true;
    isTransitioningToMainMenu = false;
    isTransitioningToRegister = false;
    LoginCanvas.SetActive(true);
    MainMenuCanvas.SetActive(false);
    RegisterCanvas.SetActive(false);

  }

  public void transitionToMainMenu()
  {
    isTransitioningToMainMenu = true;
    isTransitioningToLogin = false;
    isTransitioningToRegister = false;
    MainMenuCanvas.SetActive(true);
    LoginCanvas.SetActive(false);
    RegisterCanvas.SetActive(false);
  }

  public void transitionToRegister()
  {
    isTransitioningToRegister = true;
    isTransitioningToLogin = false;
    isTransitioningToMainMenu = false;
    RegisterCanvas.SetActive(true);
    MainMenuCanvas.SetActive(false);
    LoginCanvas.SetActive(false);
  }
  
  public void exitGame()
  {
    Application.Quit();
    Debug.Log("Game exited");
  }

  public void Login(GameObject errorMessage)
  {
    TMP_InputField[] inputFields = LoginCanvas.GetComponentsInChildren<TMP_InputField>();
    
    if (inputFields.Length >= 1)
    {
      TMP_InputField usernameField = inputFields[0];
      if (usernameField != null && !string.IsNullOrEmpty(usernameField.text.Trim()))
      {
        Debug.Log("Login attempt with username: " + usernameField.text);
        PlayerPrefs.SetString("Username", usernameField.text);
        PlayerPrefs.Save();
        goToScene("Dashboard");
      }
      else
      {
        if (errorMessage != null)
        {
          TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
          if (errorText != null)
          {
            errorText.text = "Username cannot be empty!";
          }
        }
        else
        {
          Debug.LogError("Error message GameObject is not provided!");
        }
      }
    }
  }

  public void RegisterUser(GameObject errorMessage)
  {
    TMP_InputField[] inputFields = RegisterCanvas.GetComponentsInChildren<TMP_InputField>();
    
    if (inputFields.Length >= 2)
    {
      TMP_InputField usernameField = inputFields[0];
      TMP_InputField emailField = inputFields[1];
      
      if(usernameField != null && emailField != null && !string.IsNullOrEmpty(usernameField.text.Trim()) && !string.IsNullOrEmpty(emailField.text.Trim()))
      {
        Debug.Log("Register attempt with username: " + usernameField.text + " and email: " + emailField.text);
        PlayerPrefs.SetString("Username", usernameField.text);
        PlayerPrefs.SetString("Email", emailField.text);
        PlayerPrefs.Save();
        goToScene("Dashboard");
      }
      else
      {
        if (errorMessage != null)
        {
          TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
          if (errorText != null)
          {
            errorText.text = "Username and email cannot be empty!";
          }
        }
        else
        {
          Debug.LogError("Error message GameObject is not provided!");
        }
      }
    }
    else
    {
      if (errorMessage != null)
      {
        TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
        if (errorText != null)
        {
          errorText.text = "Username and email cannot be empty!";
        }
      }
      else
      {
        Debug.LogError("Error message GameObject is not provided!");
      }
    }
  }
 
}