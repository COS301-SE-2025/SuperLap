using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowArt.CleanFlatUI
{
    public class TabViewGroup : MonoBehaviour
    {  
        [SerializeField]        
        Button buttonPrevious;

        [SerializeField]
        Button buttonNext;

        [SerializeField]
        int startIndex = 0;        
        
        public TabViewGroupItem[] tabViewGroups;

        [Serializable]
        public class TabViewGroupItem
        {
            public GameObject tabGroup; 
            public GameObject viewGroup; 
        }      

        int currentIndex = 0;    

        public int StartIndex
        {
            get => startIndex;
            set
            {
                startIndex = value;
            }
        }   

        public int CurrentIndex
        {
            get => currentIndex;
            set
            {
                if(currentIndex == value)
                {
                    return;
                }
                currentIndex = value;
            }
        }   
        
        void Start()
        {
            Initviews();              
            InitButtons();  
            UpdateButtons();                        
        }

        void Initviews()
        {
            currentIndex = startIndex; 
            UpdateViews();
        }       

        void InitButtons()
        {
            if(buttonPrevious != null)
            {
                buttonPrevious.onClick.RemoveAllListeners();
                buttonPrevious.onClick.AddListener(OnButtonClickPrevious); 
            } 
            if(buttonNext != null)
            {
                buttonNext.onClick.RemoveAllListeners();
                buttonNext.onClick.AddListener(OnButtonClickNext); 
            } 
        }

        void UpdateButtons()
        {
            if( currentIndex == (tabViewGroups.Length-1))
            {
                buttonPrevious.gameObject.SetActive(true);
                buttonNext.gameObject.SetActive(false);
            }
            else if(currentIndex == 0)
            {
                buttonPrevious.gameObject.SetActive(false);
                buttonNext.gameObject.SetActive(true);
            }
            else
            {
                buttonPrevious.gameObject.SetActive(true);
                buttonNext.gameObject.SetActive(true);
            }
        }

        public void OnButtonClickPrevious()
        {
            SetViews(false);                            
        }
        
        public void OnButtonClickNext()
        {
            SetViews(true);                           
        }  

        void SetViews(bool bNext)
        {
            if(bNext)      
            {
                currentIndex++;     
            }
            else
            {
                currentIndex--;  
            }
            UpdateViews();
            UpdateButtons();
        }
        
        void UpdateViews()
        {           
            for(int i = 0; i < tabViewGroups.Length; i++)
            {
                if(i == currentIndex)
                {
                    tabViewGroups[i].tabGroup.SetActive(true);
                    tabViewGroups[i].viewGroup.SetActive(true);
                    tabViewGroups[i].tabGroup.GetComponent<TabView>().InitTabViews();
                }
                else
                {
                    tabViewGroups[i].tabGroup.SetActive(false);   
                    tabViewGroups[i].viewGroup.SetActive(false);                           
                }
            }              
        }
    }
}