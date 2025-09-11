using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarCircularRoundDemo : MonoBehaviour
{  
    //The ProgressBarCircularRound Component.      
    public ProgressBarCircularRound m_ProgressBar;
    void Start()
    {   
        //Set the current value.
        m_ProgressBar.CurrentValue = 50; 
        //Set the maximum value.
        m_ProgressBar.MaxValue = 100; 
        //Set whether to show the text.
        m_ProgressBar.HasText = true; 
        
        //If 'Clockwise' is true, then current progress will increase clockwise.
        //If 'Clockwise' is false, then current progress will increase anticlockwise.
        m_ProgressBar.Clockwise = true;  
        //Set the start position which include four positions such as Top, Bottom, Left, Right.
        m_ProgressBar.CurOrigin = ProgressBarCircularRound.Origin.Bottom;          
    }          
}
*/

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class ProgressBarCircularRound : MonoBehaviour
    {
        public enum Origin
        {
            Bottom,
            Right,
            Top,
            Left
        };

        [SerializeField]
        float currentValue = 0f;

        [SerializeField]
        float maxValue = 100.0f;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        public TextMeshProUGUI text;

        [SerializeField]
        Image foreground;

        [SerializeField]
        RectTransform roundArea;
        
        [SerializeField]
        Image roundImage;

        [SerializeField]
        bool clockwise = true;
    
        [SerializeField]
        Origin origin = Origin.Bottom;     

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

        public bool Clockwise
        {
            get => clockwise;
            set
            {
                if (clockwise == value)
                {
                    return;
                }
                clockwise = value;
                UpdateGUI();
            }
        }

        public Origin CurOrigin
        {
            get => origin;
            set
            {
                if (origin == value)
                {
                    return;
                }
                origin = value;
                UpdateGUI();
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

        void InitRoundImage()
        {
            if(currentValue <= 0f)
            {
                roundArea.gameObject.SetActive(false);
            }
            else
            {
                roundArea.gameObject.SetActive(true);
            }
        }

        void Start()
        {
            InitRoundImage();
            UpdateGUI();
        }

        void Update()
        {
            if(bDelayedUpdate)
            {
                bDelayedUpdate = false;
                OnValueChanged();
            }
        }
          
        public void UpdateGUI()
        {
            UpdateForeground();            
            UpdateRoundArea();                         
            UpdateText();
        }

        void UpdateForeground()
        {
            foreground.fillAmount = currentValue / maxValue;
            foreground.fillMethod = Image.FillMethod.Radial360;
            foreground.fillOrigin = (int)origin;
            foreground.fillClockwise = clockwise;
            if(clockwise)
            {
                roundImage.fillOrigin = (int)Origin.Right;
            }
            else
            {
                roundImage.fillOrigin = (int)Origin.Left;
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
                text.text = (int)((currentValue / maxValue) * 100) + "%";
            }
        }

        void UpdateRoundArea()
        {      
            if(currentValue <= 0f)
            {
                roundArea.gameObject.SetActive(false);
            }
            else
            {
                roundArea.gameObject.SetActive(true);
                Vector3 capRotaionValue = Vector3.zero;
                switch (origin)
                {
                    case Origin.Top:
                    {
                        if( clockwise)
                        {
                            capRotaionValue.z = 360 * (1 - foreground.fillAmount);
                        }
                        else
                        {
                            capRotaionValue.z = 360 * (foreground.fillAmount);    
                        }
                        break;
                    }
                    case Origin.Bottom:
                    {
                        if( clockwise)
                        {
                            capRotaionValue.z = 360 * (1 - foreground.fillAmount)+180;  
                        }
                        else
                        {
                            capRotaionValue.z = 360 * (foreground.fillAmount)-180;    
                        }
                        break;
                    }
                    case Origin.Right:
                    {
                        if( clockwise)
                        {
                            capRotaionValue.z = 360 * (1 - foreground.fillAmount)+270;
                            
                        }
                        else
                        {
                            capRotaionValue.z = 360 * (foreground.fillAmount)+270;    
                        }
                        break;
                    }
                    case Origin.Left:
                    {
                        if( clockwise)
                        {
                            capRotaionValue.z = 360 * (1 - foreground.fillAmount)+90;
                        }
                        else
                        {
                            capRotaionValue.z = 360 * (foreground.fillAmount)+90;    
                        }
                        break;
                    }
                }            
                capRotaionValue.z = capRotaionValue.z % 360;
                roundArea.localEulerAngles = capRotaionValue;
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