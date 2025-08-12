using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonEvent : MonoBehaviour
{
    [SerializeField]
    protected Button button;
    [SerializeField]
    protected GameObject popup;
    [SerializeField]
    protected AudioSource clickAudioSource;

    protected virtual void Awake()
    {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClickBtn);
        button.onClick.AddListener(ClickSound);
    }

    protected virtual void OnClickBtn()
    {
        if(popup != null)
        {
            popup.SetActive(true);
        }
        else
        {
            Debug.Log("Please Add Popup");
        }
    }

    protected virtual void ClickSound()
    {
        if(clickAudioSource != null)
        { 
            clickAudioSource.Play();
        }
        else
        {
            Debug.Log("Please Add Sound");
        }
    }
}
