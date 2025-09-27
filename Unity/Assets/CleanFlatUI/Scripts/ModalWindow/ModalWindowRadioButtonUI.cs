using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowRadioButtonUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowRadioButton modalWindow;
        
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

        void ModalWindowConfirm(int index)
        {            
            Debug.Log("Confirm Button Clicked, index:"+index);
        }

        void ModalWindowCancel(int index)
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}