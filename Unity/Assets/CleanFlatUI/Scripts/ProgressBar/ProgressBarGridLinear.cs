using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class ProgressBarGridLinearDemo : MonoBehaviour
{  
    //The ProgressBarGridLinear Component. 
    public ProgressBarGridLinear m_ProgressBar;
    void Start()
    {   
        //Set the current value.
        m_ProgressBar.CurrentValue = 5; 
        //Set whether to show the text.
        m_ProgressBar.HasText = true; 
    }   
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class ProgressBarGridLinear : MonoBehaviour
    {
        [SerializeField]
        int currentValue = 0;

        [SerializeField]
        int maxValue = 10;

        [SerializeField]
        float spacing = 10;

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

        public int CurrentValue
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
      
        void Start()
        {  
            OnValueChanged();
            CreateList(bgList, background, bgTemplate);
            CreateList(fgList, foreground, fgTemplate);            
            UpdateGUI();
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
                RectTransform item = CreateItem(rectParent, template,i);
                list.Add(item);
                Vector3 pos = item.anchoredPosition3D;
                pos.x = curX;
                item.anchoredPosition3D = pos;
                curX += (itemWidth + spacing);
            }
           
        }

        RectTransform CreateItem(RectTransform rectParent, RectTransform template,int index)
        {
            GameObject obj = GameObject.Instantiate(template.gameObject, rectParent);
            obj.gameObject.SetActive(true);
            obj.gameObject.name = "item" + (index + 1);
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