using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



[GenerateAuthoringComponent]
public struct Block : IComponentData
{
    public Entity PushingAgent;
    public Entity TargetZone;
}
