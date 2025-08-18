using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarGridLinearAutoDemo : MonoBehaviour
{
    //The ProgressBarGridLinearAuto Component.  
    public ProgressBarGridLinearAuto m_ProgressBar;
    void Start()
    {
        //Set the minimum value.
        m_ProgressBar.MinValue = 0; 
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
    public class ProgressBarGridLinearAuto : MonoBehaviour
    {
        [SerializeField]
        int minValue = 0;

        [SerializeField]
        int maxValue = 10;

        int currentValue = 0;

        [SerializeField]
        float spacing = 10;

        [SerializeField]
        [Range(0,1)]
        float loadSpeed = 0.2f;

        [SerializeField]
        bool forward = true;

        [SerializeField]
        bool loop = true;

        [SerializeField]
        bool hasText = true;

        [SerializeField]
        TextMeshProUGUI text;  

        [SerializeField]
        RectTransform background;

        [SerializeField]
        RectTransform foreground;

        [SerializeField]
        RectTransform bgTemplate;

        [SerializeField]
        RectTransform fgTemplate;
        
        List<RectTransform> bgList = new List<RectTransform>(); 
        List<RectTransform> fgList = new List<RectTransform>();   
        float totalTime = 0f;

        public int MinValue
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
                UpdateGUI();
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
            if(currentValue < 0)
            {
                currentValue = 0;
            }
            if (maxValue < 0)
            {
                maxValue = 10;
            }
            if(currentValue > maxValue)
            {
                currentValue = maxValue;
            }            
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
            OnValueChanged();
            CreateList(bgList, background, bgTemplate);
            CreateList(fgList, foreground, fgTemplate);
            UpdateGUI();
        } 

        void Update()
        {
            if(forward)
            {          
                totalTime += loadSpeed * (Time.deltaTime * 10);
                if(totalTime >= 1)
                {
                    currentValue++;
                    totalTime = 0f;
                    if (currentValue >= maxValue)
                    {
                        currentValue = maxValue;
                    }
                    UpdateGUI();                    
                    if (loop)
                    {
                        if (currentValue >= maxValue)
                        {
                            currentValue = minValue - 1;
                        }
                    }
                }                     
            }
            else
            {
                totalTime += loadSpeed * (Time.deltaTime * 10);
                if(totalTime >= 1)
                {
                    currentValue--;
                    totalTime = 0f;
                    if (currentValue <= minValue)
                    {
                        currentValue = minValue;
                    }
                    UpdateGUI();
                    if (loop)
                    {
                        if (currentValue <= minValue)
                        {
                            currentValue = maxValue + 1;
                        }
                    }
                }
            }  
        }

        void UpdateGUI()
        {            
            UpdateForeground();  
            UpdateText();
        }

        void CreateList( List<RectTransform> list, RectTransform rectParent, RectTransform template)
        {
            template.gameObject.SetActive(false);
            float curX = 0;
            float itemWidth = template.rect.width;
            for (int i = 0; i < maxValue; i++)
            {
                RectTransform item = CreateItem(rectParent, template);
                list.Add(item);
                Vector3 pos = item.anchoredPosition3D;
                pos.x = curX;
                item.anchoredPosition3D = pos;
                curX += (itemWidth + spacing);
            }
        }

        RectTransform CreateItem(RectTransform rectParent, RectTransform template)
        {
            GameObject obj = GameObject.Instantiate(template.gameObject, rectParent);
            obj.gameObject.SetActive(true);
            RectTransform rectTrans = obj.GetComponent<RectTransform>();
            rectTrans.localScale = Vector3.one;
            rectTrans.localEulerAngles = Vector3.zero;
            rectTrans.anchoredPosition3D = Vector3.zero;
            return rectTrans;
        }

        void UpdateForeground()
        {
            for(int i = 0; i < fgList.Count; i++)
            {
                RectTransform rectTrans = fgList[i];
                if(i < currentValue)
                {                    
                    rectTrans.gameObject.SetActive(true);
                }  
                else
                {
                    rectTrans.gameObject.SetActive(false);
                }              
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
                float val = (float)currentValue / (float)maxValue;
                text.text = Mathf.FloorToInt(val * 100) + "%";
            }
        }     
    }
}