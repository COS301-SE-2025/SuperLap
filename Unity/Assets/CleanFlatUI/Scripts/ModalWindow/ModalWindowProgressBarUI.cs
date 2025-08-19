using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowProgressBarUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowProgressBar modalWindow;

        IEnumerator updateCoroutine;
        
        public void Start()
        {
            modalWindow.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);              
        }

        void OnButtonClick()
        { 
            modalWindow.OnCancel.RemoveAllListeners(); 
            modalWindow.OnCancel.AddListener(ModalWindowCancel);    
            modalWindow.OnFinish.RemoveAllListeners(); 
            modalWindow.OnFinish.AddListener(ModalWindowFinish);
            modalWindow.ShowModalWindow(); 
            UpdateProgress();
        }

        void ModalWindowCancel()
        {          
            if(updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }    
            Debug.Log("Cancel");
        }

        void ModalWindowFinish()
        {            
            Debug.Log("Finish");
        }

        void UpdateProgress()
        {
            if(updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }    
            updateCoroutine = UpdateTransition();              
            StartCoroutine(updateCoroutine);   
        }

        IEnumerator UpdateTransition()
        {
            float curProgress = 0f;
            float maxProgress = 100.0f;  
            while (curProgress <= maxProgress) 
            {
                modalWindow.SetProgress(curProgress);
                curProgress++;              
                yield return new WaitForSeconds(0.1f);
            }      
            modalWindow.FinishProgress();
        }               
    }
}