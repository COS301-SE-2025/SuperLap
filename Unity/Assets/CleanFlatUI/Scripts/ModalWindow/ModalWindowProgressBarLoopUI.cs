using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowProgressBarLoopUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ModalWindowProgressBarLoop modalWindow;

        IEnumerator updateCoroutine;
        
        public void Start()
        {
            modalWindow.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);              
        }

        void OnButtonClick()
        {     
            modalWindow.OnFinish.RemoveAllListeners(); 
            modalWindow.OnFinish.AddListener(ModalWindowFinish);
            modalWindow.ShowModalWindow(); 
            UpdateProgress();
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
            yield return new WaitForSeconds(2.5f);   
            modalWindow.FinishProgress();
        }               
    }
}