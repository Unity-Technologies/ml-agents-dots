## Preview package
This package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.

## API
Another approach to designing ml-agents-dots would be to use typical API used for example in [Unity.Physics](https://github.com/Unity-Technologies/Unity.Physics) where a "MLAgents World" holds data, processes it and the data can then be retrieved. 
The user would access the `MLAgentsWorld` in the main thread :

```csharp
var world = new MLAgentsWorld(
  100,                              // The maximum number of agents that can request a decision per step
  new int3[] { new int3(3, 0, 0) }, // The observation shapes (here, one observation of shape (3,0,0))
  ActionType.CONTINUOUS,            // Continuous = float, Discrete = int
  3);                               // The number of actions
  
world.SubscribeWorldWithBarracudaModel(Name, Model, InferenceDevice);
``` 

The user could then in his own jobs add and retrieve data from the world. Here is an example of a job in which the user populates the sensor data :

```csharp
public struct UserCreateSensingJob : IJobParallelFor
    {
        public NativeArray<Entity> entities;
        public MLAgentsWorld world;

        public void Execute(int i)
        {
            world.RequestDecision(entities[i])
                .SetReward(1.0f)
                .SetObservation(0, new float3(3.0f, 0, 0)); // observation index and then observation struct

        }
    }
```

The job would be called this way :

```csharp
protected override JobHandle OnUpdate(JobHandle inputDeps)
{
    var job = new MyPopulationJob{
	    world = myWorld,
	    entities = ...,
	    sensors = ...,
	    reward = ...,
    }
    return job.Schedule(N_Agents, 64, inputDeps);
}
```

Note that this API can also be called outside of a job and used in the main thread to be compatible with OOTS. There is no reliance at all on IComponentData and ECS which means that we do not have to feed the data with blitable structs and could use NativeArrays as well.

```csharp
var visObs = VisualObservationUtility.GetVisObs(camera, 84, 84, Allocator.TempJob);
world.RequestDecision(entities[i])
  .SetReward(1.0f)
  .SetObservationFromSlice(1, visObs.Slice());
```

In order to retrieve actions, we use a custom job : 

```csharp
public struct UserCreatedActionEventJob : IActuatorJob
    {
        public void Execute(ActuatorEvent data)
        {
            var tmp = new float3();
            data.GetContinousAction(out tmp);
            Debug.Log(data.Entity.Index + "  " + tmp.x);
        }
    }
```
The ActuatorEvent data contains a key (here an entity) to identify the Agent and a `GetContinousAction` or `GetDiscreteAction` method to retrieve the data in the event. This is very similar to how collisions are currently handled in the Physics package.

## UI to create MLAgentsWorld

We currently offer a `MLAgentsWorldSpecs` struct that has a custom inspector drawer (you can add it to a MonoBehaviour to edit the properties of your MLAgentsWorld and even add a neural network for its behavior.
To use the word call `MLAgentsWorldSpecs.GenerateAndRegisterWorld()`.
