using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ContextMenuLongPress : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField]
        ContextMenu contextMenu;    

        [SerializeField]
        RectTransform areaScope;

        Camera cachedEnterEventCamera;   
        bool isPressed = false;
        float elapsedTime = 0f;
        float duration = 0.3f;

        void Start()
        {
            contextMenu.gameObject.SetActive(false); 
            contextMenu.OnValueChanged.AddListener(ContextMenuValueChanged); 
        }   

        void Update()
        {
            if(isPressed)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= duration)
                {
                    showContextMenu();   
                    isPressed = false;   
                    elapsedTime = 0f;                                                
                }
            }
        } 

        void showContextMenu()
        {
            if(cachedEnterEventCamera != null)
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                Vector2 mousePosition = Input.mousePosition;
#endif
                Vector2 localMousePos = Vector2.zero;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(areaScope, mousePosition, cachedEnterEventCamera, out localMousePos))
                {
                    UpdatePosition();                        
                }
            }       
        }

        void UpdatePosition()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 mousePosition = Input.mousePosition;
#endif 
            Vector2 mousePos = Vector2.zero;
            RectTransform contextMenuRect = contextMenu.gameObject.GetComponent<RectTransform>();
            RectTransform contextMenuParentRect = contextMenuRect.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(contextMenuParentRect, mousePosition, cachedEnterEventCamera, out mousePos);
            contextMenu.Show(mousePos, areaScope);                             
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            cachedEnterEventCamera = eventData.enterEventCamera;   
            isPressed = true;                
            elapsedTime = 0;
        } 

        public void OnPointerUp(PointerEventData eventData)
        {             
            cachedEnterEventCamera = eventData.enterEventCamera;
            isPressed = false;  
            elapsedTime = 0;                     
        }  

        public void OnPointerClick(PointerEventData eventData)
        {            
            if (eventData.button == PointerEventData.InputButton.Right)
            {                    
                cachedEnterEventCamera = null;             
            }
        }    

        void ContextMenuValueChanged(int index)
        {            
            Debug.Log("ContextMenu value changed, index:"+index);
        } 
    }
}