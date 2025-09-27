using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowCheckBoxUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowCheckBox modalWindow;
        
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

        void ModalWindowConfirm(int[] selectedIndexes)
        {
            Debug.Log("Confirm Button Clicked, index: ");
            foreach(int index in selectedIndexes)
            {
                Debug.Log(index +",");                
            }
        }

        void ModalWindowCancel(int[] selectedIndexes)
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}