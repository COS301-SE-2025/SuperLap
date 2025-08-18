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
public class ModalWindowListDemo : MonoBehaviour
{  
    //The ModalWindowList Component.
    public ModalWindowList m_ModalWindow; 
    
    //Create a List of new options.
    List<string> mOptions = new List<string> {"Option 1", "Option 2", "Option 3", "Option 4"};
    
    void Start()
    {
        //Add OnConfirm event listener.
        m_ModalWindow.OnConfirm.AddListener(ModalWindowConfirm);  
        //Add OnCancel event listener.
        m_ModalWindow.OnCancel.AddListener(ModalWindowCancel);   

        //Clear the old options.
        m_ModalWindow.ClearOptions();        
        //Add the new options.
        m_ModalWindow.AddOptions(mOptions);  

        //Show Modal Window.
        m_ModalWindow.ShowModalWindow(); 
    }    

    void ModalWindowConfirm(int index)
    {
        Debug.Log("ModalWindowConfirm, index:"+index);
    }

    void ModalWindowCancel(int index)
    {
        Debug.Log("ModalWindowCancel");
    }
}
*/

namespace RainbowArt.CleanFlatUI
{
    public class ModalWindowList : MonoBehaviour
    {    
        protected internal class ContentItem : MonoBehaviour
        {
            public TextMeshProUGUI itemText;
            public Image itemImage;
            public Image itemSelect;
            public Button button;
        }   
        
        [SerializeField]
        Image iconTitle;

        [SerializeField]
        TextMeshProUGUI title;

        [SerializeField]
        Button buttonClose;

        [SerializeField]
        Button buttonConfirm; 
        
        [SerializeField]
        Button buttonCancel;     

        [SerializeField]
        Animator animator;   

        [SerializeField]
        RectTransform contentRect; 

        [SerializeField]
        GameObject itemTemplate;  

        [SerializeField]
        TextMeshProUGUI itemText;

        [SerializeField]
        Image itemImage;

        [SerializeField]
        Image itemSelect;

        [SerializeField] 
        RectOffset padding = new RectOffset();

        [SerializeField]
        float spacing = 2;

        [Serializable]
        public class OptionItem
        {
            public string text;
            public Sprite icon;

            public OptionItem()
            {
            }

            public OptionItem(string newText)
            {
                text = newText;
            }

            public OptionItem(Sprite newImage)
            {
                icon = newImage;
            }
            public OptionItem(string newText, Sprite newImage)
            {
                text = newText;
                icon = newImage;
            }
        }

        [SerializeField]
        List<OptionItem> options = new List<OptionItem>();

        List<ContentItem> contentItems = new List<ContentItem>();    
         
        [Serializable]
        public class ModalWindowEvent : UnityEvent<int>{ }

        [SerializeField]
        ModalWindowEvent onConfirm = new ModalWindowEvent();         

        [SerializeField]
        ModalWindowEvent onCancel = new ModalWindowEvent(); 

        IEnumerator diableCoroutine;
        float disableTime = 0.5f;   
        int itemSelectedIndex = -1;   

        public string TitleValue
        {
            get
            {
                if(title != null)
                {
                    return title.text;
                }
                return "";               
            }
            set
            {
                if(title != null)
                {
                    title.text = value;
                }  
            }
        } 

        public Sprite IconValue
        {
            get
            {
                if(iconTitle != null)
                {
                    return iconTitle.sprite;
                }
                return null;           
            }
            set
            {
                if(iconTitle != null)
                {
                    if(value != null)
                    {
                        iconTitle.gameObject.SetActive(true);
                        iconTitle.sprite = value;
                    }
                    else
                    {
                        iconTitle.gameObject.SetActive(false);
                        iconTitle.sprite = null;
                    }                    
                }                
            }
        }   

        public ModalWindowEvent OnConfirm
        {
            get => onConfirm;
            set
            {
                onConfirm = value;
            }
        }  

        public ModalWindowEvent OnCancel
        {
            get => onCancel;
            set
            {
                onCancel = value;
            }
        }          

        public void ShowModalWindow()
        {
            gameObject.SetActive(true);
            InitButtons();   
            InitAnimation();    
            DestroyAllItems();      
            SetupTemplate();
            CreateAllItems(options);  
            PlayAnimation(true); 
        }
        
