using UnityEngine;
using UnityEngine.UI;

public class ErrorPopup : MonoBehaviour
{
  [Header("UI References")]
  [SerializeField] private GameObject Parent;

  public void Hide()
  {
    Parent.gameObject.SetActive(false);
  }
}