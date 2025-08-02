using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UserInput : MonoBehaviour
{
  public List<Selectable> elements;  // Buttons, InputFields, TMP_InputFields
  private int currentIndex = 0;

  void Start()
  {
    if (elements == null || elements.Count == 0)
    {
      Debug.LogWarning("No UI elements assigned.");
      return;
    }

    currentIndex = 0;
    elements[currentIndex].Select();
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Tab))
    {
      MoveToNextElement();
    }

    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
    {
      HandleEnterOnCurrentElement();
    }
  }

  void MoveToNextElement()
  {
    currentIndex = (currentIndex + 1) % elements.Count;
    elements[currentIndex].Select();
  }

  void HandleEnterOnCurrentElement()
  {
    Selectable current = elements[currentIndex];

    // Handle input field save
    TMP_InputField tmpInput = current.GetComponent<TMP_InputField>();
    InputField input = current.GetComponent<InputField>();

    if (tmpInput != null)
    {
      Debug.Log("Saved TMP input: " + tmpInput.text);
    }
    else if (input != null)
    {
      Debug.Log("Saved InputField: " + input.text);
    }
    else
    {
      // If it's a button or something else with onClick
      Button btn = current.GetComponent<Button>();
      if (btn != null)
      {
        btn.onClick.Invoke();
      }
    }

    // Move to next
    MoveToNextElement();
  }
}
