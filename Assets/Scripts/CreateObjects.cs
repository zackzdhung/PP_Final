using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateObjects : MonoBehaviour
{
    public GameObject mObject;
    // public GameObject copyHuman;
    public int num = 2;
    public ObjectId id;
    public enum ObjectId
    {
        Obstacle,
        People,
        Exit
    }
    // public Vector3 speed = new Vector3(0, 0, 0);
    // alert area
    // public float radius = 2;
    // Start is called before the first frame update
    void Awake()
    {
        float xRange, y, zRange;

        switch (id)
        {
            case ObjectId.Obstacle:
                xRange = 19;
                y = 0.5f;
                zRange = 19;
                break;
            case ObjectId.People:
                xRange = 29;
                y = 1;
                zRange = 29;
                break;
            case ObjectId.Exit:
                xRange = 39;
                y = 1;
                zRange = 39;
                break;
            default:
                xRange = 19;
                y = 1;
                zRange = 19;
                break;
        }
        
        for (var i = 0; i < num; i++)
        {
            Vector3 position = new Vector3(Random.Range(xRange, -xRange), y, Random.Range(zRange, -zRange));
            GameObject newObject = Instantiate(mObject);
            newObject.transform.localPosition = position;
        }
    }
}
