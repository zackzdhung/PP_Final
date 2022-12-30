using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;


// TODO: Burst Compiler ? SIMD ?

public struct Pedestrian
{
    public float3 position;
    public float3 velocity;
    public float3 desiredDirection;
    public float desiredSpeed;
    public float maxSpeed;
    public int desiredExitIndex;
    public int midpointIndex;
}

public struct Border
{
    public float3 position;
    public float3 normalVec;
    public float length;
    // TODO: rectangle borders
}

public struct Destination
{
    public float3 position;
    public int midpointsStartIndex;
    public int midpointsLength;
}


// [BurstCompile]
public struct UpdatePedestriansJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Pedestrian> prevPedestrians;
    [WriteOnly]
    public NativeArray<Pedestrian> curPedestrians;
    [ReadOnly]
    public NativeArray<Destination> destinations;
    [ReadOnly]
    public NativeArray<Border> borders;
    [ReadOnly] 
    public NativeArray<float3> midpoints;

    [ReadOnly]
    public float simulationStep;
    [ReadOnly]
    public float destinationForce_relaxationTime;
    [ReadOnly]
    public float pedestrianForce_v;
    [ReadOnly]
    public float pedestrianForce_sigma;
    [ReadOnly]
    public float pedestrianForce_deltaT;
    [ReadOnly]
    public float borderForce_u;
    [ReadOnly]
    public float borderForce_r;
    [ReadOnly]
    public float sight_phi;
    [ReadOnly]
    public float sight_c;


    public void Execute(int index)
    {
        var prevState = prevPedestrians[index];
        var curState = prevState;

        curState.desiredExitIndex = GetDesiredExitIndex(in curState, in destinations);


        curState.midpointIndex = GetMidpointIndex(in curState, in destinations, in midpoints);

        
        curState.desiredDirection = GetDesiredDirection(in curState, in midpoints);

        // curState.desiredDirection = GetDesiredDirection(in curState, in destinations);

        var force = float3.zero;
        force += GetDestinationForce(in curState);
        force += GetPedestrianForce(in curState, index);
        force += GetBorderForce(in curState);
        // TODO: fluctuations ?

        // remove translation on the y-axis
        force.y = 0f;

        // velocity
        curState.velocity += force * simulationStep;
        if (math.length(curState.velocity) > curState.maxSpeed)
            curState.velocity = math.normalizesafe(curState.velocity) * curState.maxSpeed;

        // position
        curState.position += curState.velocity * simulationStep;

        curPedestrians[index] = curState;
    }


    public static int GetDesiredExitIndex(in Pedestrian pedestrian, in NativeArray<Destination> destinations, bool init = false)
    {
        if (!init) return pedestrian.desiredExitIndex;
        int res = 0;
        float curDist = float.MaxValue;
        for (var i = 0; i < destinations.Length; i++)
        {
            var destination = destinations[i];
            var dist = math.length(destination.position - pedestrian.position);
            if (dist < curDist)
            {
                curDist = dist;
                res = i;
            }
        }
        return res;
    }
    
    public static int GetMidpointIndex(in Pedestrian pedestrian, in NativeArray<Destination> destinations, in NativeArray<float3> midpoints, bool init = false)
    {
        if (init)
        {
            return destinations[pedestrian.desiredExitIndex].midpointsStartIndex;
        }
        var toDestVec = midpoints[pedestrian.midpointIndex] - pedestrian.position;
        return math.length(toDestVec) < 5f ? math.min( destinations[pedestrian.desiredExitIndex].midpointsStartIndex + destinations[pedestrian.desiredExitIndex].midpointsLength - 1, pedestrian.midpointIndex + 1) : pedestrian.midpointIndex;
    }


    // public static float3 GetDesiredDirection(in Pedestrian pedestrian, in NativeArray<Destination> destinations)
    public static float3 GetDesiredDirection(in Pedestrian pedestrian, in NativeArray<float3> midpoints)
    {
        // float3 minToDestVector = -pedestrian.position;
        // float minDistance = float.MaxValue;
        //
        // for (int i = 0; i < destinations.Length; i++)
        // {
        //     var destPosition = destinations[i].position;
        //     var toDestVector = destPosition - pedestrian.position;
        //     var destDistance = math.length(toDestVector);
        //
        //     if (destDistance < minDistance)
        //     {
        //         minDistance = destDistance;
        //         minToDestVector = toDestVector;
        //     }
        // }
        
        
        // return math.normalizesafe(minToDestVector);
        
        var destPosition = midpoints[pedestrian.midpointIndex];
        var toDestVector = destPosition - pedestrian.position;
        
        return math.normalizesafe(toDestVector);
    }

    private float3 GetDestinationForce(in Pedestrian pedestrian)
    {
        return (pedestrian.desiredSpeed * pedestrian.desiredDirection - pedestrian.velocity) / destinationForce_relaxationTime;
    }

    private float3 GetPedestrianForce(in Pedestrian pedestrian, int index)
    {
        var pedestrianForce = float3.zero;

        for (int i = 0; i < prevPedestrians.Length; i++)
        {
            if (i == index) continue;

            var beta = prevPedestrians[i];
            var v_beta_deltaT = math.length(beta.velocity) * pedestrianForce_deltaT;
            var nextPos_beta = v_beta_deltaT * beta.desiredDirection;

            var r_ab = pedestrian.position - beta.position;
            var r_ab_norm = math.length(r_ab);
            var r_ab_step_norm = math.length(r_ab - nextPos_beta);
            var r_ab_norm_r_ab_step_norm = r_ab_norm + r_ab_step_norm;

            var b = math.sqrt(math.pow(r_ab_norm_r_ab_step_norm, 2f) - math.pow(v_beta_deltaT, 2f)) / 2f;

            var force = pedestrianForce_v * math.exp(-b / pedestrianForce_sigma) *
                        r_ab_norm_r_ab_step_norm / (4f * b * pedestrianForce_sigma) *
                        (r_ab / r_ab_norm + (r_ab - nextPos_beta) / r_ab_step_norm);

            if (math.dot(pedestrian.desiredDirection, -force) < math.length(force) * math.cos(sight_phi))
                force *= sight_c;
            
            pedestrianForce += force;
        }

        return pedestrianForce;
    }

    private float3 GetBorderForce(in Pedestrian pedestrian)
    {
        var force = float3.zero;

        foreach (var border in borders)
        {
            var r_ab = pedestrian.position - GetNearestPoint(in pedestrian, border);
            var r_ab_norm = math.length(r_ab);
            force += borderForce_u * math.exp(-r_ab_norm / borderForce_r) / (r_ab_norm * borderForce_r) * r_ab;
        }

        return force;
    }

    private static float3 GetNearestPoint(in Pedestrian pedestrian, in Border border)
    {
        // TODO : robust implementation
        if (!(math.length(border.normalVec) > 0)) return border.position;
        
        var pedestrianToBorder = border.position - pedestrian.position;
        var vec = math.dot(pedestrianToBorder, border.normalVec) * border.normalVec;
        var res = pedestrian.position + vec;

        if (math.length(res - border.position) > border.length / 2)
        {
            res = border.position + math.normalize(res - border.position) * border.length / 2;
        }
        
        return res;
    }
}

