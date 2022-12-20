using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class SFM : MonoBehaviour
{
    public People people;
    public Walls wall;
    // The unit of distance : (m)
    // The unit of time : (s)
    // Only one exit 
    public Vector3 Exit;



    // The desired speed
    static Random random = new Random();
    private static double mean = 1.34;
    private static double stddev = 0.26;
    private static double desiredSpeed = SampleGaussian(random, mean, stddev);
    // maximun speed
    private double max_speed = 1.3*desiredSpeed;
    // initial speed is (0, 0, 0)
    private Vector3 currentSpeed = new Vector3(0, 0, 0);

    // compute frequency
    public float frequency = 2;

    // default parameters in paper
    private float relaxationTime = 0.5f;


    // use for compute repulsion with other people
    private double V_0_ab = 2.1;
    private double sigma = 0.3;

    // use for compute repulsion with walls 
    private double U_0_ab = 10;
    private double R = 0.2;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void speedCompute(People me)
    {
        Vector3 Motivate = new Vector3(0, 0, 0);

        Vector3 wish = Wish_Force(me);
        Motivate += wish;

        var otherPeople = FindObjectsOfType<People>();
        for (int i = 0; i < otherPeople.Length; i++)
        {
            if(Vector3.Distance(me.transform.position, otherPeople[i].transform.position) <= me.radius)
            {
                Vector3 peopleRepulsion = V_ab_gradient(me, otherPeople[i]);
                Motivate += peopleRepulsion;
            }
        }

        var Walls = FindObjectsOfType<Walls>();
        // only consider the nearest wall
        Walls nearest_wall = Walls[0];
        for (int i = 1; i < Walls.Length; i++)
        {
            if (Vector3.Distance(me.transform.position, Walls[i].transform.position) < Vector3.Distance(me.transform.position, nearest_wall.transform.position))
            {
                nearest_wall = Walls[i];
            }
        }

        Vector3 WallRepulsion = U_ab_gradient(me, nearest_wall);
        Motivate += WallRepulsion;
        // summed all forces to get total motivate
        // consider the fluctuation term if need


        // compute the speed
        Vector3 speed = Motivate * frequency;

        // speed should bounded by maximun speed
        speed = speedBound(speed);

        me.speed = speed;


    }

    // The force computed by my wish
    // Consider the nearest exit only
    Vector3 Wish_Force(People me)
    {
        Vector3 locate = me.transform.position;
        Vector3 goalDirect = Vector3.Normalize(Exit - locate);
        Vector3 speed = (float)desiredSpeed * goalDirect;

        Vector3 Force = (speed - currentSpeed) / relaxationTime;

        return Force;
    }

    // The Repulsion between people and people
    Vector3 V_ab_gradient(People me, People other)
    {
        float v_beta = Vector3.Distance(other.speed, Vector3.zero);
        Vector3 myPosition = me.transform.position;
        Vector3 othersPosition = other.transform.position;
        Vector3 r_ab = myPosition - othersPosition;
        double step = v_beta * frequency;
        Vector3 e_beta = othersPosition;
        Vector3 nextPosition = v_beta * frequency * e_beta;

        double r_ab_norm = Vector3.Distance(r_ab, Vector3.zero);
        double r_ab_step_norm = Vector3.Distance(r_ab - v_beta * frequency * e_beta, Vector3.zero);

        double b = Math.Sqrt(Math.Pow(r_ab_norm + r_ab_step_norm, 2) - Math.Pow(step, 2)) / 2;
        double scalar = -V_0_ab * Mathf.Exp((float)(-b / sigma)) * (r_ab_norm + r_ab_step_norm) / (4 * sigma * b);

        double x_term = r_ab.x / r_ab_norm + (r_ab.x - nextPosition.x) / r_ab_step_norm;
        double y_term = r_ab.y / r_ab_norm + (r_ab.y - nextPosition.y) / r_ab_step_norm;
        double z_term = r_ab.z / r_ab_norm + (r_ab.z - nextPosition.z) / r_ab_step_norm;

        return new Vector3((float)(scalar * x_term), (float)(scalar * y_term), (float)(scalar * z_term));
    }


    // The Repulsion between people and Wall
    // Note that only consider the nearest wall
    Vector3 U_ab_gradient(People me, Walls wall)
    {
        Vector3 myPosition = me.transform.position;
        Vector3 WallPosition = wall.transform.position;
        Vector3 r_ab = myPosition - WallPosition;
        double r_ab_norm = Vector3.Distance(r_ab, Vector3.zero);
        double scalar = -U_0_ab * Mathf.Exp((float)(-r_ab_norm / R)) / (R * r_ab_norm);

        return new Vector3((float)(scalar * myPosition.x), (float)(scalar * myPosition.y), (float)(scalar * myPosition.z));
    }

    Vector3 speedBound(Vector3 speed)
    {
        if (max_speed < Vector3.Distance(speed, Vector3.zero))
        {
            double scalar = max_speed / Vector3.Distance(speed, Vector3.zero);
            speed = (float)scalar * speed;
        }
        return speed;
    }

    public static double SampleGaussian(Random random, double mean, double stddev)
    {
        // The method requires sampling from a uniform random of (0,1]
        // but Random.NextDouble() returns a sample of [0,1).
        double x1 = 1 - random.NextDouble();
        double x2 = 1 - random.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
        return y1 * stddev + mean;
    }
    // There are no attract designed by paper
    //Vector3 W_ai_gradient(Vector3 myPosition, Vector3 attractPosition)
    //{
    //
    //}
}
