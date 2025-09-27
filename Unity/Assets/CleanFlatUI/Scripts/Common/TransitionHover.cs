using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class TransitionHover : MonoBehaviour,IPointerEnterHandler
    {
        [SerializeField]
        Animator animator; 
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            animator.Play("Transition",0,0);            
        }
    }
}