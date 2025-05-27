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
      TMP_InputField passwordField = inputFields[1];
      if (usernameField != null && !string.IsNullOrEmpty(usernameField.text.Trim()) && passwordField != null && !string.IsNullOrEmpty(passwordField.text.Trim()))
      {
        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();
        APIManager.Instance.LoginUser(username, password, (success, message, user) =>
        {
          if (success)
          {
            // Store user data in PlayerPrefs
            PlayerPrefs.SetString("Username", user.username);
            PlayerPrefs.SetString("Email", user.email);
            PlayerPrefs.SetString("Password", user.password);
            PlayerPrefs.Save();
            
            Debug.Log("Login successful: " + message);
            goToScene("Dashboard");
          }
          else
          {
            // Show error message
            if (errorMessage != null)
            {
              TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
              if (errorText != null)
              {
                errorText.text = message;
              }
            }
            else
            {
              Debug.Log("Error message GameObject is not provided!");
            }
          }
        });
      }
      else
      {
        if (errorMessage != null)
        {
          TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
          if (errorText != null)
          {
            errorText.text = "Username or Passwordcannot be empty!";
          }
        }
        else
        {
          Debug.Log("Error message GameObject is not provided!");
        }
      }
    }
  }

  public void PasswordChange(TMP_InputField passwordField)
  {
    if(passwordField)
    {
      passwordField.contentType = TMP_InputField.ContentType.Password;
    }
  }

  public void RegisterUser(GameObject errorMessage)
  {
    TMP_InputField[] inputFields = RegisterCanvas.GetComponentsInChildren<TMP_InputField>();
    
    if (inputFields.Length >= 2)
    {
      TMP_InputField usernameField = inputFields[0];
      TMP_InputField passwordField = inputFields[1];
      TMP_InputField emailField = inputFields[2];

      if(usernameField != null && !string.IsNullOrEmpty(usernameField.text.Trim()) && 
      emailField != null && !string.IsNullOrEmpty(emailField.text.Trim()) && 
      passwordField != null && !string.IsNullOrEmpty(passwordField.text.Trim()))
      {
        string username = usernameField.text.Trim();
        string email = emailField.text.Trim();
        string password = passwordField.text.Trim();
        APIManager.Instance.RegisterUser(username, email, password, (success, message) =>
        {
          if (success)
          {
            // Store user data in PlayerPrefs
            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.SetString("Email", email);
            PlayerPrefs.SetString("Password", password);
            PlayerPrefs.Save();
            
            Debug.Log("Registration successful: " + message);
            goToScene("Dashboard");
          }
          else
          {
            // Show error message
            if (errorMessage != null)
            {
              TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
              if (errorText != null)
              {
                errorText.text = message;
              }
            }
            else
            {
              Debug.Log("Error message GameObject is not provided!");
            }
          }
        });
      }
      else
      {
        if (errorMessage != null)
        {
          TMP_Text errorText = errorMessage.GetComponent<TMP_Text>();
          if (errorText != null)
          {
            errorText.text = "Username and email and password cannot be empty!";
          }
        }
        else
        {
          Debug.Log("Error message GameObject is not provided!");
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
        Debug.Log("Error message GameObject is not provided!");
      }
    }
  }
 
}