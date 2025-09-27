using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ToastContentFitterUI : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        ToastContentFitter toast;
        
        void Start()
        {
            toast.gameObject.SetActive(false);
            button.onClick.AddListener(OnButtonClick);  
        }

        public void OnButtonClick()
        {
            toast.ShowToast(); 
        }
    }
}