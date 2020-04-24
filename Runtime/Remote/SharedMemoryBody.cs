using System.Collections.Generic;
using Unity.Mathematics;


namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Only C# can add new data, but both C# and python can edit it
    /// </summary>
    internal class SharedMemoryBody : BaseSharedMemory
    {
        private Dictionary<string, RLDataOffsets> m_OffsetDict = new Dictionary<string, RLDataOffsets>();
        private int m_SideChannelBufferSize;
        private int m_RlDataBufferSize;
        private int m_CurrentEndOffset;
        public SharedMemoryBody(
            string fileName,
            bool createFile,
            SharedMemoryBody copyFrom,
            int sideChannelBufferSize,
            int rlDataBufferSize) : base(fileName, createFile, sideChannelBufferSize + rlDataBufferSize)
        {
            m_SideChannelBufferSize = sideChannelBufferSize;
            m_RlDataBufferSize = rlDataBufferSize;
            m_CurrentEndOffset = m_SideChannelBufferSize;
            if (createFile && copyFrom != null)
            {
                SideChannelData = copyFrom.SideChannelData;
                RlData = copyFrom.RlData;
            }
            if (!createFile)
            {
                RefreshOffsets();
            }
        }

        private void RefreshOffsets()
        {
            m_OffsetDict.Clear();
            int offset = m_SideChannelBufferSize;
            while (offset < m_SideChannelBufferSize + m_RlDataBufferSize)
            {
                string name = null;
                if (GetInt(offset) == 0)
                {
                    return;
                }
                var dataOffset = RLDataOffsets.FromSharedMemory(this, offset, out name);
                m_OffsetDict[name] = dataOffset;
                offset = dataOffset.EndOfDataOffset;
                m_CurrentEndOffset = offset;
            }
        }

        public bool ContainsWorld(string name)
        {
            return m_OffsetDict.ContainsKey(name);
        }

        public void WriteWorld(string name, MLAgentsWorld world)
        {
            if (!CanEdit)
            {
                return;
            }
            if (!m_OffsetDict.ContainsKey(name))
            {
                throw new MLAgentsException("TODO");
            }
            var dataOffsets = m_OffsetDict[name];
            int totalFloatObsPerAgent = 0;
            foreach (int3 shape in world.SensorShapes)
            {
                totalFloatObsPerAgent += shape.GetTotalTensorSize();
            }

            // Decision data
            var decisionCount = world.DecisionCounter.Count;
            SetInt(dataOffsets.DecisionNumberAgentsOffset, decisionCount);
            SetArray(dataOffsets.DecisionObsOffset, world.DecisionObs, 4 * decisionCount * totalFloatObsPerAgent);
            SetArray(dataOffsets.DecisionRewardsOffset, world.DecisionRewards, 4 * decisionCount);
            SetArray(dataOffsets.DecisionAgentIdOffset, world.DecisionAgentIds, 4 * decisionCount);
            if (world.ActionType == ActionType.DISCRETE)
            {
                SetArray(dataOffsets.DecisionActionMasksOffset, world.DecisionActionMasks, decisionCount * world.DiscreteActionBranches.Sum());
            }

            //Termination data
            var terminationCount = world.TerminationCounter.Count;
            SetInt(dataOffsets.TerminationNumberAgentsOffset, terminationCount);
            SetArray(dataOffsets.TerminationObsOffset, world.TerminationObs, 4 * terminationCount * totalFloatObsPerAgent);
            SetArray(dataOffsets.TerminationRewardsOffset, world.TerminationRewards, 4 * terminationCount);
            SetArray(dataOffsets.TerminationAgentIdOffset, world.TerminationAgentIds, 4 * terminationCount);
            SetArray(dataOffsets.TerminationStatusOffset, world.TerminationStatus, terminationCount);
        }

        public void WriteWorldSpecs(string name, MLAgentsWorld world)
        {
            m_OffsetDict[name] = RLDataOffsets.FromWorld(world, name, m_CurrentEndOffset);
            var offset = m_CurrentEndOffset;
            offset = SetString(offset, name); // Name
            offset = SetInt(offset, world.DecisionAgentIds.Length); // Max Agents
            offset = SetBool(offset, world.ActionType == ActionType.CONTINUOUS);
            offset = SetInt(offset, world.ActionSize);
            if (world.ActionType == ActionType.DISCRETE)
            {
                foreach (int branchSize in world.DiscreteActionBranches)
                {
                    offset = SetInt(offset, branchSize);
                }
            }
            offset = SetInt(offset, world.SensorShapes.Length);
            foreach (int3 shape in world.SensorShapes)
            {
                offset = SetInt(offset, shape.x);
                offset = SetInt(offset, shape.y);
                offset = SetInt(offset, shape.z);
            }
            m_CurrentEndOffset = offset;
        }

        public void ReadWorld(string name, MLAgentsWorld world)
        {
            if (!CanEdit)
            {
                return;
            }
            if (!m_OffsetDict.ContainsKey(name))
            {
                throw new MLAgentsException("World not registered");
            }
            var dataOffsets = m_OffsetDict[name];
            SetInt(dataOffsets.DecisionNumberAgentsOffset, 0);
            SetInt(dataOffsets.TerminationNumberAgentsOffset, 0);
            if (world.ActionType == ActionType.DISCRETE)
            {
                GetArray(dataOffsets.ActionOffset, world.DiscreteActuators, 4 * world.DecisionCounter.Count * world.ActionSize);
            }
            else
            {
                GetArray(dataOffsets.ActionOffset, world.ContinuousActuators, 4 * world.DecisionCounter.Count * world.ActionSize);
            }
        }

        public byte[] SideChannelData
        {
            get
            {
                if (!CanEdit)
                {
                    return null;
                }
                int length = GetInt(0);
                if (length == 0)
                {
                    return null;
                }
                return GetBytes(4, length);
            }
            set
            {
                int length = value.Length;
                SetInt(0, length);
                SetBytes(4, value);
            }
        }

        public byte[] RlData
        {
            get
            {
                if (!CanEdit)
                {
                    return null;
                }
                if (m_RlDataBufferSize == 0)
                {
                    return null;
                }
                return GetBytes(m_SideChannelBufferSize, m_RlDataBufferSize);
            }
            set
            {
                SetBytes(m_SideChannelBufferSize, value);
                RefreshOffsets();
            }
        }
    }
}
