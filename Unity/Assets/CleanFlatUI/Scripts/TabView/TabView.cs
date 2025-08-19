using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class TabViewDemo : MonoBehaviour
{
    //The TabView Component.
    public TabView m_TabView;
    void Start()
    {
        //Add OnValueChanged event listener.
        m_TabView.OnValueChanged.AddListener(TabViewValueChange);
        //Set start index.
        m_TabView.StartIndex = 1;        
    }
    public void TabViewValueChange(int val)
    {
        Debug.Log("TabViewValueChange, value: " + val);
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class TabView : MonoBehaviour
    {
        [SerializeField]
        int startIndex = 0;        

        [SerializeField] 
        TabViewItem[] tabViews;

        [Serializable]
        public class TabViewItem
        {
            public GameObject tab; 
            public GameObject view;         
        }       

        [Serializable]
        public class TabViewEvent : UnityEvent<int>{ }

        [SerializeField]
        TabViewEvent onValueChanged = new TabViewEvent();  

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
                SetCurrentIndex(value);
                onValueChanged.Invoke(currentIndex);
            }
        }   

        public TabViewEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        }   

        void OnEnable()
        {
            InitAnimators();
            InitTabViews();                    
        }
        
        void OnDisable()
        {
            for (int i = 0; i < tabViews.Length; i++)
            {
                int index = i;
                TabViewItem item = tabViews[i];
                Toggle toggle = item.tab.GetComponent<Toggle>();
                toggle.onValueChanged.RemoveAllListeners();
            }
        }

        void InitAnimators()
        {
            for(int i = 0; i < tabViews.Length; i++)
            {
                Animator animator = tabViews[i].view.GetComponent<Animator>();
                ResetAnimation(animator);                             
            }
        }

        public void InitTabViews()
        {
            SetCurrentIndex(startIndex);             
            onValueChanged.Invoke(currentIndex);
            for (int i = 0; i < tabViews.Length; i++)
            {
                int index = i;
                TabViewItem item = tabViews[i];
                Toggle toggle = item.tab.GetComponent<Toggle>();
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((bool value) => TabValueChanged(index, value));
            }
        }

        void SetCurrentIndex(int newCurrentIndex)
        {
            for (int i = 0; i < tabViews.Length; i++)
            {
                int index = i;
                TabViewItem item = tabViews[i];
                Toggle toggle = item.tab.GetComponent<Toggle>();
                if(i == newCurrentIndex)
                {
                    toggle.SetIsOnWithoutNotify(true);
                    Animator animatorTab = item.tab.GetComponent<Animator>();
                    Animator animatorView = item.view.GetComponent<Animator>();
                    item.view.SetActive(true);
                    PlayAnimation(animatorTab, "On Init");
                    PlayAnimation(animatorView, "Init");
                }
                else
                {
                    toggle.SetIsOnWithoutNotify(false);
                    item.view.SetActive(false);
                    item.tab.GetComponent<Tab>().UpdateStatusContent();
                }
            }
            currentIndex = newCurrentIndex;           
        }

        public void TabValueChanged(int index, bool value)
        {
            TabViewItem item = tabViews[index];
            Toggle toggle = item.tab.GetComponent<Toggle>();     
            Tab tab = item.tab.GetComponent<Tab>();   
            Animator animatorTab = item.tab.GetComponent<Animator>(); 
            Animator animatorView = item.view.GetComponent<Animator>();                    
            if (toggle.isOn)
            {
                currentIndex = index;
                onValueChanged.Invoke(currentIndex);
                item.view.SetActive(true);    
                PlayAnimation(animatorTab, "On");
                PlayAnimation(animatorView, "On");
            }
            else
            {
                item.view.SetActive(false);
                PlayAnimation(animatorTab, "Off");
                ResetAnimation(animatorView);
            }
        }  

        void PlayAnimation(Animator animator,string animStr)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                animator.Play(animStr,0,0);  
            }
        }

        void ResetAnimation(Animator animator)
        {
            if(animator != null)
            {
                animator.enabled = false;
            }
        }

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }   
    }
}