using UnityEngine;
using UnityEngine.UI;
public class AISettingsPage : MonoBehaviour
{
  [Header("Pages")]
  [SerializeField] private GameObject settingsPage;
  [SerializeField] private GameObject previewPage;


  void Start()
  {
    ViewSettings();
  }

  void OnEnable()
  {
    ViewSettings();
  }

  public void ViewSettings()
  {
    if (settingsPage != null) settingsPage.SetActive(true);
    if (previewPage != null) previewPage.SetActive(false);
  }
  public void ViewPreview()
  {
    if (settingsPage != null) settingsPage.SetActive(false);
    if (previewPage != null) previewPage.SetActive(true);
  }
}