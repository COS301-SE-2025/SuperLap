using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CustomSliders
{
    [ExecuteAlways]
    public class SliderTransition: MonoBehaviour,IPointerDownHandler
    {
        [SerializeField]
        Slider slider;

        [SerializeField]
        Animator animator;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text;

        
        [SerializeField]
        int precision = 1;

        [SerializeField]
        string unit = "";

        bool bDelayedUpdate = false;

        public bool HasText
        {
            get => hasText;
            set
            {
                hasText = value;
                UpdateText();
            }
        }

        void UpdateText()
        {
            if (text != null && text.gameObject.activeSelf != hasText)
            {
                text.gameObject.SetActive(hasText);
            }
            if (hasText && (text != null))
            {
                float useValue = (float)Math.Round((double)slider.value, precision);
                text.text = useValue + " " + unit;
            }
        }

        void Start ()
        {
            if(slider == null)
            {
                slider = GetComponent<Slider>();
            }            
            slider.onValueChanged.AddListener(SliderValueChange);
            SliderValueChange(slider.value);
        }

        void Update()
        {
            if(bDelayedUpdate)
            {
                bDelayedUpdate = false;
                if(text != null)
                {
                    text.gameObject.SetActive(hasText);
                }
                if(slider == null)
                {
                    slider = GetComponent<Slider>();
                }  
                SliderValueChange(slider.value);   
            }
        }

        public void SliderValueChange(float value)
        {
            if (hasText && (text != null))
            {
                float useValue = (float)Math.Round((double)slider.value, precision);
                text.text = useValue + " " + unit;
            }       
        } 

        public void OnPointerDown(PointerEventData eventData)
        {
            if(animator!=null)
            {
                animator.Play("Transition",0,0);  
            }                      
        }

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            bDelayedUpdate = true;           
        }
        #endif
    }
}