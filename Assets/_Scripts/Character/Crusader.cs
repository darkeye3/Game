using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusader : Character
{
    public Crusader()
    {

    }
    

    protected override void Awake()
    {
        characterName = "Reynauld";
        InitCharacter(characterName);
        Debug.Log("Crusader Awake");
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        InitStat();
        Attack();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void InitCharacter(string name)
    {
        imagePath = "Common/Textures/Charactor/sprite/gerolt";
        characterSprites = Resources.LoadAll<Sprite>(imagePath);

        //characterImg = gameObject.GetComponent<SpriteRenderer>();
        //characterImg.sprite = characterSprites[0];
    }

    protected override void OnIconClick()
    {
        //ÆË¾÷¿¡ ½ºÅÝ ³Ö±â
    }
}
