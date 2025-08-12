using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHero : ButtonEvent
{
    Character character;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void OnClickBtn()
    {
        //UserManager.Instance.AddCharacterToInventory(character);
    }

    protected override void ClickSound()
    {
        base.ClickSound();
    }
}
