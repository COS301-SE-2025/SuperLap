using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class PopupMenuLongPress : MonoBehaviour,IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField]
        PopupMenu popupMenu;    

        Camera cachedEnterEventCamera;       

        bool isPressed = false;
        float elapsedTime = 0f;
        float duration = 0.3f;

        void Start()
        {
            popupMenu.gameObject.SetActive(false);  
            popupMenu.OnValueChanged.AddListener(PopupMenuValueChanged);
        }   

        void Update()
        {
            if(isPressed)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= duration)
                {
                    showPopupMenu();   
                    isPressed = false;   
                    elapsedTime = 0f;                                                
                }
            }
        } 

        void showPopupMenu()
        {
            if(cachedEnterEventCamera != null)
            {
                RectTransform popupMenuRect = popupMenu.GetComponent<RectTransform>();
                RectTransform popupMenuParentRect = popupMenuRect.parent as RectTransform;
                if(popupMenuParentRect == null)
                {
                    return;
                }
                RectTransform uiRect = GetComponent<RectTransform>();
                float uiWidth = uiRect.rect.width;
                float uiHeight = uiRect.rect.height;       
                Vector3[] corners = new Vector3[4];
                uiRect.GetWorldCorners(corners);
                Vector3[] cornersInArea = new Vector3[4];
                for(int i = 0; i < 4; i++)
                {
                    cornersInArea[i] = popupMenuParentRect.InverseTransformPoint(corners[i]); 
                } 
                Vector3 position = cornersInArea[0];
                popupMenu.ShowPopupMenu(position,uiWidth,uiHeight);           
            }       
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

        void PopupMenuValueChanged(int index)
        {            
            Debug.Log("PopupMenu value changed, index:"+index);
        } 
    }
}