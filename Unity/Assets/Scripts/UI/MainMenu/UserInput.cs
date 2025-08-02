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
    int startIndex = currentIndex;
    do
    {
      currentIndex = (currentIndex + 1) % elements.Count;
      Selectable next = elements[currentIndex];

      // Skip if inactive or not interactable
      if (next != null &&
          next.gameObject.activeInHierarchy &&
          next.IsInteractable())
      {
        next.Select();
        return;
      }

    } while (currentIndex != startIndex); // Prevent infinite loop if all are inactive
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

      // Move to next
      MoveToNextElement();
      return;
    }
    else if (input != null)
    {
      Debug.Log("Saved InputField: " + input.text);

      // Move to next
      MoveToNextElement();
      return;
    }
  }

  public void SetCurrentElement(Selectable selected)
  {
    int index = elements.IndexOf(selected);
    if (index >= 0)
    {
      currentIndex = index;
      elements[currentIndex].Select();
    }
    else
    {
      Debug.LogWarning("Element not found in list.");
    }
  }

}
