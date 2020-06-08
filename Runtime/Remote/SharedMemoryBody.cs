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

        public bool ContainsPolicy(string name)
        {
            return m_OffsetDict.ContainsKey(name);
        }

        public void WritePolicy(string name, Policy policy)
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
            foreach (int3 shape in policy.SensorShapes)
            {
                totalFloatObsPerAgent += shape.GetTotalTensorSize();
            }

            // Decision data
            var decisionCount = policy.DecisionCounter.Count;
            SetInt(dataOffsets.DecisionNumberAgentsOffset, decisionCount);
            SetArray(dataOffsets.DecisionObsOffset, policy.DecisionObs, 4 * decisionCount * totalFloatObsPerAgent);
            SetArray(dataOffsets.DecisionRewardsOffset, policy.DecisionRewards, 4 * decisionCount);
            SetArray(dataOffsets.DecisionAgentIdOffset, policy.DecisionAgentIds, 4 * decisionCount);
            if (policy.ActionType == ActionType.DISCRETE)
            {
                SetArray(dataOffsets.DecisionActionMasksOffset, policy.DecisionActionMasks, decisionCount * policy.DiscreteActionBranches.Sum());
            }

            //Termination data
            var terminationCount = policy.TerminationCounter.Count;
            SetInt(dataOffsets.TerminationNumberAgentsOffset, terminationCount);
            SetArray(dataOffsets.TerminationObsOffset, policy.TerminationObs, 4 * terminationCount * totalFloatObsPerAgent);
            SetArray(dataOffsets.TerminationRewardsOffset, policy.TerminationRewards, 4 * terminationCount);
            SetArray(dataOffsets.TerminationAgentIdOffset, policy.TerminationAgentIds, 4 * terminationCount);
            SetArray(dataOffsets.TerminationStatusOffset, policy.TerminationStatus, terminationCount);
        }

        public void WritePolicySpecs(string name, Policy policy)
        {
            m_OffsetDict[name] = RLDataOffsets.FromPolicy(policy, name, m_CurrentEndOffset);
            var offset = m_CurrentEndOffset;
            offset = SetString(offset, name); // Name
            offset = SetInt(offset, policy.DecisionAgentIds.Length); // Max Agents
            offset = SetBool(offset, policy.ActionType == ActionType.CONTINUOUS);
            offset = SetInt(offset, policy.ActionSize);
            if (policy.ActionType == ActionType.DISCRETE)
            {
                foreach (int branchSize in policy.DiscreteActionBranches)
                {
                    offset = SetInt(offset, branchSize);
                }
            }
            offset = SetInt(offset, policy.SensorShapes.Length);
            foreach (int3 shape in policy.SensorShapes)
            {
                offset = SetInt(offset, shape.x);
                offset = SetInt(offset, shape.y);
                offset = SetInt(offset, shape.z);
            }
            m_CurrentEndOffset = offset;
        }

        public void ReadPolicy(string name, Policy policy)
        {
            if (!CanEdit)
            {
                return;
            }
            if (!m_OffsetDict.ContainsKey(name))
            {
                throw new MLAgentsException("Policy not registered");
            }
            var dataOffsets = m_OffsetDict[name];
            SetInt(dataOffsets.DecisionNumberAgentsOffset, 0);
            SetInt(dataOffsets.TerminationNumberAgentsOffset, 0);
            if (policy.ActionType == ActionType.DISCRETE)
            {
                GetArray(dataOffsets.ActionOffset, policy.DiscreteActuators, 4 * policy.DecisionCounter.Count * policy.ActionSize);
            }
            else
            {
                GetArray(dataOffsets.ActionOffset, policy.ContinuousActuators, 4 * policy.DecisionCounter.Count * policy.ActionSize);
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
