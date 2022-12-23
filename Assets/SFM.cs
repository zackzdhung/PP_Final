using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Random = System.Random;

public class SFM : MonoBehaviour
{
    public People me;
    public Walls wall;
    // The unit of distance : (m)
    // The unit of time : (s)
    // Only one exit 
    public Exit exit;


    // The desired speed
    static Random random = new System.Random();
    private static double mean = 1.34;
    private static double stddev = 0.26;
    private static double desiredSpeed = SampleGaussian(random, mean, stddev);
    // maximun speed
    private double max_speed = 1.3*desiredSpeed;

    // compute frequency
    private float frequency = 0.01f;

    // default parameters in paper
    private float relaxationTime = 0.5f;


    // use for compute repulsion with other people
    private double V_0_ab = 2.1;
    private double sigma = 0.3;

    // use for compute repulsion with walls 
    private double U_0_ab = 10;
    private double R = 0.2;     // control the behavior while people near to the wall


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moving = moveDistance(me);
        //Debug.Log(moving);
        me.transform.position += moving;
        
    }


    Vector3 moveDistance(People me)
    {
        Vector3 Motivate = new Vector3(0, 0, 0);

        var exits = FindObjectsOfType<Exit>();
        // only consider the nearest exit
        Exit myGaol = exits[0];
        for(int i = 1; i < exits.Length; i++)
        {
            if(Vector3.Distance(me.transform.position, exits[i].transform.position) < Vector3.Distance(me.transform.position, myGaol.transform.position))
            {
                myGaol = exits[i];
            }
        }
        Vector3 wish = Wish_Force(me, myGaol);
        Motivate += wish;

        var otherPeople = FindObjectsOfType<People>();
        if (otherPeople.Length > 0)
        {
            //Debug.Log("otherPeople.Length");
            //Debug.Log(otherPeople.Length);
            for (int i = 0; i < otherPeople.Length; i++)
            {
                //Debug.Log(me.transform.position);
                //Debug.Log(otherPeople[i].transform.position);
                //Debug.Log(Vector3.Distance(me.transform.position, otherPeople[i].transform.position));
                //Debug.Log(me.radius);
                if(Vector3.Distance(me.transform.position, otherPeople[i].transform.position) <= me.radius)
                {
                    //Debug.Log("computing the force with others");
                    Vector3 peopleRepulsion = V_ab_gradient(me, otherPeople[i]);
                    Motivate += peopleRepulsion;
                }
            }
        }
            

        var Walls = FindObjectsOfType<Walls>();
        // only consider the nearest wall
        if (Walls.Length > 0)
        {
            //Debug.Log("walls Length > 0");
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
        }
        
        // summed all forces to get total motivate
        // consider the fluctuation term if need


        // compute the speed
        Vector3 speed = Motivate * frequency;

        // speed should bounded by maximun speed
        speed = speedBound(speed);

        //mpdate my speed
        me.speed = speed;

        return speed;
    }

    // The force computed by my wish
    // Consider the nearest exit only
    Vector3 Wish_Force(People me, Exit exit)
    {
        Vector3 locate = me.transform.position;
        Vector3 exitPosition = exit.transform.position;
        Vector3 goalDirect = Vector3.Normalize(exitPosition - locate);
        Vector3 speed = (float)desiredSpeed * goalDirect;

        Vector3 Force = (speed - me.speed) / relaxationTime;

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

        if (r_ab == Vector3.zero)
        {
            // reference myself
            return Vector3.zero;
        }

        double r_ab_norm = Vector3.Distance(r_ab, Vector3.zero);
        //Debug.Log(r_ab_norm);
        if (r_ab_norm == 0)
        {
            //Debug.Log("r_ab_norm = 0, error");
            r_ab_norm = 0.00001;
        }
        double r_ab_step_norm = Vector3.Distance(r_ab - nextPosition, Vector3.zero);
        //Debug.Log(r_ab_step_norm);
        if (r_ab_step_norm == 0)
        {
            //Debug.Log("r_ab_step_norm = 0, error");
            r_ab_step_norm = 0.00001;
        }
        // b : the semi-minor axis of the ellipse
        //Debug.Log(Math.Pow(r_ab_norm + r_ab_step_norm, 2));
        //Debug.Log(Math.Pow(step, 2));
        double b = Math.Sqrt(Math.Pow(r_ab_norm + r_ab_step_norm, 2) - Math.Pow(step, 2)) / 2;
        //Debug.Log(b);
        if (b == 0)
        {
            //Debug.Log("b = 0, error");
            b = 0.00001;
        }
        double scalar = V_0_ab * Mathf.Exp((float)(-b / sigma)) * (r_ab_norm + r_ab_step_norm) / (4 * sigma * b);

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
        //Debug.Log(r_ab_norm);
        if (r_ab_norm == 0)
        {
            //Debug.Log("r_ab_norm = 0, error");
        }
        double scalar = -U_0_ab * Mathf.Exp((float)(-r_ab_norm / R)) / (R * r_ab_norm);

        return new Vector3((float)(scalar * myPosition.x), (float)(scalar * myPosition.y), (float)(scalar * myPosition.z));
    }

    Vector3 speedBound(Vector3 speed)
    {
        if (Vector3.Distance(speed, Vector3.zero) == 0)
        {
            //Debug.Log("r_ab_norm = 0, error");
        }
        double mySpeed = Vector3.Distance(speed, Vector3.zero);
        if (max_speed < mySpeed)
        {
            double scalar = max_speed / mySpeed;
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
