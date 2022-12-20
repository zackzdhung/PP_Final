using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
    public GameObject myPrefab;
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < 2; i++)
        {
            Instantiate(myPrefab, new Vector3(i, 0, 0), transform.rotation);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
