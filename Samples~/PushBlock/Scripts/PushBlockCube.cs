using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public enum PushBlockStatus
{
    UnInitialized,
    Ongoing,
    Success,
}

[GenerateAuthoringComponent]
public struct PushBlockCube : IComponentData
{
    public Entity block;
    public Entity goal;
    public PushBlockStatus status;
    public float3 resetPosition;
    public int stepCount;
}
