using System;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;


namespace DOTS_MLAgents.Core
{
    public unsafe class SharedMemoryCom : IDisposable
    {
        private const int k_ApiVersion = 0;
        private const int k_FileLengthOffset = 0;
        private const int k_VersionOffset = 4;
        private const int k_MutexOffset = 8; // Unity blocked = True, Python Blocked = False
        private const int k_CommandOffset = 9;
        private const int k_SideChannelOffset = 10;

        /// <summary>
        /// This struct is used to keep track of where in the shared memory file each section starts
        /// </summary>
        private struct AgentGroupFileOffsets
        {
            public int NumberAgentsOffset;
            public int ObsOffset;
            public int RewardsOffset;
            public int DoneOffset;
            public int MaxStepOffset;
            public int AgentIdOffset;
            public int ActionMasksOffset;
            public int ActionOffset;
        }

        public enum PythonCommand : sbyte
        {
            DEFAULT = 0,
            RESET = 1,
            CHANGE_FILE = 2,
            CLOSE = 3
        }



        private Dictionary<string, AgentGroupFileOffsets> groupOffsets =
            new Dictionary<string, AgentGroupFileOffsets>();
        private string filePath;
        private MemoryMappedViewAccessor accessor;
        private IntPtr accessorPointer;
        private int currentRLDataCapacity = 4; // One int to store the number of groups

        public SharedMemoryCom(string filePath)
        {
            if (filePath == null)
            {
                throw new MLAgentsException("filePath argument of SharedMemoryCom cannot be null.");
            }
            this.filePath = filePath;
            CreateAccessor(filePath);
        }

        private void CreateAccessor(string path)
        {
            if (!File.Exists(path))
            {
                throw new MLAgentsException("The requested file for shared memory communication does not exist.");
            }

            var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
            // This first file created should only contain the version id and the total file capacity
            accessor = mmf.CreateViewAccessor(0, 8, MemoryMappedFileAccess.ReadWrite);
            var capacity = accessor.ReadInt32(k_FileLengthOffset);
            var version = accessor.ReadInt32(k_VersionOffset);
            if (version != k_ApiVersion)
            {
                throw new MLAgentsException(string.Format("API incompatible. Python version : {0}, Unity version : {1}", version, k_ApiVersion));
            }
            // Now that we kow the current capacity of the file, we create a new accessor with the right capacity
            accessor.Dispose();

            accessor = mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);
            mmf.Dispose();

            byte* ptr = (byte*)0;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            accessorPointer = new IntPtr(ptr);
        }

        /// <summary>
        /// Reads the current side channel capacity in the shared memory file
        /// </summary>
        private int SideChannelCapacity()
        {
            return accessor.ReadInt32(k_SideChannelOffset);
        }

        public byte[] ReadAndClearSideChannelData()
        {
            if (accessor.CanWrite)
            {
                var messageSize = accessor.ReadInt32(k_SideChannelOffset + 4);
                if (messageSize == 0)
                {
                    return null;
                }
                var result = new byte[messageSize];
                accessor.ReadArray(k_SideChannelOffset + 8, result, 0, messageSize);
                accessor.WriteArray(k_SideChannelOffset + 4, new byte[SideChannelCapacity()], 0, SideChannelCapacity());
                return result;
            }
            else
            {
                return null;
            }
        }

        public void WriteSideChannelData(byte[] data)
        {
            if (accessor.CanWrite)
            {
                int oldCapacity = SideChannelCapacity();
                if (data.Length > oldCapacity - 4)
                { // 4 is the int for the size of the data
                    int newCapacity = data.Length * 2 + 20;
                    ExtendFile(newCapacity, 0);
                    MoveAllOffsets(newCapacity - oldCapacity);
                }
                accessor.Write(k_SideChannelOffset + 4, data.Length);
                accessor.WriteArray(k_SideChannelOffset + 8, data, 0, data.Length);
            }
        }

