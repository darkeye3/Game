using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonEnum;
using System;

public class Character_old : MonoBehaviour
{
    [SerializeField]
    protected Sprite icon;
    [SerializeField]
    protected Sprite explorationModeImg;
    [SerializeField]
    protected string characterName;
    [SerializeField]
    protected float hp;
    [SerializeField]
    protected float stress;
    [SerializeField]
    protected int minAtkDmg;
    [SerializeField]
    protected int maxAtkDmg;
    [SerializeField]
    protected int depence;
    [SerializeField]
    protected int speed;
    [SerializeField]
    protected int datge;
    [SerializeField]
    protected int accuracy;
    [SerializeField]
    protected int critical;

    //Resist
    [SerializeField]
    protected BleedingResist bleedingResist;
    [SerializeField]
    protected DeathAttackResist deathAttackResist;
    [SerializeField]
    protected StunResist stunResist;
    [SerializeField]
    protected DiseaseResist diseaseResist;
    [SerializeField]
    protected PoisonResist poisonResist;
    [SerializeField]
    protected RestraintResist restraintResist;
    [SerializeField]
    protected TrapResist trapResist;
    [SerializeField]
    protected WeakeningResist weakeningResist;

    [SerializeField]
    protected List<Skill> skillList = new List<Skill>(7);

    [SerializeField]
    protected Weapon weapon ;

    protected virtual void Awake()
    {
        
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }
}

public class Weapon
{
    [SerializeField]
    protected WeaponLevel weaponUpgrade;

    protected int minAtkDmg;
    protected int maxAtkDmg;
    protected int critical;
    protected int speed;

    Weapon()
    {
        weaponUpgrade = WeaponLevel.Level_1;

    }
}

public class Armor
{
    [SerializeField]
    protected ArmorLevel armorUpgrade;

    protected int hp;
    protected int depence;
}

public class Skill
{
    protected SkillLevel skillLevel;

    protected bool[] activePosition;
    protected bool[] targetRange;
    
    protected float correctionDMG;
    protected float critical;
    protected int accuracy;

    protected virtual void Action()
    {

    }
}
