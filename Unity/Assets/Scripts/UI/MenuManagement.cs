using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManagement : MonoBehaviour
{
  public void goToScene(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
    Debug.Log("Scene loaded: " + sceneName);
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
