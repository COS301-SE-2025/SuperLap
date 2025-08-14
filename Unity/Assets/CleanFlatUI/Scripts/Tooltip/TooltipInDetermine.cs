using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class TooltipInDetermine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        Tooltip tooltip;     
           
        RectTransform cachedRect;
        Camera cachedEnterEventCamera;

        void Start ()
        {
            cachedRect = GetComponent<RectTransform>();            
            tooltip.gameObject.SetActive(false); 
            UpdatePosition();
        }
        
        void Update()
        {
            if(tooltip.gameObject.activeSelf == true && cachedEnterEventCamera != null)
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                Vector2 mousePosition = Input.mousePosition;
#endif
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(cachedRect, mousePosition, cachedEnterEventCamera, out localMousePos))
                {
                    UpdatePosition();
                }
            }
        }

        void UpdatePosition()
        {
            RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
            RectTransform tooltipParentRect = tooltipRect.parent as RectTransform;
            if(tooltipParentRect == null)
            {
                return;
            }
#if ENABLE_INPUT_SYSTEM
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 mousePosition = Input.mousePosition;
#endif
            Vector2 mousePos;
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParentRect, mousePosition, cachedEnterEventCamera, out mousePos);
            Vector3 position = new Vector3(mousePos.x,mousePos.y,0);   
            tooltip.SetTooltipPosition(position,0,0);            
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            cachedEnterEventCamera = eventData.enterEventCamera;    
            tooltip.ShowTooltip();                       
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            cachedEnterEventCamera = null;
            tooltip.HideTooltip();  
        }      
    }
}