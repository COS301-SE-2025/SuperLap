using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowInputFieldUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowInputField modalWindow;
        
        public void Start()
        {
            modalWindow.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);              
        }

        void OnButtonClick()
        {     
            modalWindow.OnConfirm.RemoveAllListeners(); 
            modalWindow.OnConfirm.AddListener(ModalWindowConfirm);
            modalWindow.OnCancel.RemoveAllListeners();   
            modalWindow.OnCancel.AddListener(ModalWindowCancel);      
            modalWindow.ShowModalWindow(); 
        }

        void ModalWindowConfirm(string inputText)
        {            
            Debug.Log("Confirm Button Clicked, text:"+ inputText);
        }

        void ModalWindowCancel(string inputText)
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}