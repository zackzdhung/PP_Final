using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollidAndDestroy : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "exit")
        {
            Debug.Log("arrive goal");
            Destroy(gameObject);
        }
        if (collision.gameObject.tag == "Player")
        {
            Debug.Log("not arrive goal");
        }

    }
}
