using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class NomadWagonHandler : MonoBehaviour
{
    private List<string> heroList;
    private List<Character> waitingHeroes;
    private System.Random random;
    [SerializeField]
    private Button creatButton;
    private GameObject heroInventory;
    private string heroPrefabPath = "Common/Prefabs/Charactor/";


    private void Awake()
    {
        heroList = new List<string>() { "Crusader", "Highwayman", "Vestal", "PlagueDoctor", };
        waitingHeroes = new List<Character>(4);
        random = new System.Random(); // Initialize random here
        creatButton.onClick.AddListener(CreateHero);
        
    }
    // Start is called before the first frame update
    void Start()
    {
        //CreateHero();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateHero()
    {
        waitingHeroes = new List<Character>(4);
        for (int i = 0; i < 4; i++)
        {
            int randomIdx = random.Next(0, heroList.Count); // Use random to generate an index
            string heroType = heroList[randomIdx]; // Get hero name from list
            Character newHero = null; // Initialize newHero as null
            string heroPath = null;
            //GameObject newHeroObject = new GameObject(heroType); // Create a new GameObject with the heroType as the name

            switch (heroType)
            {
                case "Crusader":
                    //newHero = newHeroObject.AddComponent<Crusader>(); // Add Crusader component to the GameObject
                    heroPath = heroPrefabPath + heroType;
                    Crusader crusader = Resources.Load<Crusader>(heroPath);

                    newHero = Instantiate(crusader);
                    
                    break;
                case "Highwayman":
                    heroPath = heroPrefabPath + heroType;
                    Highwayman highwayman = Resources.Load<Highwayman>(heroPath);

                    newHero = Instantiate(highwayman);

                    break;
                case "Vestal":
                    heroPath = heroPrefabPath + heroType;
                    Vestal verstal = Resources.Load<Vestal>(heroPath);

                    newHero = Instantiate(verstal);
                    break;
                case "PlagueDoctor":
                    heroPath = heroPrefabPath + heroType;
                    PlagueDoctor doctor = Resources.Load<PlagueDoctor>(heroPath);

                    newHero = Instantiate(doctor);
                    break;
                default:
                    Debug.LogWarning("None Hero String");
                    break;
            }

 /*           if (newHero != null)
            {
                newHero.characterName = heroType; // Set the character name
                waitingHeroes.Add(newHero); // Add the new hero to the list
                Debug.Log("CreateHero() :" + newHero.characterName);
            }*/
        }
    }
}
