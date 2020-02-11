using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.AI.MLAgents;
using Unity.Collections;
using Barracuda;
using System;


// Alternatively, we could just have a struct that contains the WorldSpecs with a nice inspector
// for the users to add to their monobehaviors. 
public class WorldCreator : MonoBehaviour{

    public string name;
    [Space]
    [Space]
    public int NumberAgents;
    public ActionType actionType;
    public int3[] obsShapes;
    public int actionSize;
    public int[] discreteActionBranches = null;
    [Space]
    [Space]
    public NNModel Model;
    public InferenceDevice InferenceDevice;

    // Seems impossible, but ideally, this should run before a user tries to get the world
    public void Initialize(){
        var sys = World.Active.GetOrCreateSystem<MLAgentsSystem>();
        var world = new MLAgentsWorld(NumberAgents, actionType, obsShapes, actionSize, discreteActionBranches);
        sys.SubscribeWorldWithBarracudaModel(name, world, Model, InferenceDevice);
        WorldRegistry.Put(name, world);
    }
}

public static class WorldRegistry{
    static NativeHashMap<NativeString64, int> m_NameToIndex;
    static MLAgentsWorld[] m_Worlds;
    static bool m_Initialized;

    public static void Put(string name, MLAgentsWorld world){
        if (!m_Initialized){
            m_Initialized = true;
            m_NameToIndex = new NativeHashMap<NativeString64, int>(10, Allocator.Persistent);
            m_Worlds = new MLAgentsWorld[0];
            var wc = GameObject.FindObjectsOfType<WorldCreator>();
            foreach (var w in wc)
                w.Initialize();
        }
        // This needs some bugproofing
        Array.Resize<MLAgentsWorld>(ref m_Worlds, m_Worlds.Length + 1);
        m_NameToIndex[new NativeString64(name)] = m_Worlds.Length -1;
        m_Worlds[m_Worlds.Length -1] = world;
    }

    public static MLAgentsWorld Get(string name){
        if (!m_Initialized){
            m_Initialized = true;
            m_NameToIndex = new NativeHashMap<NativeString64, int>(10, Allocator.Persistent);
            m_Worlds = new MLAgentsWorld[0];
            var wc = GameObject.FindObjectsOfType<WorldCreator>();
            foreach (var w in wc)
                w.Initialize();
        }
        int index = -1;
        m_NameToIndex.TryGetValue(new NativeString64(name), out index);
        return m_Worlds[index];
    }

    public static void Dispose(){
        m_NameToIndex.Dispose();
        m_Worlds = null;
        m_Initialized = false;
    }

}