// [BurstCompile]
public struct UpdatePeopleJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<Pedestrian> pedestrians;


    public void Execute(int index, TransformAccess transformAccess)
    {  
        transformAccess.position = pedestrians[index].position;
    }
}


[Serializable]
public enum ExecutionType
{
    Serial, MultiThread
}


public class SocialForceManager : MonoBehaviour
{
    public ExecutionType executionType;
    [Range(1, 32)]
    public int batchSize;


    private People[] peoples;
    private NativeArray<Pedestrian> prevPedestrians;
    private NativeArray<Pedestrian> curPedestrians;
    private TransformAccessArray peopleTransformAccessArray;
    private Exit[] exits;
    private NativeArray<Destination> destinations;
    private NativeArray<float3> midpoints;
    private Obstacle[] walls;
    private NativeArray<Border> borders;

    private Unity.Mathematics.Random random = new (1u);

    private UpdatePedestriansJob updatePedestriansJob;
    private UpdatePeopleJob updatePeopleJob;

    private ProfilerMarker profilerMarker_SFM = new ("SFM");
    private ProfilerMarker profilerMarker_updatePedestrians = new ("Update Pedestrians");
    private ProfilerMarker profilerMarker_updatePeople = new ("Update People");


    // constants
    // TODO: expose to the inspector ?
    private const float DESIRED_SPEED_MEAN = 1.34f;
    private const float DESIRED_SPEED_STDDEV = .26f;
    private const float MAX_SPEED_FACTOR = 1.3f;
    private const float DESTINATION_FORCE_RELAXATION_TIME = .5f;
    private const float PEDESTRIAN_FORCE_V = 2.1f;
    private const float PEDESTRIAN_FORCE_SIGMA = .3f;
    private const float PEDESTRIAN_FORCE_DELTA_T = 2f;
    private const float BORDER_FORCE_U = 10f;
    private const float BORDER_FORCE_R = .2f;
    private const float SIGHT_PHI = 100f * Mathf.Deg2Rad;
    private const float SIGHT_C = .5f;


    private void Start()
    {
        InitDestinations();
        InitBorders();
        InitPedestrians();
        InitJobs();
    }