        public void HideModalWindow()
        {
            PlayAnimation(false);    
            if(animator != null)
            {
                if(diableCoroutine != null)
                {
                    StopCoroutine(diableCoroutine);
                    diableCoroutine = null;
                }    
                diableCoroutine = DisableTransition();              
                StartCoroutine(diableCoroutine); 
            }  
            else
            {
                gameObject.SetActive(false);
            }          
        }

        IEnumerator DisableTransition()
        {
            yield return new WaitForSeconds(disableTime);
            gameObject.SetActive(false);         
        }  
      
        void InitButtons()
        {
            if(buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners(); 
                buttonClose.onClick.AddListener(OnCloseClick);  
            }
            if(buttonConfirm != null)
            {
                buttonConfirm.onClick.RemoveAllListeners();
                buttonConfirm.onClick.AddListener(OnConfirmClick);  
            }
            if(buttonCancel != null)
            {   
                buttonCancel.onClick.RemoveAllListeners();
                buttonCancel.onClick.AddListener(OnCancelClick);  
            }            
        }

        void OnCloseClick()
        {
            OnCancelClick();           
        }

        void OnCancelClick()
        {            
            HideModalWindow();      
            onCancel.Invoke(-1);      
        }

        void OnConfirmClick()
        {            
            HideModalWindow();       
            onConfirm.Invoke(itemSelectedIndex);
        }

        void InitAnimation()
        {
            if(animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localScale = Vector3.one;
                animator.gameObject.transform.localEulerAngles = Vector3.zero;
            } 
        }
        
        void PlayAnimation(bool bShow)
        {
            if(animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                if(bShow)
                {
                    animator.Play("In",0,0);  
                }
                else
                {
                    animator.Play("Out",0,0);  
                }
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

        void SetupTemplate()
        {
            ContentItem contentItem = itemTemplate.GetComponent<ContentItem>();
            if (contentItem == null)
            {
                contentItem = itemTemplate.AddComponent<ContentItem>();
                contentItem.itemText = itemText;
                contentItem.itemImage = itemImage;       
                contentItem.itemSelect = itemSelect;      
                contentItem.button = itemTemplate.GetComponent<Button>();
            }
            itemTemplate.SetActive(false);
        }

        void CreateAllItems(List<OptionItem> options)
        {
            float itemWidth = itemTemplate.GetComponent<RectTransform>().rect.width;
            RectTransform templateParentTransform = itemTemplate.transform.parent as RectTransform;
            int dataCount = options.Count;
            float curY = -padding.top;
            for (int i = 0; i < dataCount; ++i)
            {
                OptionItem itemData = options[i];
                int index = i;
                GameObject go = Instantiate(itemTemplate) as GameObject;
                go.transform.SetParent(itemTemplate.gameObject.transform.parent, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localEulerAngles = Vector3.zero;
                go.SetActive(true);
                go.name = "Item" + i;
                ContentItem curItem = go.GetComponent<ContentItem>();
                contentItems.Add(curItem);
                curItem.itemText.text = itemData.text;
                if(itemData.icon == null)
                {
                    curItem.itemImage.gameObject.SetActive(false);
                    curItem.itemImage.sprite = null;
                }
                else
                {
                    curItem.itemImage.gameObject.SetActive(true);
                    curItem.itemImage.sprite = itemData.icon;
                }    
                curItem.button.onClick.RemoveAllListeners();
                curItem.button.onClick.AddListener(delegate { OnItemClicked(index); });

                RectTransform curRectTransform = go.GetComponent<RectTransform>();
                curRectTransform.anchoredPosition3D = new Vector3(padding.left, curY, 0);
                float curItemHeight = curRectTransform.rect.height;
                curY = curY - curItemHeight;
                if (i < (dataCount - 1))
                {
                    curY = curY - spacing;
                }                
            }            
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(curY) + padding.bottom);
            contentRect.anchoredPosition3D = new Vector3(0, 0, 0);
        }  

        void DestroyAllItems()
        {
            var itemsCount = contentItems.Count;
            for (int i = 0; i < itemsCount; i++)
            {
                if (contentItems[i] != null)
                {
                    Destroy(contentItems[i].gameObject);
                }
            }
            contentItems.Clear();
        }   

        void OnItemClicked(int index)
        {
            itemSelectedIndex = index;
            for (int i = 0; i < contentItems.Count; i++)
            {
                ContentItem curItem = contentItems[i];
                if( i == itemSelectedIndex)
                {
                    curItem.itemSelect.gameObject.SetActive(true);
                }
                else
                {
                    curItem.itemSelect.gameObject.SetActive(false);
                }   
            }
        }     
    }
}