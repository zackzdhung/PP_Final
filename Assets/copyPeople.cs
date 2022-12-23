using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class copyPeople : MonoBehaviour
{
    public GameObject human;
    public GameObject copyHuman;
    public int numberOfPeople = 2;
    public Vector3 speed = new Vector3(0, 0, 0);
    // alert area
    public float radius = 2;
    // Start is called before the first frame update
    void Start()
    {
        for (var i = 0; i < numberOfPeople; i++)
        {
            Vector3 position = new Vector3(Random.Range(49, -49), 0, Random.Range(49, -49));
            copyHuman = Instantiate(human);
            copyHuman.transform.localPosition = position;

        }
    }
}
