using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        Notification notification;
        
        void Start()
        {
            notification.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);  
        }

        public void OnButtonClick()
        {
            notification.OnCancel.RemoveAllListeners();   
            notification.OnCancel.AddListener(NotificationCancel); 
            notification.ShowNotification(); 
        }

        void NotificationCancel()
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}