using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class SFM : MonoBehaviour
{
    // public People me;
    // public Walls wall;
    // The unit of distance : (m)
    // The unit of time : (s)
    // Only one exit 
    // public Vector3 exit = new Vector3(10, 0, 10);


    // The desired speed
    private static readonly Random MRandom = new();
    private const double Mean = 1.34;
    private const double Stddev = 0.26;

    private static readonly double DesiredSpeed = SampleGaussian();
    // maximum speed
    private readonly double _maxSpeed = 1.3 * DesiredSpeed;

    // compute frequency
    private const float Frequency = 0.1f;

    // default parameters in paper
    private const float RelaxationTime = 0.5f;


    // use for compute repulsion with other people
    private double V_0_ab = 2.1;
    private double sigma = 0.3;

    // use for compute repulsion with walls 
    private double U_0_ab = 10;
    private double R = 0.2;

    private Obstacle[] _walls;
    private People[] _peoples;
    private Vector3[] _moveDistances;
    private Exit[] _exits;
    private Exit[] _nearestExits;
    
    // Start is called before the first frame update
    void Start()
    {
        // _init = false;
        _walls = FindObjectsOfType<Obstacle>();
        _peoples = FindObjectsOfType<People>();
        _exits = FindObjectsOfType<Exit>();
        _nearestExits = new Exit[_peoples.Length];
        for (int i = 0; i < _nearestExits.Length; i++)
        {
            _nearestExits[i] = _exits[0];
        }
        
        _moveDistances = new Vector3[_peoples.Length];
        CheckOutsideTheWall();
    }

    private void CheckOutsideTheWall()
    {
        foreach (var p in _peoples)
        {
            foreach (var w in _walls)
            {
                var vec = p.transform.position - w.transform.position;
                var dist = vec.magnitude;
                var r = p.radius + w.radius;
                if (dist > r) continue;
                p.transform.position += vec / dist * r;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNearestExits();
        UpdateMoveDistance();
        PeopleMove();
    }

    private void PeopleMove()
    {
        for (int i = 0; i < _peoples.Length; i++)
        {
            _peoples[i].transform.position += _moveDistances[i];
        }
    }

    private void UpdateMoveDistance()
    {
        for (int i = 0; i < _peoples.Length; i++)
        {
            _moveDistances[i] = MoveDistance(_peoples[i], _nearestExits[i]);
        }
    }

    private void UpdateNearestExits()
    {
        // Maintain _nearestExits
        for (int i = 0; i < _peoples.Length; i++)
        {
            var mExit = _nearestExits[i];
            var mDist = (mExit.transform.position - _peoples[i].transform.position).magnitude;
            foreach (var e in _exits)
            {
                var dist = (e.transform.position - _peoples[i].transform.position).magnitude;
                if (dist > mDist) continue;
                mExit = e;
                mDist = dist;
            }
            _nearestExits[i] = mExit;
        }
    }


    Vector3 MoveDistance(People myself, Exit nearestExit)
    {
        Vector3 motivate = new Vector3(0, 0, 0);
        Vector3 wish = Wish_Force(myself, nearestExit);
        motivate += wish;

        if (_peoples.Length > 0)
        {
            //Debug.Log("otherPeople.Length");
            //Debug.Log(otherPeople.Length);
            foreach (var p in _peoples)
            {
                //Debug.Log(me.transform.position);
                //Debug.Log(otherPeople[i].transform.position);
                //Debug.Log(Vector3.Distance(me.transform.position, otherPeople[i].transform.position));
                //Debug.Log(me.radius);
                if(Vector3.Distance(myself.transform.position, p.transform.position) <= myself.radius)
                {
                    //Debug.Log("computing the force with others");
                    Vector3 peopleRepulsion = V_ab_gradient(myself, p);
                    motivate += peopleRepulsion;
                }
            }
        }
            

        // only consider the nearest wall
        if (_walls.Length > 0)
        {
            //Debug.Log("walls Length > 0");
            Obstacle nearestWall = _walls[0];
            for (int i = 1; i < _walls.Length; i++)
            {
                if (Vector3.Distance(myself.transform.position, _walls[i].transform.position) < Vector3.Distance(myself.transform.position, nearestWall.transform.position))
                {
                    nearestWall = _walls[i];
                }
            }

            Vector3 wallRepulsion = U_ab_gradient(myself, nearestWall);
            motivate += wallRepulsion;
        }
        
        // summed all forces to get total motivate
        // consider the fluctuation term if need


        // compute the speed
        Vector3 speed = motivate * Frequency;

        // speed should bounded by maximum speed
        speed = speedBound(speed);

        //update my speed
        myself.speed = speed;

        return speed / 50;
    }

    // The force computed by my wish
    // Consider the nearest exit only
    Vector3 Wish_Force(People myself, Exit nearestExit)
    {
        Vector3 locate = myself.transform.position;
        Vector3 goalDirect = Vector3.Normalize(nearestExit.transform.position - locate);
        Vector3 speed = (float)DesiredSpeed * goalDirect;

        Vector3 force = (speed - myself.speed) / RelaxationTime;

        return force;
    }

    // The Repulsion between people and people
    Vector3 V_ab_gradient(People myself, People other)
    {
        float v_beta = Vector3.Distance(other.speed, Vector3.zero);
        Vector3 myPosition = myself.transform.position;
        Vector3 othersPosition = other.transform.position;
        Vector3 r_ab = myPosition - othersPosition;
        double step = v_beta * Frequency;
        Vector3 e_beta = othersPosition;
        Vector3 nextPosition = v_beta * Frequency * e_beta;

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
    Vector3 U_ab_gradient(People me, Obstacle wall)
    {
        Vector3 myPosition = me.transform.position;
        Vector3 wallPosition = wall.transform.position;
        Vector3 r_ab = myPosition - wallPosition;
        double r_ab_norm = Vector3.Distance(r_ab, Vector3.zero);
        //Debug.Log(r_ab_norm);
        if (r_ab_norm == 0)
        {
            //Debug.Log("r_ab_norm = 0, error");
        }
        double scalar = U_0_ab * Mathf.Exp((float)(-r_ab_norm / R)) / (R * r_ab_norm);

        return new Vector3((float)(scalar * myPosition.x), (float)(scalar * myPosition.y), (float)(scalar * myPosition.z));
    }

    Vector3 speedBound(Vector3 speed)
    {
        if (Vector3.Distance(speed, Vector3.zero) == 0)
        {
            //Debug.Log("r_ab_norm = 0, error");
        }
        double mySpeed = Vector3.Distance(speed, Vector3.zero);
        if (_maxSpeed < mySpeed)
        {
            double scalar = _maxSpeed / mySpeed;
            speed = (float)scalar * speed;
        }
        return speed;
    }

    private static double SampleGaussian()
    {
        // The method requires sampling from a uniform random of (0,1]
        // but Random.NextDouble() returns a sample of [0,1).
        double x1 = 1 - MRandom.NextDouble();
        double x2 = 1 - MRandom.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
        return y1 * Stddev + Mean;
    }

    // There are no attract designed by paper
    //Vector3 W_ai_gradient(Vector3 myPosition, Vector3 attractPosition)
    //{
    //
    //}
}
