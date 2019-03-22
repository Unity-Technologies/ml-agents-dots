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

## Future Work

A basic implementation of Python communication using shared memory has been added (OSX only). Communication will need to be made more flexible and ideally will be able to support non-RL use cases.

In future work, we will explore C# refection so the definition of the sensor and actuator can be more flexible. We could create a map of the sensor/actuator struct memory structure and communicate it to python so it can put it into appropriate struct objects. We could also use refection to define which numbers in the sensor correspond to Reward and done signals. 

```csharp
[Serializable]
public struct ShipSensor : IComponentData
{
	[RewardAttribute]
	public float Reward;
	[FloatSensorAttribute(name="Absolute Ship Position")
	public float3 Position;
        
	public quaternion Rotation;
}
```
     
# Alternaltive Approaches

## 1
Another form of API that we could use would be the following : Instead of creating a System with generic types, one would create a `Decision` object with generic types. Instead of declaring a system, we could expose the decision is this manner :

```csharp
Decision = new NNDecision(model);

//...

Entities.ForEach((ref MySensor sensor, ref MyActuator actuator) =>
   {
      Decision.AddToBatch(sensor, actuator);
   }
   Decision.ProcessBatch();
```

This is still compatible with the refection approach to decorate training signals and would simplify greatly the Request Decision mechanism and the filtering of entities since full freedom is given to the developer. On the other hand, this relies on the reference data not changing and the user calling `Decision.ProcessBtch` appropriately. It would also make it harder to keep track of done flags and agent ids for instance. Might make it impossible to use in a job. Would make it a lot easier to have multiple sensors/actuators/cameras.

## 2
We could process the decisions at the level of a Native Array, instantiated and maintained by the user.

```csharp
var sensors = new NativeArray<MySensor>(INITIAL_MEMORY_SIZE, Allocator.Persistent);
var actuators = new NativeArray<MyActuators>(INITIAL_MEMORY_SIZE, Allocator.Persistent);

Decision.Process(sensors, actuators);
```
