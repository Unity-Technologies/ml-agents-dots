## ML-Agents DOTS Installation Guide
Please note that this package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.

## Installation

### Install C# code
 * Create a new Project on Unity 2019.3.0f5
 * Navigate to the new created project folder and add the following entries into `Package/manifest.json` under "Dependencies":
 ```json
 "com.unity.ai.mlagents": "https://github.com/Unity-Technologies/ml-agents-dots.git#release-0.2.0",
 "com.unity.physics": "0.2.4-preview",
 "com.unity.rendering.hybrid": "0.3.3-preview.11",
 "com.unity.burst":"1.3.0-preview.2",
 "com.unity.test-framework.performance": "1.3.3-preview",
 "com.unity.coding": "0.1.0-preview.13"
 ```
 * In your Unity project, you should see the Package Manager resolving the new packages (this may take several minutes).
 * Go to Window -> Package Manager. Select DOTS ML-Agents and import all the samples.



### Install ML-Agents DOTS Python code
 * Clone this repository in a new folder
 * Checkout release-0.2.0 
 ```
 git clone --branch release-0.2.0 https://github.com/Unity-Technologies/ml-agents-dots.git
 ```
 * Run the following command inside the cloned repository:
 ```
 pip3 install -e ./ml-agents-envs~
 ```

### Install ML-Agents Trainer code
 * ML-Agents on DOTS is compatible with version 0.15.1 of the [ml-agents packages](https://github.com/Unity-Technologies/ml-agents/blob/0.15.1).
 * Checkout the ml-agents repository on version 0.15.1
  ```
 git clone --branch release-0.15.1 https://github.com/Unity-Technologies/ml-agents
 ```
 * Run the following command inside the cloned repository:
 ```
 pip3 install -e ./ml-agents-envs
 pip3 install -e ./ml-agents
 ```
 * Modify `./ml-agents/mlagents/trainers/learn.py` by replacing line 24:
   ```python
   from mlagents_envs.environment import UnityEnvironment
   ```
   with
   ```python
   from mlagents_dots_envs.unity_environment import UnityEnvironment
   ```
 * Similarly, modify `./ml-agents/mlagents/trainers/subprocess_env_manager.py` by replacing line 4:
   ```python
   from mlagents_envs.environment import UnityEnvironment
   ```
   with
   ```python
   from mlagents_dots_envs.unity_environment import UnityEnvironment
   ```


## Train using 3DBall
 * From the project window, open the 3DBall scene under Assets\Samples\DOTS ML-Agents\0.2.0-preview\3DBall\Scene\
 * From the ml-agents 0.15.1 repository root, call
 ```
 mlagents-learn --train config/trainer_config.yaml
 ```
 and press play in the Editor.
 If the installation was successful, you should see in the `Ball_DOTS` training results.


## API
One approach to designing ml-agents to be compativle with DOTS would be to use typical API used for example in [Unity.Physics](https://github.com/Unity-Technologies/Unity.Physics) where a "MLAgents World" holds data, processes it and the data can then be retrieved.
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

The job would be called this way or use the `Entities.ForEach` API :

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

Note that this API can also be called outside of a job and used in the main thread to be compatible with OOTS. There is no reliance at all on IComponentData and ECS which means that we do not have to feed the data with blitable structs and could use NativeArrays as well. Here is an example on how to feed sensor data from a NativeSlice:

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
            var tmp = data.GetAction<float3>();
            Debug.Log(data.Entity.Index + "  " + tmp.x);
        }
    }
```
The ActuatorEvent data contains a key (here an entity) to identify the Agent and a `GetAction` method to retrieve the data in the event. This is very similar to how collisions are currently handled in the Physics package.

## UI to create MLAgentsWorld

We currently offer a `MLAgentsWorldSpecs` struct that has a custom inspector drawer (you can add it to a MonoBehaviour to edit the properties of your MLAgentsWorld and even add a neural network for its behavior).
To generate the MLAgentsWorld with the given settings call `MLAgentsWorldSpecs.GetWorld()`.

## Communication Between C# and Python
In order to exchange data with Python, we use shared memory. Python will create a small file that contains information required for starting the communication. The path to the file will be randomly generated and passed by Python to the Unity Executable as command line argument. For in editor training, a default file will be used. Using shared memory allows for faster data exchange and will remove the need to serialize the data to an intermediate format.

__Note__ : The python code for communication is located in [ml-agents-envs~](./ml-agents-envs~).

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
