using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

/*
//Set properties in C# example codes.
using RainbowArt.CleanFlatUI;
public class DropdownMultiCheckTransitionDemo : MonoBehaviour
{  
    //The DropdownMultiCheckTransition Component.
    public DropdownMultiCheckTransition m_Dropdown;
    void Start()
    {
        //Add OnSelectValueChanged event listener.
        m_Dropdown.OnSelectValueChanged.AddListener(DropdownSelectValueChange);
    }

    //Set all of the selected option indexes.
    public void SetSelectedOptions()
    {
        m_Dropdown.SelectedOptions = new int[] {1, 3};
    } 

    //Set an option to be selected.
    public void SetOneSelectedOption()
    {
        m_Dropdown.SetOptionSelected(1, true);
    }   

    //Unselect all options.
    public void UnSelectAllOptions()
    {
        m_Dropdown.UnSelecteAll();
    } 

    //Check whether an option is selected.
    public bool IsOptionSelected(int index)
    {
        return m_Dropdown.IsOptionSelected(index);
    }

    //It will be triggered when select value changed.
    public void DropdownSelectValueChange()
    {
        int[] selectedIndexes = m_Dropdown.SelectedOptions;
        Debug.Log("DropdownSelectValueChange, index : ");
        foreach (int index in selectedIndexes)
        {
            Debug.Log(index +",");
        }        
    }     
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class DropdownMultiCheckTransition : TMP_Dropdown
    {     
        [SerializeField]
        List<int> selectedOptions = new List<int>();

        [Serializable]
        public class DropdownMultiCheckTransitionEvent : UnityEvent { }

        [SerializeField]
        DropdownMultiCheckTransitionEvent onSelectValueChanged = new DropdownMultiCheckTransitionEvent();
        
        Toggle[] toggleList;
        Animator animatorList;   
        HashSet<int> selectedOptionsHashSet = new HashSet<int>();

        public int[] SelectedOptions
        {
            get
            {
                int[] ret = new int[selectedOptionsHashSet.Count];
                selectedOptionsHashSet.CopyTo(ret);
                return ret;
            }
            set
            {
                selectedOptionsHashSet.Clear();
                if (value != null)
                {
                    foreach (int index in value)
                    {
                        selectedOptionsHashSet.Add(index);
                    }
                }
                onSelectValueChanged.Invoke();
            }
        }

        public DropdownMultiCheckTransitionEvent OnSelectValueChanged
        {
            get => onSelectValueChanged;
            set
            {
                onSelectValueChanged = value;
            }
        }          

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach(int index in selectedOptions)
            {
                selectedOptionsHashSet.Add(index);
            }
        }

        public bool IsOptionSelected(int index)
        {
            return selectedOptionsHashSet.Contains(index);
        }

        public void SetOptionSelected(int index,bool selected,bool sendEvent = true)
        {
            if (IsOptionSelected(index) == selected)
            {
                return;
            }
            if(selected)
            {
                selectedOptionsHashSet.Add(index);
            }
            else
            {
                selectedOptionsHashSet.Remove(index);
            }
            if(sendEvent)
            {
                onSelectValueChanged.Invoke();
            }
        }

        public void UnSelecteAll()
        {
            int count = selectedOptionsHashSet.Count;
            selectedOptionsHashSet.Clear();
            if(count > 0)
            {
                onSelectValueChanged.Invoke();
            }
        }

        public new void Show()
        {
            Transform dropdownList = transform.Find("Dropdown List");
            if (dropdownList != null)
            {
                return;
            }
            base.Show();
            Transform contentTransform = transform.Find("Dropdown List/Viewport/Content");
            toggleList = contentTransform.GetComponentsInChildren<Toggle>(false);

            for (int i = 0; i < toggleList.Length; i++)
            {
                int index = i;
                Toggle item = toggleList[i];
                item.onValueChanged.RemoveAllListeners();
                item.onValueChanged.AddListener(x => OnSelectItemCustom(index,x));
                item.SetIsOnWithoutNotify(IsOptionSelected(i));
            } 

            if(animatorList == null)
            {
                Transform listTransform = transform.Find("Dropdown List");
                animatorList = listTransform.gameObject.GetComponent<Animator>();                
            }       
            PlayAnimation(true);      
        }

        public new void Hide()
        {
            if(animatorList == null)
            {
                Transform listTransform = transform.Find("Dropdown List");
                animatorList = listTransform.gameObject.GetComponent<Animator>();                
            } 
            PlayAnimation(false);
            
            base.Hide();  
        }

        void PlayAnimation(bool bShow)
        {
            if(animatorList != null)
            {
                if(animatorList.enabled == false)
                {
                    animatorList.enabled = true;
                }
                if(bShow)
                {
                    animatorList.Play("In",0,0);  
                }
                else
                {
                    animatorList.Play("Out",0,0);  
                }
            }            
        }     

        public override void OnPointerClick(PointerEventData eventData)
        {
            Show();
        }

        void OnSelectItemCustom(int selectedIndex, bool isSelected)
        {
            SetOptionSelected(selectedIndex, isSelected);
        }
    }
}