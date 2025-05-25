using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManagement : MonoBehaviour
{
  public GameObject MainMenuCanvas;
  public GameObject LoginCanvas;
  public GameObject RegisterCanvas;
  public GameObject MainCamera;

  public GameObject ErrorMessage;

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
    if (isTransitioningToRegister)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-10, 4, 4), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0,0,10));
      if(MainCamera.transform.position == new Vector3(-10, 4, 4))
      {
        isTransitioningToRegister = false;
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
    RegisterCanvas.SetActive(false);
    LoginCanvas.SetActive(true);
    isTransitioningToLogin = true;
  }

  public void transitionToMainMenu()
  {
    MainMenuCanvas.SetActive(true);
    LoginCanvas.SetActive(false);
    RegisterCanvas.SetActive(false);
    isTransitioningToMainMenu = true;
  }

  public void transitionToRegister()
  {
    MainMenuCanvas.SetActive(false);
    LoginCanvas.SetActive(false);
    RegisterCanvas.SetActive(true);
    isTransitioningToRegister = true;
  }
  
  public void exitGame()
  {
    Application.Quit();
    Debug.Log("Game exited");
  }

  private void ShowErrorMessage(string message)
  {
    if (ErrorMessage != null)
    {
      ErrorMessage.SetActive(true);
      TMP_Text errorText = ErrorMessage.GetComponent<TMP_Text>();
      if (errorText != null)
      {
        errorText.text = message;
      }
      }
    }
  }

  public void Login(TMP_InputField usernameInputField)
  {
    if (usernameInputField != null)
    {
      string username = usernameInputField.text;
      if (!string.IsNullOrEmpty(username.Trim()))
      {
        Debug.Log("Login attempt with username: " + username);
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();
        goToScene("Dashboard");
      }
      else
      {
        ShowErrorMessage("Username cannot be empty!");
      }
    }
    else
    {
      ShowErrorMessage("Username cannot be empty!");
    }
  }

  public void Register(TMP_InputField usernameInputField, TMP_InputField emailInputField)
  {
    if (usernameInputField != null && emailInputField != null)
    {
      string username = usernameInputField.text;
      string email = emailInputField.text;
      if(!string.IsNullOrEmpty(username.Trim()) && !string.IsNullOrEmpty(email.Trim()))
      {
        Debug.Log("Register attempt with username: " + username);
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.SetString("Email", email);
        PlayerPrefs.Save();
        goToScene("Dashboard");
      }
      else
      {
        ShowErrorMessage("Username and email cannot be empty!");
      }
    }
    else
    {
      ShowErrorMessage("Username and email cannot be empty!");
    }
  }

  public void RegisterUser()
  {
    TMP_InputField[] inputFields = RegisterCanvas.GetComponentsInChildren<TMP_InputField>();
    
    if (inputFields.Length >= 2)
    {
      TMP_InputField usernameField = inputFields[0];
      TMP_InputField emailField = inputFields[1];
      
      Register(usernameField, emailField);
    }
    else
    {
      ErrorMessage.SetActive(true);
      ErrorMessage.GetComponent<TMP_Text>().text = "Missing input fields!";
      Debug.LogError("RegisterCanvas needs at least 2 TMP_InputField components for username and email");
    }
  }
}
