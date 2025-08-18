using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace RainbowArt.CleanFlatUI
{
    [ExecuteAlways]
    public class PopupMenu : MonoBehaviour, IPointerDownHandler
    {
        protected internal class PopupMenuItem : MonoBehaviour, IPointerEnterHandler
        {
            public TextMeshProUGUI itemText;
            public Image itemImage;
            public Image itemLine;
            public Button button;
            public virtual void OnPointerEnter(PointerEventData eventData)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        [SerializeField]
        GameObject itemTemplate;

        [SerializeField]
        TextMeshProUGUI itemText;

        [SerializeField]
        Image itemImage;

        [SerializeField]
        Image itemLine;

        [SerializeField]
        Animator animator;

        [SerializeField] 
        RectOffset padding = new RectOffset();

        [SerializeField]
        float spacing = 2;

        public enum Origin
        {
            TopStart = 0,
            TopCenter,
            TopEnd,
            BottomStart,
            BottomCenter,
            BottomEnd, 
            LeftStart,
            LeftCenter,
            LeftEnd,
            RightStart,
            RightCenter,
            RightEnd
        }

        [SerializeField]
        Origin origin = Origin.BottomStart;

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

        [Serializable]
        public class PopupMenuEvent: UnityEvent<int> { }

        [SerializeField]
        PopupMenuEvent onValueChanged = new PopupMenuEvent();           
        
        List<PopupMenuItem> menuItems = new List<PopupMenuItem>();
        GameObject clickerBlocker;
        IEnumerator diableCoroutine; 
        float disableTime = 0.15f; 
        uint distance = 10;        

        public RectOffset Padding
        {
            get => padding;
            set
            {
                padding = value;
            }
        }

        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
            }
        }

        public PopupMenuEvent OnValueChanged
        {
            get => onValueChanged;
            set
            {
                onValueChanged = value;
            }
        } 

        public void ShowPopupMenu(Vector3 position, float width, float height)
        {
            if(options.Count > 0)
            {
                gameObject.SetActive(true);
                DestroyAllMenuItems();
                DestroyClickBlocker();
                SetupTemplate();
                CreateAllMenuItems(options);
                UpdatePosition(position,width,height);
                CreateClickBlocker();
                PlayAnimation(true);
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
            PopupMenuItem menuItem = itemTemplate.GetComponent<PopupMenuItem>();
            if (menuItem == null)
            {
                menuItem = itemTemplate.AddComponent<PopupMenuItem>();
                menuItem.itemText = itemText;
                menuItem.itemImage = itemImage;   
                menuItem.itemLine = itemLine;              
                menuItem.button = itemTemplate.GetComponent<Button>();
            }
            itemTemplate.SetActive(false);
        }

        void CreateAllMenuItems(List<OptionItem> options)
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
                go.name = "MenuItem" + i;
                PopupMenuItem curMenuItem = go.GetComponent<PopupMenuItem>();
                menuItems.Add(curMenuItem);
                curMenuItem.itemText.text = itemData.text;
                if(itemData.icon == null)
                {
                    curMenuItem.itemImage.gameObject.SetActive(false);
                    curMenuItem.itemImage.sprite = null;
                }
                else
                {
                    curMenuItem.itemImage.gameObject.SetActive(true);
                    curMenuItem.itemImage.sprite = itemData.icon;
                }   
                if(curMenuItem.itemLine != null)
                {
                    if(i == (dataCount-1))
                    {
                        curMenuItem.itemLine.gameObject.SetActive(false);
                    }
                    else
                    {
                        curMenuItem.itemLine.gameObject.SetActive(true);
                    }
                }
                curMenuItem.button.onClick.RemoveAllListeners();
                curMenuItem.button.onClick.AddListener(delegate { OnItemClicked(index); });
                
                RectTransform curRectTransform = go.GetComponent<RectTransform>();
                curRectTransform.anchoredPosition3D = new Vector3(padding.left, curY, 0);
                float curItemHeight = curRectTransform.rect.height;
                curY = curY - curItemHeight;
                if (i < (dataCount - 1))
                {
                    curY = curY - spacing;
                }                
            }
            
            gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(curY) + padding.bottom);
            gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth + padding.left+padding.right);
        }

        Canvas GetRootCanvas()
        {
            List<Canvas> list = new List<Canvas>();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count == 0)
            {
                return null;
            }
            var listCount = list.Count;
            Canvas rootCanvas = list[listCount - 1];
            for (int i = 0; i < listCount; i++)
            {
                if (list[i].isRootCanvas || list[i].overrideSorting)
                {
                    rootCanvas = list[i];
                    break;
                }
            }
            return rootCanvas;
        }

        RectTransform GetRootCanvasRectTrans()
        {
            Canvas rootCanvas = GetRootCanvas();
            if (rootCanvas == null)
            {
                return null;
            }
            return rootCanvas.GetComponent<RectTransform>();
        }

        void CreateClickBlocker()
        {
            Canvas rootCanvas = GetRootCanvas();
            if(rootCanvas == null)
            {
                return;
            }
            clickerBlocker = new GameObject("ClickBlocker");
            RectTransform blockerRect = clickerBlocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = new Vector2(0.5f, 0.5f);
            blockerRect.anchorMax = new Vector2(0.5f, 0.5f);
            blockerRect.pivot = new Vector2(0.5f, 0.5f);
            Image blockerImage = clickerBlocker.AddComponent<Image>();
            blockerImage.color = Color.clear;
            RectTransform rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
            float rootCanvasWidth = rootCanvasRect.rect.width;
            float rootCanvasHeight = rootCanvasRect.rect.height;
            blockerRect.SetParent(rootCanvas.transform, false);
            blockerRect.localPosition = Vector3.zero;
            blockerRect.SetParent(gameObject.transform, true);
            blockerRect.SetAsFirstSibling();
            blockerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rootCanvasHeight);
            blockerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rootCanvasWidth);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HidePopupMenu(true);
        }

        void OnItemClicked(int index)
        {
            onValueChanged.Invoke(index);
            HidePopupMenu(true);
        }

        void DestroyAllMenuItems()
        {
            var itemsCount = menuItems.Count;
            for (int i = 0; i < itemsCount; i++)
            {
                if (menuItems[i] != null)
                {
                    Destroy(menuItems[i].gameObject);
                }
            }
            menuItems.Clear();
        }

        void DestroyClickBlocker()
        {
            if(clickerBlocker != null)
            {
                Destroy(clickerBlocker);
                clickerBlocker = null;
            }
        }

        public void HidePopupMenu(bool playAnim)
        {
            if (diableCoroutine != null)
            {
                StopCoroutine(diableCoroutine);
                diableCoroutine = null;
            }
            if(playAnim)
            {
                PlayAnimation(false);
                diableCoroutine = DisableTransition();
                StartCoroutine(diableCoroutine);
            }
            else
            {
                if (animator != null)
                {
                    animator.enabled = false;
                    animator.gameObject.transform.localScale = Vector3.one;
                    animator.gameObject.transform.localEulerAngles = Vector3.zero;
                }
                gameObject.SetActive(false);
                DestroyAllMenuItems();
                DestroyClickBlocker();
            }                 
        }   

        IEnumerator DisableTransition()
        {
            yield return new WaitForSeconds(disableTime);
            gameObject.SetActive(false);
            DestroyAllMenuItems();       
        }    

        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        void UpdatePosition(Vector3 position, float uiWidth, float uiHeight)
        {
            RectTransform selfRect = GetComponent<RectTransform>();
            float width = selfRect.rect.width;
            float height = selfRect.rect.height;   
            float selfPosX = position.x;
            float selfPosY = position.y;
            switch (origin)
            {                
                case Origin.BottomStart:
                {
                    selfPosY = position.y - distance; 
                    break;
                }
                case Origin.BottomCenter:
                {
                    selfPosX = position.x + uiWidth/2 - width/2;
                    selfPosY = position.y - distance; 
                    break;
                }
                case Origin.BottomEnd:
                {
                    selfPosX = position.x + uiWidth - width;
                    selfPosY = position.y - distance; 
                    break;
                }
                case Origin.TopStart:
                {
                    selfPosY = position.y + uiHeight + height + distance;                 
                    break;
                }
                case Origin.TopCenter:
                {
                    selfPosX = position.x + uiWidth/2 - width/2;
                    selfPosY = position.y + uiHeight + height + distance;                 
                    break;
                }
                case Origin.TopEnd:
                {
                    selfPosX = position.x + uiWidth - width;
                    selfPosY = position.y + uiHeight + height + distance;  
                    break;
                }
                case Origin.LeftStart:
                {
                    selfPosX = position.x - width - distance; 
                    selfPosY = position.y + uiHeight;  
                    break;
                } 
                case Origin.LeftCenter:
                {
                    selfPosX = position.x - width - distance; 
                    selfPosY = position.y + uiHeight/2 + height/2;  
                    break;
                } 
                case Origin.LeftEnd:
                {
                    selfPosX = position.x - width - distance; 
                    selfPosY = position.y + height;  
                    break;
                } 
                case Origin.RightStart:
                {
                    selfPosX = position.x + uiWidth + distance; 
                    selfPosY = position.y + uiHeight;   
                    break;
                } 
                case Origin.RightCenter:
                {
                    selfPosX = position.x + uiWidth + distance; 
                    selfPosY = position.y + uiHeight/2 + height/2;   
                    break;
                } 
                case Origin.RightEnd:
                {
                    selfPosX = position.x + uiWidth + distance; 
                    selfPosY = position.y + height;  
                    break;
                } 
            }  
            Vector3 selfPos = new Vector3(selfPosX,selfPosY,0);            
            selfRect.anchoredPosition3D = selfPos;

        }
       
        void PlayAnimation(bool bShow)
        {
            if (animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localScale = Vector3.one;
                animator.gameObject.transform.localEulerAngles = Vector3.zero;
            }
            if (animator != null)
            {
                if(animator.enabled == false)
                {
                    animator.enabled = true;
                }
                string animationStr = null; 
                if(bShow)
                {      
                    animationStr =  "In Right Bottom";               
                    switch (origin)
                    {   
                        case Origin.BottomStart:  
                        case Origin.RightStart:  
                        {                            
                            break;
                        }
                        case Origin.TopStart:
                        case Origin.RightEnd:     
                        {
                            animationStr = "In Right Top"; 
                            break;
                        }
                        case Origin.TopEnd:                        
                        case Origin.LeftEnd:  
                        {
                            animationStr = "In Left Top"; 
                            break;
                        }               
                        case Origin.BottomEnd:     
                        case Origin.LeftStart:                   
                        {
                            animationStr = "In Left Bottom"; 
                            break;
                        } 
                        case Origin.TopCenter:     
                        {
                            animationStr = "In Top Middle"; 
                            break;
                        }
                        case Origin.BottomCenter:    
                        {
                            animationStr = "In Bottom Middle";
                            break;
                        }
                        case Origin.LeftCenter:
                        {
                            animationStr = "In Left Middle"; 
                            break;
                        } 
                        case Origin.RightCenter:                             
                        {     
                            animationStr = "In Right Middle";                       
                            break;
                        }
                                          
                    }                       
                }
                else
                {
                    animationStr = "Out Right Bottom"; 
                    switch (origin)
                    {
                        case Origin.BottomStart:                             
                        case Origin.RightStart:
                        {
                            break;
                        } 
                        case Origin.TopStart:                        
                        case Origin.RightEnd:       
                        {
                            animationStr = "Out Right Top"; 
                            break;
                        }
                        case Origin.TopEnd:                        
                        case Origin.LeftEnd:  
                        {
                            animationStr = "Out Left Top"; 
                            break;
                        }                                        
                        case Origin.BottomEnd:
                        case Origin.LeftStart:
                        {
                            animationStr = "Out Left Bottom"; 
                            break;
                        }                         
                        case Origin.TopCenter:     
                        {
                            animationStr = "Out Top Middle"; 
                            break;
                        }
                        case Origin.BottomCenter:    
                        {
                            animationStr = "Out Bottom Middle";
                            break;
                        }
                        case Origin.LeftCenter:
                        {
                            animationStr = "Out Left Middle"; 
                            break;
                        } 
                        case Origin.RightCenter:                             
                        {     
                            animationStr = "Out Right Middle";                       
                            break;
                        }
                    }                    
                }
                animator.Play(animationStr,0,0);
            }            
        }   
    }
}