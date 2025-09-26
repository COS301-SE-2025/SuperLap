using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ContextMenuLeftClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        ContextMenu contextMenu;   

        [SerializeField]
        RectTransform areaScope;
       
        void Start()
        {
            contextMenu.gameObject.SetActive(false);  
            contextMenu.OnValueChanged.AddListener(ContextMenuValueChanged);    
        }

        public void OnPointerClick(PointerEventData eventData)
        {            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                Vector2 mousePosition = Input.mousePosition;
#endif
                Vector2 mousePos = Vector2.zero;
                RectTransform contextMenuRect = contextMenu.gameObject.GetComponent<RectTransform>();
                RectTransform contextMenuParentRect = contextMenuRect.parent as RectTransform;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(contextMenuParentRect, mousePosition, eventData.enterEventCamera, out mousePos))
                {
                    contextMenu.Show(mousePos, areaScope); 
                }                                     
            }        
        }

        void ContextMenuValueChanged(int index)
        {            
            Debug.Log("ContextMenu value changed, index:"+index);
        } 
    }
}