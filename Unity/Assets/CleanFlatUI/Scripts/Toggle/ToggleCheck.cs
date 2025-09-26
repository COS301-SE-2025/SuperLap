using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowArt.CleanFlatUI
{
    public class ToggleCheck : MonoBehaviour 
    {
        [SerializeField]
        Toggle toggle;

        [SerializeField]
        Animator animator; 
        
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
        }

        void ToggleValueChanged(bool value)
        {
            if(value && (animator != null))
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