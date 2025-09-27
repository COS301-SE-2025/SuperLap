using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarBubbleDemo : MonoBehaviour
{  
    //The ProgressBarBubble Component.  
    public ProgressBarBubble m_ProgressBar; 
    void Start()
    {         
        //Set the current value.
        m_ProgressBar.CurrentValue = 50; 
        //Set the maximum value.
        m_ProgressBar.MaxValue = 100; 
        //Set whether to show the text.
        m_ProgressBar.HasText = true; 
    }      
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class ProgressBarBubble : MonoBehaviour
    {
        [SerializeField]
        float currentValue = 0f;

        [SerializeField]
        float maxValue = 100.0f;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text;  

        [SerializeField]
        Image foreground;

        [SerializeField]
        RectTransform bubble;
        
        bool bDelayedUpdate = false;

        public float CurrentValue
        {
            get => currentValue;
            set
            {
                if(currentValue == value)
                {
                    return;
                }
                currentValue = value;
                OnValueChanged();
            }
        }
        
        public float MaxValue
        {
            get => maxValue;
            set
            {
                if(maxValue == value)
                {
                    return;
                }
                maxValue = value;
                OnValueChanged();
            }
        }

        public bool HasText
        {
            get => hasText;
            set
            {
                if (hasText == value)
                {
                    return;
                }
                hasText = value;
                UpdateText();
            }
        }

        void OnValueChanged()
        {
            if(maxValue < 0)
            {
                maxValue = 100.0f;
            }
            if(currentValue < 0)
            {
                currentValue = 0f;
            }
            currentValue = Mathf.Clamp(currentValue, 0, maxValue);
            UpdateGUI();
        }

        void Start()
        {
            OnValueChanged();
        }

        void OnEnable()
        {
            OnValueChanged();
        }

        void Update()
        {
            if(bDelayedUpdate)
            {
                bDelayedUpdate = false;
                OnValueChanged();
            }
        }

        void UpdateGUI()
        {
            UpdateForeground();     
            UpdateText(); 
        }

        void UpdateForeground()
        {
            foreground.fillAmount = currentValue / maxValue;
        }

        void UpdateText()
        {
            if (bubble != null && bubble.gameObject.activeSelf != hasText)
            {
                bubble.gameObject.SetActive(hasText);
            }
            if (hasText && (text != null) && (bubble != null))
            {
                text.text = (int)((currentValue/maxValue)*100) + "%";
                float totalWidth = foreground.rectTransform.rect.width;
                float filledWidth = totalWidth * foreground.fillAmount;
                float x = -totalWidth / 2.0f + filledWidth;
                Vector3 bubblePos = bubble.anchoredPosition3D;
                bubblePos.x = x;
                bubble.anchoredPosition3D = bubblePos;
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