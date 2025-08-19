using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ButtonTransition : MonoBehaviour
    {
        Button button;      

        [SerializeField]  
        Animator animator; 
        
        void Start()
        {
            if(button == null)
            {
                button = gameObject.GetComponent<Button>();
            }   
            button.onClick.AddListener(OnButtonClick); 
            if(animator != null)
            {
                animator.enabled = false;               
            } 
        }
        public void OnButtonClick()
        {
            if( animator != null )
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                animator.Play("Transition",0,0);         
            }            
        }
    }
}