using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserManager : SingletonMonoBehaviour<UserManager>
{
    public List<Character> heroInventory;

    // Define events for adding or removing characters
    public delegate void InventoryChanged();
    public event InventoryChanged OnInventoryAdded;
    public event InventoryChanged OnInventoryRemoved;

    // Start is called before the first frame update
    private void Awake()
    {
        heroInventory = new List<Character>();
    }

    private void Start()
    {
        //UserData 가져오기
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    public void AddCharacterToInventory(Character character)
    {
        heroInventory.Add(character);
        // Trigger the added event
        OnInventoryAdded?.Invoke();
    }

    // Method to remove a character from the inventory
    public void RemoveCharacterFromInventory(Character character)
    {
        if (heroInventory.Remove(character))
        {
            // Trigger the removed event only if the character was successfully removed
            OnInventoryRemoved?.Invoke();
        }
    }

    // Optional: Clearing all characters from the inventory
    public void ClearInventory()
    {
        heroInventory.Clear();
        OnInventoryRemoved?.Invoke(); // Assuming we want to update the UI on clear as well
    }
}
