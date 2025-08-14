using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RainbowArt.CleanFlatUI
{
    public class ButtonMultiTransition : MonoBehaviour
    {
        Button button;

        [SerializeField]
        Animator[] animators;

        public void Start()
        {
            if(button == null)
            {
                button = gameObject.GetComponent<Button>();
            }
            button.onClick.AddListener(OnButtonClick);
        }
        
        public void OnButtonClick()
        {
            for(int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                animator.Play("Transition",0,0);               
            }
        }
    }
}