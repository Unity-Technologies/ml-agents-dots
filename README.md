
# ML-Agents DOTS

[ML-Agents on DOTS Proposal Google Doc](https://docs.google.com/document/d/1QnGSjOfLpwaRopbMf9ZDC89oZJuG0Ii6ORA22a5TWzE/edit#heading=h.py1zfmz3396x)

## Installation

 * Create a new Project on Unity 2019.3.0f5
 * To your `Package/manifest.json` add the package :
 ```json
 "com.unity.ai.mlagents": "https://github.com/Unity-Technologies/ml-agents-dots.git#master"
 ```
 and add the registry : 
 ```json
 "registry": "https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-candidates",
 ```
 * In the Package Manager window, select DOTS ML-Agents and import the Samples you need.

## Communication Between C# and Python
In order to exchange data with Python, we use shared memory. Python will create a small file that contains information required for starting the communication. The path to the file will be randomly generated randomly generated and passed by Python to the Unity Executable as command line argument. For in editor training, a default file will be used. Using shared memory would allow faster data exchange and will remove the need to serialize the data to an intermediate format.

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

 - GUID : 16 bytes : The length of the side channel data for the current step (starts at 0)
 - ??? : Side channel data (Size = total side channel capacity - 16 bytes )

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


