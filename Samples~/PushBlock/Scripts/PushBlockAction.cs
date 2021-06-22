using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct PushBlockAction: IComponentData{
    public PushBlockAction(int f, int r)
    {
        Forward = f;
        Rotate = r;
    }
    public int Forward;
    public int Rotate;
}
