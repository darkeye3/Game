using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CloseButton : MonoBehaviour
{
    private Button closeButton;
    [SerializeField]
    private GameObject targetObject;
    // Start is called before the first frame update
    void Start()
    {
        closeButton = gameObject.GetComponent<Button>();
        closeButton.onClick.AddListener(CloseObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CloseObject()
    {
        targetObject.SetActive(false);
    }
}
