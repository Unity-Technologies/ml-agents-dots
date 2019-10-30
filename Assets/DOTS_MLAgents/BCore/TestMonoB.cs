using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using DOTS_MLAgents.Core;

public class TestMonoB : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var ent = World.Active.EntityManager.CreateEntity();
        var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
        var world = sys.GetExistingMLAgentsWorld<float3, float4>("test");
        world.Spawn(ent);
        world.SetSensor(ent, new float3(2, 2, 2));
        // Debug.Log(world.Actuators[0] + "   " + world.Sensors[0] + "   " + world.Actuators[1]);
        // Debug.Log(world.GetActuator<float4>(ent));
    }
}
