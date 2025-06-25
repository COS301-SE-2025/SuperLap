using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HyperLink : MonoBehaviour, IPointerClickHandler
{
  public void OnPointerClick(PointerEventData eventData)
  {
    TMP_Text textComponent = GetComponent<TMP_Text>();
    if (textComponent != null && eventData.button == PointerEventData.InputButton.Left)
    {
      int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, eventData.position, null);
      if (linkIndex != -1)
      {
        TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();

        if (linkId == "myLink")
        {
          Application.OpenURL("https://cos301-se-2025.github.io/SuperLap/"); // Replace with your URL
        }
      }
    }
  }
}