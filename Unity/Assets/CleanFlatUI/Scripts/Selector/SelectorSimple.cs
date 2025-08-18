using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
 
/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class SelectorSimpleDemo : MonoBehaviour
{
    //The SelectorSimple Component.
    public SelectorSimple m_Selector;

    //Create a List of new options.
    List<string> mOptions = new List<string> {"Option 1", "Option 2", "Option 3"};
    
    void Start()
    {
        //Add OnValueChanged event listener.
        m_Selector.OnValueChanged.AddListener(SelectorValueChange);

        //Set start index.
        m_Selector.StartIndex = 1; 
        //Clear the old options.
        m_Selector.ClearOptions();        
        //Add the new options.
        m_Selector.AddOptions(mOptions);
    }
    public void SelectorValueChange(int val)
    {
        Debug.Log("SelectorValueChange, index: " + val);
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class SelectorSimple : MonoBehaviour 
    {
        [SerializeField]
        public Button buttonPrevious;

        [SerializeField]
        Button buttonNext;

        [SerializeField]
        Image imageCurrent;

        [SerializeField]
        TextMeshProUGUI textCurrent;

        [SerializeField]
        bool loop = false;

        [SerializeField]
        bool hasIndicator = false;

        [SerializeField]
        TextMeshProUGUI indicator;

        [SerializeField]
        RectTransform indicatorRect;        

        [SerializeField]
        int startIndex = 0;
        
        [Serializable]
        public class OptionItem
        {
            public string optionText = "option"; 
            public Sprite optionImage;    

            public OptionItem()
            {
            }

            public OptionItem(string newText)
            {
                optionText = newText;
            }

            public OptionItem(Sprite newImage)
            {
                optionImage = newImage;
            }
            public OptionItem(string newText, Sprite newImage)
            {
                optionText = newText;
                optionImage = newImage;
            }                   
        }

        public List<OptionItem> options = new List<OptionItem>();

        [Serializable]
        public class SelectorSimpleEvent : UnityEvent<int>{ }

        [SerializeField]
        SelectorSimpleEvent onValueChanged = new SelectorSimpleEvent();

        bool changed = true;
        int newIndex = 0;
        int currentIndex = 0;          

        public int CurrentIndex
        {
            get => currentIndex;
            set
            {
                SetCurrentOptions(value);
                onValueChanged.Invoke(currentIndex);
            }
        }

        public int StartIndex
        {
            get => startIndex;
            set
            {
                startIndex = value;
                SetCurrentOptions(value);
            }
        }

        public bool HasIndicator
        {
            get => hasIndicator;
            set
            {
                hasIndicator = value;
                if (indicator != null && indicator.gameObject.activeSelf != hasIndicator)
                {
                    indicator.gameObject.SetActive(hasIndicator);
                }
            }
        }

        public SelectorSimpleEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        } 

        void Start()
        {
            if(buttonPrevious != null)
            {
                buttonPrevious.onClick.AddListener(OnButtonClickPrevious); 
            } 
            if(buttonNext != null)
            {
                buttonNext.onClick.AddListener(OnButtonClickNext); 
            }
            CurrentIndex = startIndex;
        }

        public void OnButtonClickPrevious()
        {
            UpdateOptions(false);            
            if(changed)
            {
                onValueChanged.Invoke(CurrentIndex);
            }                    
        }
        
        public void OnButtonClickNext()
        {
            UpdateOptions(true);                    
            if(changed)
            {
                onValueChanged.Invoke(CurrentIndex);
            }                 
        }

        public void AddOptions(List<OptionItem> optionList)
        {
            options.AddRange(optionList);
        }

        public void AddOptions(List<string> optionList)
        {
            for (int i = 0; i < optionList.Count; i++)
            {
                options.Add(new OptionItem(optionList[i]));
            }                
        }

        public void AddOptions(List<Sprite> optionList)
        {
            for (int i = 0; i < optionList.Count; i++)
            {
                options.Add(new OptionItem(optionList[i]));
            }                
        }

        public void ClearOptions()
        {
            options.Clear();
        }

        void SetCurrentOptions(int newCurrentIndex)
        {
            currentIndex = newCurrentIndex;            
            SetOptions();
            if (hasIndicator && (indicator != null))
            {
                indicator.text = (currentIndex + 1) + " / " + options.Count;
            }
        }      

        void SetOptions()
        {
            textCurrent.text = options[currentIndex].optionText;
            if(imageCurrent != null)
            {
                if(options[currentIndex].optionImage != null)
                {
                    imageCurrent.gameObject.SetActive(true);
                    imageCurrent.sprite = options[currentIndex].optionImage;
                }
                else
                {
                    imageCurrent.gameObject.SetActive(false);
                    imageCurrent.sprite = null;
                }                       
            }    
        }  

        void UpdateOptions(bool bNext)
        {
            changed = true;
            if( bNext )
            {
                if(currentIndex == options.Count - 1)
                {
                    if(loop)
                    {
                        newIndex = 0;
                    }
                    else
                    {
                        changed = false;
                    }                    
                }
                else
                {                  
                    newIndex = currentIndex + 1;                    
                }             
            }
            else
            {
                if(currentIndex == 0)
                {
                    if(loop)
                    {
                        newIndex = options.Count -1;
                    }
                    else
                    {
                        changed = false;
                    }                    
                }
                else
                {                 
                    newIndex = currentIndex - 1;                    
                }                 
            } 
            if(changed)
            {     
                currentIndex = newIndex;               
                SetOptions();           
                if(hasIndicator &&(indicator != null))
                {
                    indicator.text = (newIndex+1) +" / "+ options.Count;
                }                
            }          
        }
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            if(indicatorRect != null)
            {
                indicatorRect.gameObject.SetActive(hasIndicator);
            } 
        }
        #endif
    }
}