using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowArt.CleanFlatUI
{
    public class ToggleText : MonoBehaviour 
    {
        [SerializeField]
        Toggle toggle;

        [SerializeField]
        Animator animator; 

        [SerializeField]
        RectTransform on;

        [SerializeField]
        RectTransform off;

        CanvasGroup canvasGroupOn;
        CanvasGroup canvasGroupOff;

        void Start() 
        {            
            UpdateGUI();
        }

        void UpdateGUI()
        {
            if(animator != null)
            {
                animator.enabled = false;
            } 
            if(toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }
            toggle.onValueChanged.AddListener(ToggleValueChanged);
            if(canvasGroupOn == null)
            {
                canvasGroupOn = on.gameObject.GetComponent<CanvasGroup>();
            } 
            if(canvasGroupOff == null)
            {
                canvasGroupOff = off.gameObject.GetComponent<CanvasGroup>();
            }
            if(toggle.isOn)
            {
                SetCanvasGroupAlpha(canvasGroupOn,1);
                SetCanvasGroupAlpha(canvasGroupOff,0);
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupOn,0);
                SetCanvasGroupAlpha(canvasGroupOff,1);
            }
        }

        void ToggleValueChanged(bool value)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                if(value)
                {
                    animator.Play("On",0,0); 
                }
                else
                {
                    animator.Play("Off",0,0); 
                }  
            }          
        }

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }
    }
}