        /// <summary>
        /// When the shared memory file is not large enough to sustain communication,
        /// a new larger file is created (file name identical with _ appended). The old
        /// file is marked as dirty (CHANGE_FILE Python command) and the data of the old
        /// file is copied to the new.
        /// Note that there are 2 sections with variable capacity: the side channel data
        /// and the RL data.
        /// </summary>
        /// <param name="channelCapacity"> The new total capacity of the side channel</param>
        /// <param name="additionalRLDataCapacity"> The extra capacity requested for the RL data section</param>
        public void ExtendFile(int channelCapacity, int additionalRLDataCapacity)
        {
            var newFilePath = filePath + "_";
            var oldChannelCapacity = SideChannelCapacity();
            var newTotalCapacity = 0;
            using (var fs = new FileStream(newFilePath, FileMode.Create, FileAccess.Write))
            {
                // Write empty bytes
                var old_size = accessor.ReadInt32(k_FileLengthOffset);
                newTotalCapacity = old_size + (channelCapacity - oldChannelCapacity) + additionalRLDataCapacity;
                fs.Write(new byte[newTotalCapacity], 0, newTotalCapacity);

            }

            var mmf = MemoryMappedFile.CreateFromFile(newFilePath, FileMode.Open);
            var newAccessor = mmf.CreateViewAccessor(0, newTotalCapacity, MemoryMappedFileAccess.ReadWrite);
            mmf.Dispose();

            byte* ptr = (byte*)0;
            newAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            var newAccessorPointer = new IntPtr(ptr);


            // Copy header block
            Buffer.MemoryCopy(accessorPointer.ToPointer(), newAccessorPointer.ToPointer(), k_SideChannelOffset, k_SideChannelOffset);

            // Copy sideChannel
            IntPtr src = IntPtr.Add(accessorPointer, k_SideChannelOffset + 4);
            IntPtr dst = IntPtr.Add(newAccessorPointer, k_SideChannelOffset + 4);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), oldChannelCapacity, oldChannelCapacity);

            // Copy RL DATA
            src = IntPtr.Add(accessorPointer, k_SideChannelOffset + 4 + oldChannelCapacity);
            dst = IntPtr.Add(newAccessorPointer, k_SideChannelOffset + 4 + channelCapacity);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), currentRLDataCapacity, currentRLDataCapacity);

            // Change the capacity of file, side channel and RL data
            newAccessor.Write(k_FileLengthOffset, newTotalCapacity);
            newAccessor.Write(k_SideChannelOffset, channelCapacity);
            currentRLDataCapacity += additionalRLDataCapacity;

            // Mark file as dirty : 
            accessor.Write(k_CommandOffset, (sbyte)PythonCommand.CHANGE_FILE);
            accessor.Write(k_MutexOffset, false);

            // Release accessor
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            accessor.Dispose();

            accessor = newAccessor;
            accessorPointer = newAccessorPointer;
            filePath = newFilePath;
        }

        /// <summary>
        /// Computes the total number of bytes necessary in the shared memory file to
        /// send and receive data for a specific MLAgentsWorld.
        /// </summary>
        public static int GetRequiredCapacity(MLAgentsWorld world)
        {
            // # int : 4 bytes : maximum number of Agents
            // # bool : 1 byte : is action discrete (False) or continuous (True)
            // # int : 4 bytes : action space size (continuous) / number of branches (discrete)
            // # -- If discrete only : array of action sizes for each branch
            // # int : 4 bytes : number of observations
            // # For each observation :
            // # 3 int : shape (the shape of the tensor observation for one agent
            // # start of the section that will change every step
            // # 4 bytes : n_agents at current step
            // # ? Bytes : the data : obs,reward,done,max_step,agent_id,masks,action
            int capacity = 64; // Name
            capacity += 4; // N Max Agents
            capacity += 1; // discrete or continuous
            capacity += 4; // action Size
            if (world.ActionType == ActionType.DISCRETE)
            {
                capacity += 4 * world.ActionSize; // The action branches
            }
            capacity += 4; // number of observations
            capacity += 3 * 4 * world.SensorShapes.Length; // The observation shapes
            capacity += 4; // Number of agents for the current step

            var nAgents = world.Rewards.Length;
            capacity += 4 * world.Sensors.Length;
            capacity += 4 * nAgents;
            capacity += nAgents;
            capacity += nAgents;
            capacity += 4 * nAgents;
            if (world.ActionType == ActionType.DISCRETE)
            {
                foreach (int branch_size in world.DiscreteActionBranches)
                {
                    capacity += branch_size * nAgents;
                }
            }
            capacity += 4 * world.ActionSize * nAgents;

            return capacity;
        }

        /// <summary>
        /// Writes to the shared memory file the Specs of a world (obs size, action size, etc...)
        /// at the specified offset (this data will not change throughout the communication) and
        /// returns an AgentGroupFileOffsets containing the offsets in the shared memory file for
        /// specific sections of the RL data.
        /// </summary>
        private AgentGroupFileOffsets WriteAgentGroupSpecs(string worldName, MLAgentsWorld world, int offset)
        {
            var startingOffsets = new AgentGroupFileOffsets();
            var maxNAgents = world.Rewards.Length;
            var bytesName = System.Text.Encoding.ASCII.GetBytes(worldName);
            sbyte len = (sbyte)bytesName.Length;
            if (len > 63)
            {
                throw new Exception(string.Format("Name of the MLAgentsWorld {0} cannot be greater than 63 characters.", worldName));
            }
            accessor.Write(offset, len);
            accessor.WriteArray(offset + 1, bytesName, 0, len);
            offset += 64;
            accessor.Write(offset, maxNAgents);
            offset += 4;
            accessor.Write(offset, world.ActionType == ActionType.CONTINUOUS);
            offset += 1;
            accessor.Write(offset, world.ActionSize);
            offset += 4;
            if (world.ActionType == ActionType.DISCRETE)
            {
                foreach (int branch_size in world.DiscreteActionBranches)
                {
                    accessor.Write(offset, branch_size);
                    offset += 4;
                }
            }
            accessor.Write(offset, world.SensorShapes.Length);
            offset += 4;
            foreach (int3 shape in world.SensorShapes)
            {
                accessor.Write(offset, shape.x);
                offset += 4;
                accessor.Write(offset, shape.y);
                offset += 4;
                accessor.Write(offset, shape.z);
                offset += 4;
            }
            accessor.Write(offset, 0); // Number of agents at the current step
            startingOffsets.NumberAgentsOffset = offset;
            offset += 4;

            startingOffsets.ObsOffset = offset;
            offset += 4 * world.Sensors.Length;

            startingOffsets.RewardsOffset = offset;
            offset += 4 * maxNAgents;

            startingOffsets.DoneOffset = offset;
            offset += maxNAgents;

            startingOffsets.MaxStepOffset = offset;
            offset += maxNAgents;

            startingOffsets.AgentIdOffset = offset;
            offset += 4 * maxNAgents;

            startingOffsets.ActionMasksOffset = offset;
            if (world.ActionType == ActionType.DISCRETE)
            {
                foreach (int branch_size in world.DiscreteActionBranches)
                {
                    offset += branch_size * maxNAgents;
                }
            }

            startingOffsets.ActionOffset = offset;

            return startingOffsets;

        }

        /// <summary>
        /// Writes the data of a world into the shared memory file.
        /// </summary>
        public void WriteWorld(string worldName, MLAgentsWorld world)
        {
            if (!groupOffsets.ContainsKey(worldName))
            {
                var numberGroupsOffset = k_SideChannelOffset + 4 + SideChannelCapacity();
                var nGroups = accessor.ReadInt32(numberGroupsOffset);
                accessor.Write(numberGroupsOffset, nGroups + 1);


                var requiredCapacity = GetRequiredCapacity(world);
                var oldEndOffset = accessor.ReadInt32(k_FileLengthOffset);
                ExtendFile(SideChannelCapacity(), requiredCapacity);
                groupOffsets[worldName] = WriteAgentGroupSpecs(worldName, world, oldEndOffset);

            }

            var offsets = groupOffsets[worldName];

            // N Agents
            var NAgents = world.AgentCounter.Count;
            accessor.Write(offsets.NumberAgentsOffset, NAgents);


            // Obs
            IntPtr dst = IntPtr.Add(accessorPointer, offsets.ObsOffset);
            IntPtr src = new IntPtr(world.Sensors.GetUnsafePtr());
            int length = world.Sensors.Length * sizeof(float); // Copy everything, might be a better solution
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);

            // Reward
            dst = IntPtr.Add(accessorPointer, offsets.RewardsOffset);
            src = new IntPtr(world.Rewards.GetUnsafePtr());
            length = NAgents * sizeof(float);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);

            // Done
            dst = IntPtr.Add(accessorPointer, offsets.DoneOffset);
            src = new IntPtr(world.DoneFlags.GetUnsafePtr());
            length = NAgents;
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);

            // MaxStep
            dst = IntPtr.Add(accessorPointer, offsets.MaxStepOffset);
            src = new IntPtr(world.MaxStepFlags.GetUnsafePtr());
            length = NAgents;
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);

            // AgentId
            dst = IntPtr.Add(accessorPointer, offsets.AgentIdOffset);
            src = new IntPtr(world.AgentIds.GetUnsafePtr());
            length = NAgents * sizeof(float);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);

            // Masks
            if (world.ActionType == ActionType.DISCRETE)
            {
                dst = IntPtr.Add(accessorPointer, offsets.MaxStepOffset);
                src = new IntPtr(world.ActionMasks.GetUnsafePtr());
                length = 0;
                foreach (int branch_size in world.DiscreteActionBranches)
                {
                    length += branch_size * NAgents;
                }

                Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            }

        }

        private void OnCloseCommand()
        {
            Dispose();
            File.Delete(filePath);
        }

        private void OnChangeFileCommand()
        {
            var oldChannelCapacity = SideChannelCapacity();
            Dispose();
            File.Delete(filePath);
            filePath = filePath + "_";
            CreateAccessor(filePath);
            // The side channel capacity has changed so all the offsets of the RL data
            // must be increased.
            var delta = SideChannelCapacity() - oldChannelCapacity;
            MoveAllOffsets(delta);
        }

        private void MoveAllOffsets(int delta)
        {
            var keys = groupOffsets.Keys.ToList();
            foreach (var k in keys)
            {
                var newOffset = new AgentGroupFileOffsets();
                newOffset.NumberAgentsOffset = groupOffsets[k].NumberAgentsOffset + delta;
                newOffset.ObsOffset = groupOffsets[k].ObsOffset + delta;
                newOffset.RewardsOffset = groupOffsets[k].RewardsOffset + delta;
                newOffset.DoneOffset = groupOffsets[k].DoneOffset + delta;
                newOffset.MaxStepOffset = groupOffsets[k].MaxStepOffset + delta;
                newOffset.AgentIdOffset = groupOffsets[k].AgentIdOffset + delta;
                newOffset.ActionMasksOffset = groupOffsets[k].ActionMasksOffset + delta;
                newOffset.ActionOffset = groupOffsets[k].ActionOffset + delta;
                groupOffsets[k] = newOffset;
            }
        }

        private void WaitOnPython()
        {
            // int max_loop = 20000000;
            int max_loop = 200000000;
            var readyToContinue = false;
            int loopIter = 0;
            while (!readyToContinue)
            {
                loopIter++;
                readyToContinue = accessor.ReadBoolean(k_MutexOffset);
                readyToContinue = readyToContinue || loopIter > max_loop;
                if (loopIter > max_loop)
                {

                    throw new Exception("Missed Communication");
                }
            }
        }

        public void SetUnityReady()
        {
            if (accessor.CanWrite)
            {
                accessor.Write(k_CommandOffset, (sbyte)PythonCommand.DEFAULT);
                accessor.Write(k_MutexOffset, false);
            }
            else
            {
                throw new MLAgentsException("Communication has stopped.");
            }
        }

        public PythonCommand Advance()
        {
            WaitOnPython();

            PythonCommand commandReceived = (PythonCommand)accessor.ReadSByte(k_CommandOffset);
            switch (commandReceived)
            {
                case PythonCommand.RESET:
                    return commandReceived;
                case PythonCommand.CLOSE:
                    OnCloseCommand();
                    return commandReceived;
                case PythonCommand.CHANGE_FILE:
                    OnChangeFileCommand();
                    return Advance();
                default:
                    return commandReceived;
            }
        }

        /// <summary>
        /// Loads the action data form the shared memory file to the world
        /// </summary>
        public void LoadWorld(string worldName, MLAgentsWorld world)
        {
            var offsets = groupOffsets[worldName];
            IntPtr src = IntPtr.Add(accessorPointer, offsets.ActionOffset);
            IntPtr dst = new IntPtr(world.DiscreteActuators.GetUnsafePtr());
            if (world.ActionType == ActionType.CONTINUOUS)
            {
                dst = new IntPtr(world.ContinuousActuators.GetUnsafePtr());
            }
            int length = world.AgentCounter.Count * world.ActionSize * sizeof(float);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
        }

        public void Dispose()
        {
            if (accessor.CanWrite)
            {
                accessor.Write(k_CommandOffset, (sbyte)PythonCommand.CLOSE);
                accessor.Write(k_MutexOffset, false);
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                accessor.Dispose();
            }
        }
    }
}
