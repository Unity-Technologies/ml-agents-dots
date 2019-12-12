
# ML-Agents DOTS

# Proposed API
Another approach to designing ml-agents-dots would be to mimic typical API used for example in [Unity.Physics](https://github.com/Unity-Technologies/Unity.Physics) where a "MLAgents World" holds data, processes it and the data can then be retrieved. An example of a simple world is given [here](Assets/DOTS_MLAgents/BCore/MLAgentsWorld.cs)
The user would access the `MLAgentsWorld` in the main thread :

```csharp
var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
var world = sys.GetExistingMLAgentsWorld<TS, TA>("The name of the policy associated");
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
                .SetObservation(new float3(3.0f, 0, 0));

        }
    }
```

The job would be called this way :

```csharp
var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
var myWorld = sys.GetExistingMLAgentsWorld<TS, TA>("The name of the policy associated");

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

Note that this API can also be called outside of a job and used in the main thread to be compatible with OOTS. There is no reliance at all on IComponentData which means that we do not have to feed the data with blitable structs but could use NativeArrays / Textures as well.

In order to retrieve actions, here is an example of what the API could look like : 

```csharp
public struct UserCreatedActionEventJob : IActuatorJob
    {
        public void Execute(ActuatorEvent data)
        {
            var tmp = new float3();
            data.GetAction(out tmp);
            Debug.Log(data.Entity.Index + "  " + tmp.x);
        }
    }
```
The ActuatorEvent data contains a key (here an entity) to identify the Agent and a GetAction method to retrieve the data in the event. This is very similar to how collisions are currently handled in the Physics package.

## Communication Between C# and Python
In order to exchange data with Python, we would use shared memory. Python will create a small file that contains information required for starting the communication. The path to the file will be randomly generated randomly generated and passed by Python to the Unity Executable as command line argument. For in editor training, a default file will be used.  Using shared memory would allow faster data exchange and will remove the need to serialize the data to an intermediate format.

### Shared memory layout
#### Header

 - int : 4 bytes : File Length : Size of the file (will change as the file grows) (start at 22)
 - int : 4 bytes : Version number : Unity and Python expecting the same memory layout
 - bool : 1 byte : mutex : Is it Python or Unity’s turn to edit the file (Unity blocked = True, Python Blocked = False) (start at `False`)
 - ushort : 1 byte : Command : [step, reset, change file, close] (starts at `step`)
   - step : DEFAULT : Nothing special
   - reset : RESET : Only from Python to Unity to signal a reset
   - change file : CHANGE_FILE : Can be sent by both C# and Python : Means the file is too short and needs to be changed. Both processes will switch to a new file (append `_` at the end of the old path) and delete the old one after reading the message. Note that to change file, the process must : Create the new file (with more capacity), copy the content of the file at appropriate location, add contexts to the file recompute the offsets to specific locations in the file, set the change file command, flip the mutex on the old file, use the new file and only flip the mutex when ready
 - int : 4 bytes : The total amount of data in the side channel (starts at 4 for the next message length int)
 - int : 4 bytes : The length of the side channel data in bytes for the current step

#### Side channel data

 - int : 4 bytes : The length of the side channel data for the current step (starts at 0)
 - ??? : Side channel data (Size = total side channel capacity - 4 bytes )

#### RL Data section

 - int : 4 bytes : The number of Agent groups in the simulation (starts at 0)
 - For each group : 

   - string : 64 byte : group name
   - int : 4 bytes : maximum number of Agents
   - bool : 1 byte : is action discrete (False) or continuous (True)
   - int : 4 bytes : action space size (continuous) / number of branches (discrete)
     - If discrete only : array of action sizes for each branch (size = n_branches x 4)
   - int : 4 bytes : number of observations
   - For each observation :
     - 3 int : shape (the shape of the tensor observation for one agent)
   - 4 bytes : n_agents at current step
   - ??? bytes : the data : obs,reward,done,max_step,agent_id,masks,action


