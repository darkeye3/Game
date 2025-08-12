using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Highwayman : Character
{
    public Highwayman()
    {

    }
    public Highwayman(string name)
    {
        characterName = name; // 생성자를 통해 전달된 이름을 내부 변수에 저장
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        InitStat();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
