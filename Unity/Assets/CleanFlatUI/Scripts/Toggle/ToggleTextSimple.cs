using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowArt.CleanFlatUI
{
    public class ToggleTextSimple : MonoBehaviour 
    {
        [SerializeField]
        Toggle toggle;

        [SerializeField]
        RectTransform on;

        [SerializeField]
        RectTransform off;

        CanvasGroup canvasGroupOn;
        CanvasGroup canvasGroupOff;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(ToggleValueChanged);
            canvasGroupOn = on.gameObject.GetComponent<CanvasGroup>();
            canvasGroupOff = off.gameObject.GetComponent<CanvasGroup>();
        }

        void Start() 
        {            
            UpdateGUI();
        }

        void UpdateGUI()
        {
            if(toggle.isOn)
            {
                SetCanvasGroupAlpha(canvasGroupOn,1);
                SetCanvasGroupAlpha(canvasGroupOff,0);
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupOn,0);
                SetCanvasGroupAlpha(canvasGroupOff,1);
            }
        }

        void ToggleValueChanged(bool value)
        {
            if(value)
            {
                SetCanvasGroupAlpha(canvasGroupOn,1);
                SetCanvasGroupAlpha(canvasGroupOff,0);                
            }
            else
            {
                SetCanvasGroupAlpha(canvasGroupOn,0);
                SetCanvasGroupAlpha(canvasGroupOff,1);                
            }        
        }

        void SetCanvasGroupAlpha(CanvasGroup obj,float alpha)
        {
            obj.alpha = alpha;
        }
    }
}