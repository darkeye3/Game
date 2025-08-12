using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Crusader_old : Character_old
{
    protected override void Awake()
    {
        //icon;

        explorationModeImg = Resources.Load<Sprite>("Common/Textures/Charactor/sprite/Crusader");

        if(explorationModeImg != null)
        {
            Debug.Log("Sucess");
        }
        else
        {
            Debug.Log("fail");
        }
        gameObject.GetComponent<SpriteRenderer>().sprite = explorationModeImg;

        characterName = "Reynauld";

        hp = 33;
        stress = 0;
        minAtkDmg = 6;
        maxAtkDmg = 12;
        depence = 0;
        speed = 1;
        datge = 5;
        accuracy = 0;
        critical = 3;

        bleedingResist = CommonEnum.BleedingResist.Level_0;
        deathAttackResist = CommonEnum.DeathAttackResist.Level_0;
        stunResist = CommonEnum.StunResist.Level_0;
        diseaseResist = CommonEnum.DiseaseResist.Level_0;
        poisonResist = CommonEnum.PoisonResist.Level_0;
        restraintResist = CommonEnum.RestraintResist.Level_0;
        trapResist = CommonEnum.TrapResist.Level_0;
        weakeningResist = CommonEnum.WeakeningResist.Level_0;


    }


    // Start is called before the first frame update
    protected override void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        
    }
}
