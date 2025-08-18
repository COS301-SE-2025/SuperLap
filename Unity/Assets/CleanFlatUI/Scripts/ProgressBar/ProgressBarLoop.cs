using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RainbowArt.CleanFlatUI
{
    public class ProgressBarLoop : MonoBehaviour
    {
        [SerializeField]
        bool hasBackground = true;
        
        [SerializeField]
        Image background;

        Animator animator;

        public bool HasBackground
        {
            get => hasBackground;
            set
            {
                hasBackground = value;
                if (background != null)
                {
                    background.gameObject.SetActive(hasBackground);
                }
            }
        }

        void Start () 
        {
            animator = gameObject.GetComponent<Animator>();
            animator.enabled = false;
            UpdateGUI();
        }

        void UpdateGUI()
        {
            if(animator.enabled == false)
            {
                animator.enabled = true;
            }
            animator.Play("Transition",0,0); 

        }
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            if(background != null)
            {
                background.gameObject.SetActive(hasBackground);
            }   
        }
        #endif
    }
}