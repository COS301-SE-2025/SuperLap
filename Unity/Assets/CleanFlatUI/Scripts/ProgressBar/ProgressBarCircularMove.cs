using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarCircularMoveDemo : MonoBehaviour
{  
    //The ProgressBarCircularMove Component.
    public ProgressBarCircularMove m_ProgressBar;  
    void Start()
    {
        //Set the current value.
        m_ProgressBar.CurrentValue = 50; 
        //Set the maximum value.
        m_ProgressBar.MaxValue = 100; 
        //Set whether to show the text.
        m_ProgressBar.HasText = true; 
        
        //Set the start position which include four positions such as Top, Bottom, Left, Right.
        m_ProgressBar.CurOrigin = ProgressBarCircularMove.Origin.Top;
        //Set whether to play pattern. 
        m_ProgressBar.PatternPlay = true;      
        //Set the speed of playing pattern.
        m_ProgressBar.PatternSpeed = 1.5f; 
        //Set the direction of the pattern moving.
        //If the origin is Top or Bottom, then the patternOrigin is PatternOriginHorizontal.Left or PatternOriginHorizontal.Right.
        //If the origin is Left or Right, then the patternOrigin is PatternOriginVertical.Top or PatternOriginVertical.Bottom.        
        m_ProgressBar.PatternOrigin = (int)ProgressBarCircularMove.PatternOriginHorizontal.Left;
    }   
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class ProgressBarCircularMove : MonoBehaviour
    {
        [SerializeField]
        public float currentValue = 0f;

        [SerializeField]
        public float maxValue = 100.0f;

        [SerializeField]
        public bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text;    

        [SerializeField]      
        RectTransform foregroundArea;

        public enum  Origin
        {
            Bottom,
            Top,
            Left,
            Right,           
        };

        public enum PatternOriginVertical
        {
            Bottom,
            Top,
        };
        public enum PatternOriginHorizontal
        {
            Left,
            Right,
        };

        [SerializeField]
        RawImage patternImage;   

        [SerializeField]
        RectTransform patternRect;

        [SerializeField]
        Origin origin = Origin.Bottom;

        [SerializeField]
        bool patternPlay = true;

        [SerializeField]
        float patternSpeed = 1.5f;

        [SerializeField]
        int patternOrigin;

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

        public Origin CurOrigin
        {
            get => origin;
            set
            {
                origin = value;
            }
        }

        public bool PatternPlay
        {
            get => patternPlay;
            set
            {
                patternPlay = value;
            }
        }

        public float PatternSpeed
        {
            get => patternSpeed;
            set
            {
                patternSpeed = value;
            }
        }

        public int PatternOrigin
        {
            get => patternOrigin;
            set
            {
                patternOrigin = value;
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
            UpdateGUI();
        }
        
        void Update()
        {
            if(Application.isPlaying)
            {
                UpdateGUI();
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
            UpdatePattern();
            UpdateText();
        }
        
        void UpdatePattern()
        {   
            if(patternPlay && Application.isPlaying)
            {
                switch (origin)
                {
                    case Origin.Bottom:
                    case Origin.Top:
                    {
                        Rect r = patternImage.uvRect;
                        if(patternOrigin == (int)PatternOriginHorizontal.Left)
                        {
                            r.x -= Time.deltaTime * patternSpeed;
                        }
                        else
                        {
                            r.x += Time.deltaTime * patternSpeed;
                        }
                        patternImage.uvRect = r;
                        break;
                    }
                    case Origin.Left:
                    case Origin.Right:
                    {
                        Rect r = patternImage.uvRect;
                        if(patternOrigin == (int)PatternOriginVertical.Bottom)
                        {
                            r.y -= Time.deltaTime * patternSpeed;
                        }
                        else
                        {
                            r.y += Time.deltaTime * patternSpeed;
                        }
                        patternImage.uvRect = r;
                        break;
                    }
                }                
            }
        }

        void  UpdateForeground()
        {
            if(currentValue == 0)
            {
                foregroundArea.gameObject.SetActive(false);
            }
            else
            {
                foregroundArea.gameObject.SetActive(true);
                ResetForegroundOrigon();
                switch (origin)
                {
                    case Origin.Bottom:
                    {
                        UpdateForegroundFromBottom();
                        break;
                    }
                    case Origin.Top:
                    {
                        UpdateForegroundFromTop();
                        break;
                    }
                    case Origin.Left:
                    {

                        UpdateForegroundFromLeft();
                        break;
                    }
                    case Origin.Right:
                    {
                        UpdateForegroundFromRight();
                        break;
                    }
                }
            }            
        }

        void ResetForegroundOrigon()
        {
            patternRect.offsetMax = Vector2.zero;
            patternRect.offsetMin = Vector2.zero;
        }

        void UpdateForegroundFromBottom()
        {
            float maxHeight = foregroundArea.rect.height;
            Vector2 offsetMax = patternRect.offsetMax;
            offsetMax.y = -(maxHeight - maxHeight*(currentValue / maxValue));
            Vector2 offsetMin = patternRect.offsetMin;
            offsetMin.y = -(maxHeight - maxHeight*(currentValue / maxValue));
            patternRect.offsetMax = offsetMax;
            patternRect.offsetMin = offsetMin;
        }

         void UpdateForegroundFromTop()
        {
            float maxHeight = foregroundArea.rect.height;
            Vector2 offsetMax = patternRect.offsetMax;
            offsetMax.y = maxHeight - maxHeight*(currentValue / maxValue);
            Vector2 offsetMin = patternRect.offsetMin;
            offsetMin.y = maxHeight - maxHeight*(currentValue / maxValue);
            patternRect.offsetMax = offsetMax;
            patternRect.offsetMin = offsetMin;
        }

        void UpdateForegroundFromLeft()
        {
            float maxWidth = foregroundArea.rect.width;
            Vector2 offsetMax = patternRect.offsetMax;
            offsetMax.x = -(maxWidth - maxWidth*(currentValue / maxValue));
            Vector2 offsetMin = patternRect.offsetMin;
            offsetMin.x = -(maxWidth - maxWidth*(currentValue / maxValue));
            patternRect.offsetMax = offsetMax;
            patternRect.offsetMin = offsetMin;
        }

        void UpdateForegroundFromRight()
        {
            float maxWidth = foregroundArea.rect.width;
            Vector2 offsetMax = patternRect.offsetMax;
            offsetMax.x = maxWidth - maxWidth*(currentValue / maxValue);
            Vector2 offsetMin = patternRect.offsetMin;
            offsetMin.x = maxWidth - maxWidth*(currentValue / maxValue);
            patternRect.offsetMax = offsetMax;
            patternRect.offsetMin = offsetMin;
        }

        void UpdateText()
        {
            if (text != null && text.gameObject.activeSelf != hasText)
            {
                text.gameObject.SetActive(hasText);
            }
            if (hasText && (text != null))
            {
                text.text = (int)((currentValue/maxValue)*100) + "%";
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

