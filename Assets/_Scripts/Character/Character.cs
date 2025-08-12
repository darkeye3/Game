using CommonEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class Character : MonoBehaviour
{
    //Base
    [SerializeField]
    protected Button icon;
    [SerializeField]
    protected Sprite iconImg;
    [SerializeField]
    public string characterName;
    [SerializeField]
    protected Sprite[] characterSprites;
    [SerializeField]
    protected SpriteRenderer characterImg;
    [SerializeField]
    protected string imagePath;

    //Game Abilities
    [SerializeField]
    protected Dictionary<string, object> abilities;
    public Dictionary<string, object> _abilities { get { return abilities; } }
    [SerializeField]
    protected Dictionary<string, Resistances> resistances;
    public Dictionary<string, Resistances> _resistances { get { return resistances; } }
    [SerializeField]
    protected Dictionary<string, EquipmentLevels> equipments;
    public Dictionary<string, EquipmentLevels> _equipments { get { return equipments; } }
    [SerializeField]
    protected Dictionary<string, object> skills;
    public Dictionary<string, object> _skills { get { return skills; } }

    protected Character()
    {

    }

    public Character(string name)
    {
        characterName = name; // 생성자를 통해 전달된 이름을 내부 변수에 저장
        icon.image.sprite = iconImg;
        InitCharacter(characterName);

        Debug.Log("생성자 작동!");
    }

    protected virtual void Awake()
    {
        InitCharacter(characterName);
    }
    // Use this for initialization
    protected virtual void Start()
    {
        InitStat();
    }

    protected virtual void InitCharacter(string name)
    {
        characterImg = gameObject.AddComponent<SpriteRenderer>();
    }

    protected void InitStat()
    {
        abilities = new Dictionary<string, object>
        {
            {"Max_HP", 0},  // Maximum Health Points
            {"DODGE", 0},  // Evasion rate
            {"PROT", 0.1},  // Protection
            {"SPD", 0},  // Speed
            {"ACC_MOD", 0},  // Accuracy Modifier
            {"CRIT", 0.03},  // Critical Hit Chance
            {"DMG", new Tuple<int, int>(6, 12)},  // Damage range (min, max)
            {"Weapon", EquipmentLevels.NONE},  // Weapon level
            {"Armor", EquipmentLevels.NONE}  // Armor level
        };

        resistances = new Dictionary<string, Resistances>
        {
            {"Stun", Resistances.NONE},
            {"Blight", Resistances.NONE},
            {"Disease", Resistances.NONE},
            {"Death", Resistances.NONE},
            {"Move", Resistances.NONE},
            {"Bleed", Resistances.NONE},
            {"Debuff", Resistances.NONE},
            {"Trap", Resistances.NONE}
        };

        equipments = new Dictionary<string, EquipmentLevels>
        {
            {"Weapon", EquipmentLevels.BASIC},
            {"Armor", EquipmentLevels.BASIC},
        };

        skills = new Dictionary<string, object>
        {
            {"Smite", new Dictionary<string, object>
                {
                    {"DMG_MOD", 0.15},  // Damage Modifier
                    {"CRIT_MOD", 0.06},  // Critical Hit Chance Modifier
                    {"ACC_BASE", 85},  // Base Accuracy
                    {"SPD", 0}  // Speed
                }
            },
            {"Zealous_Accusation", new Dictionary<string, object>
                {
                    {"DMG_MOD", -0.4},  // Damage Modifier
                    {"CRIT_MOD", 0.02},  // Critical Hit Chance Modifier
                    {"ACC_BASE", 90},  // Base Accuracy
                    {"SPD", 0}  // Speed
                }
            },
            {"Stunning_Blow", new Dictionary<string, object>
                {
                    {"DMG_MOD", -0.5},  // Damage Modifier
                    {"CRIT_MOD", 0.05},  // Critical Hit Chance Modifier
                    {"ACC_BASE", 85},  // Base Accuracy
                    {"SPD", 0},  // Speed
                    {"STUN", 1}  // Stun
                }
            },
            {"Bulwark_of_Faith", new Dictionary<string, object>
                {
                    {"DMG_MOD", -1},  // Damage Modifier
                    {"CRIT_MOD", 0},  // Critical Hit Chance Modifier
                    {"ACC_BASE", 100},  // Base Accuracy
                    {"SPD", 0},  // Speed
                    {"PROT", 0.25},  // Protection
                    {"MARK", 1}  // Mark
                }
            }
        };

        DisplayAbilities();
        DisplaySkills();
    }

    // Method to display abilities
    public void DisplayAbilities()
    {
        Debug.Log("Abilities:");
        foreach (KeyValuePair<string, object> ability in abilities)
        {
            Debug.Log("  " + ability.Key + ": " + ability.Value.ToString());
        }

        foreach (KeyValuePair<string, Resistances> resistance in resistances)
        {
            Debug.Log("  " + resistance.Key + ": " + resistance.Value.ToString());
        }

        foreach (KeyValuePair<string, EquipmentLevels> equipment in equipments)
        {
            Debug.Log("  " + equipment.Key + ": " + equipment.Value.ToString());
        }
    }

    // Method to display skills
    public void DisplaySkills()
    {
        Debug.Log("Skills:");
        foreach (KeyValuePair<string, object> skill in skills)
        {
            Debug.Log("  " + skill.Key + ":");
            foreach (KeyValuePair<string, object> detail in (Dictionary<string, object>)skill.Value)
            {
                Debug.Log("    " + detail.Key + ": " + detail.Value.ToString());
            }
        }
    }

    public void Attack()
    {
        // DMG의 최소와 최대값에서 랜덤하게 수를 하나 골라서 공격
        var damageRange = (Tuple<int, int>)abilities["DMG"];
        int damage = UnityEngine.Random.Range(damageRange.Item1, damageRange.Item2 + 1);

        // CRIT의 값에 따라서 크리티컬이 발동
        float critChance = (float)(double)abilities["CRIT"];
        if (UnityEngine.Random.value < critChance)
        {
            damage *= 2;  // 크리티컬 히트는 보통 데미지를 2배로 합니다.
            Debug.Log("Critical hit!");
        }

        ChangeImage(characterSprites[(int)CharacterMotion.Skill_5], 0f);

        ChangeImage(characterSprites[(int)CharacterMotion.IDLE], 2f);
        // 대상 캐릭터의 HP 감소
        //target.DecreaseHP(damage);
    }

    public void DecreaseHP(int damage)
    {
        int currentHP = (int)abilities["Max_HP"];
        float prot = (float)abilities["PROT"];
        float totalDamage = damage * (1 - prot);
        currentHP -= (int)Math.Round(totalDamage);
        abilities["Max_HP"] = currentHP;

        Debug.Log(characterName + " took " + damage + " damage. Current HP: " + currentHP);
    }

    public virtual void OnDefense()
    {

    }

    public virtual void OnDodge()
    {

    }

    public virtual void OnDeath()
    {

    }

    IEnumerator ChangeImageAfterDelay(Sprite newSprite, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        //characterImg.sprite = newSprite;
    }

    public void ChangeImage(Sprite newSprite, float delaySeconds)
    {
        StartCoroutine(ChangeImageAfterDelay(newSprite, delaySeconds));
    }

    protected virtual void OnIconClick()
    {

    }
}







