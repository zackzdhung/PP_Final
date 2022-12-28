using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int radius = 1;
    public Vector3 normalVec = Vector3.zero;
    public int length = 25;

    public Id obstacleType;
    public enum Id
    {
        Obstacle,
        Wall
    }
    
    // Start is called before the first frame update
    void Start()
    {
        if (obstacleType == Id.Wall)
        {
            var t = transform;
            var vec = -t.position;
            if (Math.Abs(t.rotation.eulerAngles.y - 90) < .1f)
            {
                vec.z = 0;
            }
            else
            {
                vec.x = 0;
            }
            normalVec = -vec.normalized;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
