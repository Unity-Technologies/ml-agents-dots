## Alternative API
Another approach to designing ml-agents-dots would be to mimic typical API used for example in [Unity.Physics](https://github.com/Unity-Technologies/Unity.Physics) where a "MLAgents World" holds data, processes it and the data can then be retrieved. An example of a simple world is given [here](Assets/DOTS_MLAgents/BCore/MLAgentsWorld.cs)
The user would access the `MLAgentsWorld` in the main thread :

```csharp
var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
var world = sys.GetExistingMLAgentsWorld<TS, TA>("The name of the policy associated");
``` 
The user could then in his own jobs add and retrieve data from the world. Here is an example of a job in which the user populates the sensor data :

```csharp
public struct MyPopulatingJob : IParallelJobFor
{
	public DataCollector dataCollector;
	public NativeArray<TS> sensors;
	public int reward;
	public void Execute(i){
		dataCollector.CollectData(index i, ..., sensors[i], reward);
	}
}
```

The job would be called this way :

```csharp
var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
var world = sys.GetExistingMLAgentsWorld<TS, TA>("The name of the policy associated");

protected override JobHandle OnUpdate(JobHandle inputDeps)
{
    var job = new MyPopulationJob{
	    dataCollector = world.DataCollector;
	    sensors = ...;
	    reward = ...;
    }
    return job.Schedule(N_Agents, 64, inputDeps);
}
```



# ml-agents-dots

This is a proof of concept for DOTS based ML-Agents

## The core code is inside of `DOTS_MLAgents.Core`

### High Level API

`AgentSystem<Sensor, Actuator>` is a ComponentSystem that updates the Actuator based of the data present in Sensor for all of the compatible Entities. The user can create a new `AgentSystem` by defining a class this way :

```csharp
 public class MyAgentSystem : AgentSystem<MySensor, MyActuator> { }
```

The user can modify properties of `MyAgentSystem` to modify which Entities will be affected by MyAgentSystem.
The user can also swap the decision mechanism of the system by modifying the `Decision` property of the System.
To access the instance of MyAgentSystem, use :

```csharp
 World.Active.GetExistingManager<MyAgentSystem>(); 
```

It is the responsibility of the user to create and populate the `MySensor` of each Entity as well as create and use the data in the `MyActuator` of each Entity. `MySensor` and `MyActuator` must be IComponentData struct that only contains [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types) float fields. 
__Note that an Agent IComponentData must be attached to an Entity to be affected by MyAgentSystem.__
     At each call to OnUpdate, the Data from the sensors of compatible entities will be aggregated into a single `NativeArray<float>`. The AgentSystem will then process this data in batch and generate a new `NativeArray<float>` that will be used to populate the Actuator data of all compatible Entities. Other types of data are not supported in this version.
     
### Low Level API
The high level API gathers the data of the sensors and the actuators and passes them into its `IAgentDecision<TS,TA> Decision` property by calling 

```csharp
void BatchProcess([ReadOnly] NativeArray<TS> sensors, NativeArray<TA> actuators, int offset = 0, int size = -1);
```

The sensors and actuators are then processed (either with a neural network, a heuristic or a Python process).
Note that this Object Oriented approach to decision making might not be as fast as other approaches since it requires the data to be copied into NativeArrays. 
For example, the System processing the data on the entities via a `IJobChunck` would be a lot faster but would make it harder to have a low level API.

    
## Example scenes

### SpaceMagic

Press `A`, `S`, `D` to spawn 1, 100 and 1000 new Entities in the scene.
There are 3 random neural networks used to update the acceleration of the spheres based on their position. You can replace the Decision type on each of the system from Neural Network to Heuristic by pressing `U` and `I` for the first one, `J` and `K` for the second and `N` and `M` for the third.

### SpaceWars

Press `A`, `S`, `D` to spawn 1, 100 and 1000 new Entities in the scene. This scene does uses a Neural Network trained to imitate a Heuristic that orient the ships and make them shoot towards the large spherical target.

### ZeroK

This scene is meant to demonstrate training. It uses an External decision mechanism that reads and writes to a file shared with Python. Python converts the messages to the classic mlagents.envs interface. A basic implementation of Python communication using shared memory has been added (OSX only). Communication will need to be made more flexible and ideally will be able to support non-RL use cases.

## Future Work

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
Alternatively, we could use the name of the properties of the sensor instead of relying on specific attributes. This would make the API more flexible since a researcher would not need to implement specific Attributes to satisfy a particular use case.



