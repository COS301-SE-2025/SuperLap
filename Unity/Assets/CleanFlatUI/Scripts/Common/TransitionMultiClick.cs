using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RainbowArt.CleanFlatUI
{
    public class TransitionMultiClick : MonoBehaviour,IPointerClickHandler
    {
        [SerializeField]
        Animator[] animators;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            for(int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                animator.Play("Transition",0,0);
            }    
        }
    }
}