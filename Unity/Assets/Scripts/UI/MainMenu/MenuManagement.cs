using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class MenuManagement : MonoBehaviour
{
  public GameObject MainMenuCanvas;
  public List<TMP_InputField> MainMenuInputs;
  public GameObject LoginCanvas;
  public GameObject LoginError;
  public List<TMP_InputField> LoginInputs;
  public GameObject RegisterCanvas;
  public GameObject RegisterError;
  public List<TMP_InputField> RegisterInputs;
  public GameObject MainCamera;

  public float transitionSpeed = 5.0f;

  private bool isTransitioningToLogin = false;
  private bool isTransitioningToMainMenu = false;
  private bool isTransitioningToRegister = false;

  private bool IsValidEmail(string email)
  {
    string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    return Regex.IsMatch(email, emailPattern);
  }


  enum MenuState
  {
    MainMenu,
    Login,
    Register
  }
  private MenuState currentMenuState = MenuState.MainMenu;
  private int currentInputIndex = 0;


  void Start()
  {
    MainMenuCanvas.SetActive(true);
    LoginCanvas.SetActive(false);
    RegisterCanvas.SetActive(false);
  }

  void Update()
  {
    if (isTransitioningToLogin)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-10, 4, 4), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0, 0, 10));
      if (MainCamera.transform.position == new Vector3(-10, 4, 4))
      {
        isTransitioningToLogin = false;
        isTransitioningToMainMenu = false;
        isTransitioningToRegister = false;
      }
    }
    if (isTransitioningToMainMenu)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(-7, 4, 18), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0, 0, 10));
      if (MainCamera.transform.position == new Vector3(-7, 4, 18))
      {
        isTransitioningToMainMenu = false;
        isTransitioningToLogin = false;
        isTransitioningToRegister = false;
      }
    }
    if (isTransitioningToRegister)
    {
      MainCamera.transform.position = Vector3.MoveTowards(MainCamera.transform.position, new Vector3(10, 8, 18), transitionSpeed * Time.deltaTime);
      MainCamera.transform.LookAt(new Vector3(0, 0, 10));
      if (MainCamera.transform.position == new Vector3(10, 8, 18))
      {
        isTransitioningToRegister = false;
        isTransitioningToLogin = false;
        isTransitioningToMainMenu = false;
      }
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
      if (currentMenuState == MenuState.Login)
      {
        transitionToMainMenu();
      }
      if (currentMenuState == MenuState.Register)
      {
        transitionToMainMenu();
      }
    }

    if (Input.GetKeyDown(KeyCode.KeypadEnter))
    {
      if (currentMenuState == MenuState.Login)
      {
        Login();
      }
      if (currentMenuState == MenuState.Register)
      {
        RegisterUser();
      }
      if (currentMenuState == MenuState.MainMenu)
      {
        transitionToLogin();
      }
    }

    if (Input.GetKeyDown(KeyCode.Tab))
    {
      bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

      switch (currentMenuState)
      {
        case MenuState.MainMenu:
          if (MainMenuInputs.Count > 0)
          {
            currentInputIndex = isShiftHeld
              ? (currentInputIndex - 1 + MainMenuInputs.Count) % MainMenuInputs.Count
              : (currentInputIndex + 1) % MainMenuInputs.Count;
            MainMenuInputs[currentInputIndex].Select();
          }
          break;
        case MenuState.Login:
          if (LoginInputs.Count > 0)
          {
            currentInputIndex = isShiftHeld
              ? (currentInputIndex - 1 + LoginInputs.Count) % LoginInputs.Count
              : (currentInputIndex + 1) % LoginInputs.Count;
            LoginInputs[currentInputIndex].Select();
          }
          break;
        case MenuState.Register:
          if (RegisterInputs.Count > 0)
          {
            currentInputIndex = isShiftHeld
              ? (currentInputIndex - 1 + RegisterInputs.Count) % RegisterInputs.Count
              : (currentInputIndex + 1) % RegisterInputs.Count;
            RegisterInputs[currentInputIndex].Select();
          }
          break;
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
    currentMenuState = MenuState.Login;
    currentInputIndex = 0;
  }

  public void transitionToMainMenu()
  {
    isTransitioningToMainMenu = true;
    isTransitioningToLogin = false;
    isTransitioningToRegister = false;
    MainMenuCanvas.SetActive(true);
    LoginCanvas.SetActive(false);
    RegisterCanvas.SetActive(false);
    currentMenuState = MenuState.MainMenu;
    currentInputIndex = 0;
  }

  public void transitionToRegister()
  {
    isTransitioningToRegister = true;
    isTransitioningToLogin = false;
    isTransitioningToMainMenu = false;
    RegisterCanvas.SetActive(true);
    MainMenuCanvas.SetActive(false);
    LoginCanvas.SetActive(false);
    currentMenuState = MenuState.Register;
    currentInputIndex = 0;
  }

  public void exitGame()
  {
    Application.Quit();
    Debug.Log("Game exited");
  }

  public void OnLoginButtonPressed()
  {
      _ = Login(); // Fire-and-forget
  }
  private async Task Login()
  {
    TMP_InputField[] inputFields = LoginCanvas.GetComponentsInChildren<TMP_InputField>();

    if (inputFields.Length >= 2)
    {
      string username = inputFields[0].text.Trim();
      string password = inputFields[1].text.Trim();

      if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
      {
        var (success, message, user) = await APIManager.Instance.LoginUserAsync(username, password);

        if (success)
        {
          UserManager.Instance.Username = username;
          Debug.Log("Login successful: " + message);
          goToScene("Dashboard");
        }
        else
        {
          if (LoginError != null)
          {
            TMP_Text errorText = LoginError.GetComponent<TMP_Text>();
            if (errorText != null) errorText.text = message;
          }
          else
          {
            Debug.Log("Error message GameObject is not provided!");
          }
        }
      }
      else
      {
        if (LoginError != null)
        {
          TMP_Text errorText = LoginError.GetComponent<TMP_Text>();
          if (errorText != null) errorText.text = "Username or Password cannot be empty!";
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
    if (passwordField)
    {
      passwordField.contentType = TMP_InputField.ContentType.Password;
    }
  }

  public void OnRegisterButtonPressed()
{
    _ = RegisterUser(); // Fire-and-forget
}

  private async Task RegisterUser()
  {
    TMP_InputField[] inputFields = RegisterCanvas.GetComponentsInChildren<TMP_InputField>();

    if (inputFields.Length >= 3)
    {
      string username = inputFields[0].text.Trim();
      string password = inputFields[1].text.Trim();
      string email = inputFields[2].text.Trim();

      if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(email))
      {
        if (!IsValidEmail(email))
        {
          if (RegisterError != null)
          {
            TMP_Text errorText = RegisterError.GetComponent<TMP_Text>();
            if (errorText != null) errorText.text = "Please enter a valid email address!";
          }
          else
          {
            Debug.Log("Error message GameObject is not provided!");
          }
          return;
        }

        var (success, message) = await APIManager.Instance.RegisterUserAsync(username, email, password);

        if (success)
        {
          Debug.Log("Registration successful: " + message);
          goToScene("Login");
        }
        else
        {
          if (RegisterError != null)
          {
            TMP_Text errorText = RegisterError.GetComponent<TMP_Text>();
            if (errorText != null) errorText.text = message;
          }
          else
          {
            Debug.Log("Error message GameObject is not provided!");
          }
        }
      }
      else
      {
        if (RegisterError != null)
        {
          TMP_Text errorText = RegisterError.GetComponent<TMP_Text>();
          if (errorText != null) errorText.text = "Username, Email, and Password cannot be empty!";
        }
        else
        {
          Debug.Log("Error message GameObject is not provided!");
        }
      }
    }
  }
}