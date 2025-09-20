using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class PopupMenuRightClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        PopupMenu popupMenu;   
       
        void Start()
        {
            popupMenu.gameObject.SetActive(false);     
            popupMenu.OnValueChanged.AddListener(PopupMenuValueChanged);
        }

        public void OnPointerClick(PointerEventData eventData)
        {            
            if (eventData.button == PointerEventData.InputButton.Right)
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

        void PopupMenuValueChanged(int index)
        {            
            Debug.Log("PopupMenu value changed, index:"+index);
        }
    }
}