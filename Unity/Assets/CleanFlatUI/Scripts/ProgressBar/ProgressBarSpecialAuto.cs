using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarSpecialAutoDemo : MonoBehaviour
{
    //The ProgressBarSpecialAuto Component. 
    public ProgressBarSpecialAuto m_ProgressBar;
    void Start()
    {
        //Set the minimum value.
        m_ProgressBar.MinValue = 25; 
        //Set the maximum value.
        m_ProgressBar.MaxValue = 100; 
        //Set whether to show the text.
        m_ProgressBar.HasText = false; 
        //Set the speed of the current progress auto changing.
        m_ProgressBar.LoadSpeed = 0.2f; 
        //Set whether the progress increasing or decreasing.
        m_ProgressBar.Forward = true; 
        //Set whether to auto loop.  
        m_ProgressBar.Loop = true; 
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class ProgressBarSpecialAuto : MonoBehaviour
    {
        [SerializeField]
        float minValue = 0f;

        [SerializeField]
        float maxValue = 100.0f;

        [SerializeField]
        [Range(0, 1)]
        float loadSpeed = 0.1f;

        [SerializeField]
        bool forward = true;

        [SerializeField]
        bool loop = true;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text;  

        [SerializeField]                
        RectTransform foreground;

        [SerializeField]
        RectTransform foregroundArea;

        float currentValue = 0f;
        bool bDelayedUpdate = false;

        public float MinValue
        {
            get => minValue;
            set
            {
                if(minValue == value)
                {
                    return;
                }
                minValue = value;
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

        public float LoadSpeed
        {
            get => loadSpeed;
            set 
            { 
                loadSpeed = value; 
            }
        }

        public bool Forward
        {
            get => forward;
            set
            {
                forward = value;
            }
        }

        public bool Loop
        {
            get => loop;
            set
            {
                loop = value;
            }
        }       

        void OnValueChanged()
        {
            if(maxValue < 0)
            {
                maxValue = 100.0f;
            }
            if(minValue < 0)
            {
                minValue = 0f;
            }
            currentValue = Mathf.Clamp(minValue, 0, maxValue);
            UpdateGUI();
        }

        void InitValue()
        {
            if(forward)
            {
                currentValue = minValue;
            }
            else
            {
                currentValue = maxValue;
            }
        }

        void Start()
        {
            InitValue();
            UpdateGUI();
        }

        void Update()
        {
            if(Application.isPlaying)
            {
                if(forward)
                {
                    if (currentValue < maxValue)
                    {
                        currentValue += loadSpeed * (Time.deltaTime * 100);
                        if (currentValue >= maxValue)
                        {
                            currentValue = maxValue;
                        }
                        UpdateGUI();                        
                    }
                    if(loop)
                    {
                        if (currentValue >= maxValue)
                        {
                            currentValue = minValue;
                        }
                    }
                }
                else
                {
                    if (currentValue > minValue)
                    {
                        currentValue -= loadSpeed * (Time.deltaTime * 100);
                        if (currentValue <= minValue)
                        {
                            currentValue = minValue;
                        }
                        UpdateGUI();
                    }
                    if(loop)
                    {
                        if (currentValue <= minValue)
                        {
                            currentValue = maxValue;
                        }
                    }
                }                
            }        
            else
            {
                if(bDelayedUpdate)
                {
                    bDelayedUpdate = false;
                    OnValueChanged();
                } 
            }     
        }

        void UpdateGUI()
        {            
            UpdateForeground();    
            UpdateText();                  
        }

        void UpdateForeground()
        {
            float maxWidth = foregroundArea.rect.width;
            Vector2 offsetMax = foreground.offsetMax;
            offsetMax.x = -(maxWidth - maxWidth*(currentValue / maxValue));
            foreground.offsetMax = offsetMax; 
        }

        void UpdateText()
        {
            if (text != null && text.gameObject.activeSelf != hasText)
            {
                text.gameObject.SetActive(hasText);
            }
            if (hasText && (text != null))
            {
                text.text = Mathf.Floor(currentValue) +"/"+ Mathf.Floor(maxValue);
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
