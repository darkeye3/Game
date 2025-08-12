using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_HeroInventory : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        // Subscribe to UserManager events
        //UserManager.Instance.OnInventoryAdded += UpdateUIOnAdd;
        //UserManager.Instance.OnInventoryRemoved += UpdateUIOnRemove;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        //UserManager.Instance.OnInventoryAdded -= UpdateUIOnAdd;
        //UserManager.Instance.OnInventoryRemoved -= UpdateUIOnRemove;
    }

    void UpdateUIOnAdd()
    {
        // Update your UI here, e.g., adding new character icons
        Debug.Log("Character added to inventory, updating UI.");
    }

    void UpdateUIOnRemove()
    {
        // Update your UI here, e.g., removing character icons
        Debug.Log("Character removed from inventory, updating UI.");
    }
}
