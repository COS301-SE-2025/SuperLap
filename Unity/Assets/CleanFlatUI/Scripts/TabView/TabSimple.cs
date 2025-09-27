using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class TabSimple : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler 
    {
        [SerializeField]
        Toggle toggle;  

        [SerializeField]
        RectTransform checkmark;  

        [SerializeField]   
        RectTransform on;

        [SerializeField]
        RectTransform off;

        CanvasGroup canvasGroupCheckmark;  
        CanvasGroup canvasGroupOn;
        CanvasGroup canvasGroupOff;              
        bool isPointerEntered = false;        

        void OnEnable() 
        {
            isPointerEntered = false;         
            UpdateStatusContent();
        } 

        void initCanvasGroup()
        {
            if(canvasGroupCheckmark == null)
            {
                canvasGroupCheckmark = checkmark.gameObject.GetComponent<CanvasGroup>();
            }
            if(canvasGroupOn == null)
            {
                canvasGroupOn = on.gameObject.GetComponent<CanvasGroup>();
            }
            if(canvasGroupOff == null)
            {
                canvasGroupOff = off.gameObject.GetComponent<CanvasGroup>(); 
            }            
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerEntered = true;
            UpdateStatusContent();
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerEntered = false;
            UpdateStatusContent();
        }

        public void UpdateStatusContent()
        {
            initCanvasGroup();
            if (!toggle.isOn)
            {
                if(isPointerEntered)
                {
                    SetCanvasGroupAlpha(canvasGroupOn, 0f);
                    SetCanvasGroupAlpha(canvasGroupOff, 1.0f);
                    SetCanvasGroupAlpha(canvasGroupCheckmark, 0f);
                }
                else
                {
                    SetCanvasGroupAlpha(canvasGroupOn, 0f);
                    SetCanvasGroupAlpha(canvasGroupOff, 0.4f);
                    SetCanvasGroupAlpha(canvasGroupCheckmark, 0f);
                }
            }
        }  

        public void SetTabOn(bool bOn)
        {
            initCanvasGroup();
            if(bOn)
            {
                SetCanvasGroupAlpha(canvasGroupOn, 1.0f);
                SetCanvasGroupAlpha(canvasGroupOff, 0f);
                SetCanvasGroupAlpha(canvasGroupCheckmark, 1.0f);
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupOn, 0f);
                SetCanvasGroupAlpha(canvasGroupOff, 0.4f);
                SetCanvasGroupAlpha(canvasGroupCheckmark, 0f);
            }
        }

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }      
    }
}