    private void FixedUpdate()
    {
        profilerMarker_SFM.Begin();

        // update jobs
        updatePedestriansJob.prevPedestrians = prevPedestrians;
        updatePedestriansJob.curPedestrians = curPedestrians;
        updatePedestriansJob.simulationStep = Time.fixedDeltaTime;

        // update pedestrians
        profilerMarker_updatePedestrians.Begin();

        switch (executionType)
        {
            case ExecutionType.Serial:
            {
                for (int i = 0; i < prevPedestrians.Length; i++)
                    updatePedestriansJob.Execute(i);
                break;
            }
            case ExecutionType.MultiThread:
                updatePedestriansJob.Schedule(prevPedestrians.Length, batchSize).Complete();
                break;
        }

        profilerMarker_updatePedestrians.End();

        // update people
        profilerMarker_updatePeople.Begin();

        switch (executionType)
        {
            case ExecutionType.Serial:
            {
                for (int i = 0; i < curPedestrians.Length; i++)
                    peoples[i].transform.position = curPedestrians[i].position;
                break;
            }
            case ExecutionType.MultiThread:
                updatePeopleJob.pedestrians = curPedestrians;
                updatePeopleJob.Schedule(peopleTransformAccessArray).Complete();
                break;
        }

        profilerMarker_updatePeople.End();

        // swap pedestrians
        NativeArray<Pedestrian> temp;
        temp = prevPedestrians;
        prevPedestrians = curPedestrians;
        curPedestrians = temp;

        profilerMarker_SFM.End();
    }

    private void OnDestroy()
    {
        prevPedestrians.Dispose();
        curPedestrians.Dispose();
        destinations.Dispose();
        midpoints.Dispose();
        borders.Dispose();
        peopleTransformAccessArray.Dispose();
    }


    private void InitPedestrians()
    {
        peoples = FindObjectsOfType<People>();
        prevPedestrians = new NativeArray<Pedestrian>(peoples.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        curPedestrians = new NativeArray<Pedestrian>(peoples.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        peopleTransformAccessArray = new TransformAccessArray(peoples.Length);

        for (int i = 0; i < prevPedestrians.Length; i++)
        {
            
            var state = new Pedestrian
            {
                position = peoples[i].transform.position,
                velocity = float3.zero,
            };

            state.desiredExitIndex = UpdatePedestriansJob.GetDesiredExitIndex(in state, in destinations, true);
            state.midpointIndex = UpdatePedestriansJob.GetMidpointIndex(in state, in destinations, in midpoints, true);
            state.desiredDirection = UpdatePedestriansJob.GetDesiredDirection(in state, in midpoints);
            state.desiredSpeed = SampleGaussian(DESIRED_SPEED_MEAN, DESIRED_SPEED_STDDEV);
            state.maxSpeed = state.desiredSpeed * MAX_SPEED_FACTOR;

            prevPedestrians[i] = state;

            peopleTransformAccessArray.Add(peoples[i].transform);
        }
    }

    private void InitDestinations()
    {
        exits = FindObjectsOfType<Exit>();
        destinations = new NativeArray<Destination>(exits.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        var midpointsNum = 0;

        for (int i = 0; i < destinations.Length; i++)
        {
            for (int j = 0; j < midpoints.Length; j++)
            {
                midpoints[j] = exits[i].midpoints[j].transform.position;
            }

            var destination = new Destination
            {
                position = exits[i].transform.position,
                midpointsStartIndex = midpointsNum,
                midpointsLength = exits[i].midpoints.Length
            };

            destinations[i] = destination;
            midpointsNum += exits[i].midpoints.Length;
        }

        midpoints = new NativeArray<float3>(midpointsNum, Allocator.Persistent);

        var idx = 0;

        foreach (var exit in exits)
        {
            foreach (var midpoint in exit.midpoints)
            {
                midpoints[idx] = midpoint.transform.position;
                idx++;
            }
        }
    }

    private void InitBorders()
    {
        walls = FindObjectsOfType<Obstacle>();
        borders = new NativeArray<Border>(walls.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        for (int i = 0; i < borders.Length; i++)
        {
            var border = new Border
            {
                position = walls[i].transform.position,
                normalVec = walls[i].normalVec,
                length = walls[i].length
            };

            borders[i] = border;
        }
    }

    private void InitJobs()
    {
        updatePedestriansJob = new UpdatePedestriansJob
        {
            destinations = destinations,
            midpoints = midpoints,
            borders = borders,

            destinationForce_relaxationTime = DESTINATION_FORCE_RELAXATION_TIME,
            pedestrianForce_v = PEDESTRIAN_FORCE_V,
            pedestrianForce_sigma = PEDESTRIAN_FORCE_SIGMA,
            pedestrianForce_deltaT = PEDESTRIAN_FORCE_DELTA_T,
            borderForce_u = BORDER_FORCE_U,
            borderForce_r = BORDER_FORCE_R,
            sight_phi = SIGHT_PHI,
            sight_c = SIGHT_C
        };

        updatePeopleJob = new UpdatePeopleJob();
    }


    private float SampleGaussian(float mean, float stddev)
    {
        // The method requires sampling from a uniform random of (0,1]
        // but Random.NextDouble() returns a sample of [0,1).
        var u = random.NextFloat2();
        u = math.float2(1f) - u;

        var y = math.sqrt(-2f * math.log(u.x)) * math.cos(2f * math.PI * u.y);
        return y * stddev + mean;
    }
}
