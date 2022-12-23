using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buildWall : MonoBehaviour
{
    public Wall wall;
    public Wall copyWall;
    private int wallNumber = 50;
    // Start is called before the first frame update
    void Start()
    {
        for (var i = 0; i < wallNumber; i++)
        {
            Vector3 position = new Vector3(-50+i, 0, 50);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            position = new Vector3(50 - i, 0, 50);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            position = new Vector3(50, 0, -50 + i);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            
            position = new Vector3(50, 0, 50 - i);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;


            position = new Vector3(-50 + i, 0, -50);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            position = new Vector3(50-i, 0, -50);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            position = new Vector3(-50, 0, -50 + i);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

            position = new Vector3(-50, 0, 50 - i);
            copyWall = Instantiate(wall);
            copyWall.transform.localPosition = position;

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
