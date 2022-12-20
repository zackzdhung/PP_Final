using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class roles : MonoBehaviour
{
    public GameObject Role;
    public int rolecount = 10;
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < rolecount; i++)
        {
            Vector3 position = new Vector3(Random.Range(10, -10), (float)0.5, Random.Range(10, -10));
            Instantiate(Role, position, Quaternion.identity);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
