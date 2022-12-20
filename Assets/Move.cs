using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float speed = 2; //[1] 物體移動速度
    public Transform[] target;  // [2] 目標
    public float delta = 0.2f; // 誤差值
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
        // [3] 重新初始化目標點
        target[i].position = new Vector3(target[i].position.x, transform.position.y, target[i].position.z);

        // [4] 讓物體朝向目標點 
        transform.LookAt(target[i]);

        // [5] 物體向前移動
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        // [6] 判斷物體是否到達目標點
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
            Debug.Log("撞牆了!");
        }
        if (collision.gameObject.tag == "Corner")
        {
            Debug.Log("撞牆了!");
        }
    }
}
