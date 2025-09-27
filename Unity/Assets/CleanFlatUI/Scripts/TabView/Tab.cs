using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class Tab : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler 
    {
        [SerializeField]
        Toggle toggle;

        [SerializeField]
        Animator animator; 
        
        bool isPointerEntered = false;

        void OnEnable() 
        {
            isPointerEntered = false;
            UpdateStatusContent();
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
            if (!toggle.isOn)
            {
                if(isPointerEntered)
                {
                    PlayAnimation(animator, "Hover");
                }
                else
                {
                    PlayAnimation(animator, "Off Init");
                }
            }
        }

        void PlayAnimation(Animator animator, string animStr)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                animator.Play(animStr,0,0);  
            }
        }
        void ResetAnimation(Animator animator)
        {
            if(animator != null)
            {
                animator.enabled = false;
            }
        }
    }
}