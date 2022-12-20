using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float speed = 2; //[1] ���鲾�ʳt��
    public Transform[] target;  // [2] �ؼ�
    public float delta = 0.2f; // �~�t��
    private static int i = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        moveTo();

    }

    void moveTo()
    {
        // [3] ���s��l�ƥؼ��I
        target[i].position = new Vector3(target[i].position.x, transform.position.y, target[i].position.z);

        // [4] ������¦V�ؼ��I 
        transform.LookAt(target[i]);

        // [5] ����V�e����
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        // [6] �P�_����O�_��F�ؼ��I
        if (transform.position.x > target[i].position.x - delta
            && transform.position.x < target[i].position.x + delta
            && transform.position.z > target[i].position.z - delta
            && transform.position.z < target[i].position.z + delta)
            i = (i + 1) % target.Length;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            Debug.Log("����F!");
        }
        if (collision.gameObject.tag == "Corner")
        {
            Debug.Log("����F!");
        }
    }
}
