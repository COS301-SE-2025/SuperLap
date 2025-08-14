using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowContentFitterMultiButtonUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowContentFitterMultiButton modalWindow;
        
        public void Start()
        {
            modalWindow.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);  
        }

        void OnButtonClick()
        {
            modalWindow.OnFirst.RemoveAllListeners(); 
            modalWindow.OnFirst.AddListener(ModalWindowFirst);
            modalWindow.OnSecond.RemoveAllListeners(); 
            modalWindow.OnSecond.AddListener(ModalWindowSecond);
            modalWindow.OnThird.RemoveAllListeners(); 
            modalWindow.OnThird.AddListener(ModalWindowThird);
            modalWindow.OnCancel.RemoveAllListeners();   
            modalWindow.OnCancel.AddListener(ModalWindowCancel);      
            modalWindow.ShowModalWindow(); 
        }

        void ModalWindowFirst()
        {
            Debug.Log("First Button Clicked");
        }

        void ModalWindowSecond()
        {
            Debug.Log("Second Button Clicked");
        }

        void ModalWindowThird()
        {
            Debug.Log("Third Button Clicked");
        }

        void ModalWindowCancel()
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}