using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class NotificationWithButtonUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        NotificationWithButton notification;
        
        void Start()
        {
            notification.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);  
        }

        void OnButtonClick()
        {
            notification.OnFirst.RemoveAllListeners(); 
            notification.OnFirst.AddListener(NotificationFirst);
            notification.OnSecond.RemoveAllListeners(); 
            notification.OnSecond.AddListener(NotificationSecond);
            notification.OnThird.RemoveAllListeners(); 
            notification.OnThird.AddListener(NotificationThird);
            notification.OnCancel.RemoveAllListeners();   
            notification.OnCancel.AddListener(NotificationCancel);      
            notification.ShowNotification(); 
        }

        void NotificationFirst()
        {
            Debug.Log("First Button Clicked");
        }

        void NotificationSecond()
        {
            Debug.Log("Second Button Clicked");
        }

        void NotificationThird()
        {
            Debug.Log("Third Button Clicked");
        }

        void NotificationCancel()
        {
            Debug.Log("Cancel Button Clicked");
        }
    }
}