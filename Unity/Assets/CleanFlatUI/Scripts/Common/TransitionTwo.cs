using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class TransitionTwo: MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField]
        Animator animator;

        public void OnPointerEnter(PointerEventData eventData)
        {
            animator.Play("Transition",0,0);                        
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            animator.Play("Idle",0,0);                      
        }
    }
}