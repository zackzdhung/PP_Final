using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeWalls : MonoBehaviour
{
    public GameObject Wall;
    public GameObject Corner;
    // Start is called before the first frame update
    void Start()
    {
        // use 8 pieces
        // Instantiate(Wall, new Vector3((float)10.5, (float)0.5, (float)5.5), transform.rotation);
        Instantiate(Wall, new Vector3((float)10.5, (float)0.5, (float)-5.5), transform.rotation);
        Instantiate(Wall, new Vector3((float)-10.5, (float)0.5, (float)5.5), transform.rotation);
        Instantiate(Wall, new Vector3((float)-10.5, (float)0.5, (float)-5.5), transform.rotation);

        // Instantiate(Wall, new Vector3((float)5.5, (float)0.5, (float)10.5), Quaternion.Euler(0f, 90f, 0f));
        Instantiate(Wall, new Vector3((float)5.5, (float)0.5, (float)-10.5), Quaternion.Euler(0f, 90f, 0f));
        Instantiate(Wall, new Vector3((float)-5.5, (float)0.5, (float)10.5), Quaternion.Euler(0f, 90f, 0f));
        Instantiate(Wall, new Vector3((float)-5.5, (float)0.5, (float)-10.5), Quaternion.Euler(0f, 90f, 0f));

        // fill corners
        // Instantiate(Corner, new Vector3((float)10.5, (float)0.5, (float)10.5), transform.rotation);
        Instantiate(Corner, new Vector3((float)10.5, (float)0.5, (float)-10.5), transform.rotation);
        Instantiate(Corner, new Vector3((float)-10.5, (float)0.5, (float)10.5), transform.rotation);
        Instantiate(Corner, new Vector3((float)-10.5, (float)0.5, (float)-10.5), transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
