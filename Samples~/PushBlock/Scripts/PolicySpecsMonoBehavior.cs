using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.MLAgents;
using Unity.Entities;
using Unity.Mathematics;

public class PolicySpecsMonoBehavior : MonoBehaviour
{

    public PolicySpecs PushBlockPolicySpecs;

    // Start is called before the first frame update
    void Start()
    {
        Policy pushBlockPolicy = PushBlockPolicySpecs.GetPolicy();
        if (PushBlockPolicySpecs.PolicyProcessorType == PolicyProcessorType.None){
            pushBlockPolicy.RegisterPolicyWithHeuristic<float, PushBlockAction>(PushBlockPolicySpecs.Name, discreteHeuristic:() => { 
                
                int forward = 1;
                if (Input.GetKey(KeyCode.W)){ forward = 2;}
                else if (Input.GetKey(KeyCode.S)){ forward = 0;}
                int rotate = 1;
                if (Input.GetKey(KeyCode.D)){ rotate = 2;}
                else if (Input.GetKey(KeyCode.A)){ rotate = 0;}
                return new PushBlockAction(forward,rotate); 
                });
        }
        foreach (var w in World.All){
            var s = w.GetExistingSystem<PushBlockCubeMoveSystem>();
            if (s != null){
                s.PushBlockPolicy = pushBlockPolicy;
            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
