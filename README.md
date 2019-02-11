# ml-agents-ecs

This is a proof of concept for ECS based ML-Agents

## The core code is inside of `ECS_MLAgents_v0.Core`

`AgentSystem<Sensor, Actuator>` is a JobComponentSystem that updates the Actuator based of the data present in Sensor for all of the compatible Entities. The user can create a new `AgentSystem` by defining a class this way :

```csharp
 public class MyAgentSystem : AgentSystem<MySensor, MyActuator> { }
```

The user can modify properties of `MyAgentSystem` to modify which Entities will be affected by MyAgentSystem.
To access the instance of MyAgentSystem, use :

```csharp
 World.Active.GetExistingManager<MyAgentSystem>(); 
```

It is the responsibility of the user to create and populate the MySensor of each Entity as well as create and use the data in the MyActuator of each Entity. MySensor and MyActuator must be IComponentData struct that only contains blittable float fields
__Note that an Agent IComponentData must be attached to an Entity to be affected by MyAgentSystem.__
     At each call to OnUpdate, the Data from the sensors of compatible entities will be aggregated into a single NativeArray<float>. The AgentSystem will then process this data in batch and generate a new NativeArray<float> that will be used to populate the Actuator data of all compatible Entities.
    
## Example scenes

### SpaceMagic

Press `A`, `S`, `D` to spawn 1, 100 and 1000 new Entities in the scene.
There are 3 random neural networks used to update the acceleration of the spheres based on their position. You can replace the Decision type on one of the system from Neural Network to Heuristic by pressing `U` and `I`

### SpaceWars

Press `A`, `S`, `D` to spawn 1, 100 and 1000 new Entities in the scene. This scene does not use a Neural Network but a Heuristic that orient the ships and make them shoot towards the large spherical target.
     

