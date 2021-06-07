using Unity.Mathematics;
using System.Text;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// This struct is used to keep track of where in the shared memory file each section starts
    /// </summary>
    internal struct RLDataOffsets
    {
        // Maximum N Agents
        public int MaxAgents;

        // Decision Steps
        public int DecisionNumberAgentsOffset;
        public int DecisionObsOffset;
        public int DecisionRewardsOffset;
        public int DecisionAgentIdOffset;
        public int DecisionActionMasksOffset;

        // Termination Steps
        public int TerminationNumberAgentsOffset;
        public int TerminationObsOffset;
        public int TerminationRewardsOffset;
        public int TerminationAgentIdOffset;
        public int TerminationStatusOffset;

        // Actions
        public int ContinuousActionOffset;
        public int DiscreteActionOffset;

        // End of file
        public int EndOfDataOffset;

        public static RLDataOffsets FromSharedMemory(BaseSharedMemory sharedMemory, int offset, out string name)
        {
            var startOffset = offset;
            name = sharedMemory.GetString(ref offset);
            int maxAgents = sharedMemory.GetInt(ref offset);

            int NObs = sharedMemory.GetInt(ref offset);
            int totalObsLength = 0; // The number of floats contained in an Agent's observations
            for (int i = 0; i < NObs; i++)
            {
                int prod = 1; // For observation i, what is the number of floats in this obs
                prod *= math.max(1,  sharedMemory.GetInt(ref offset));
                prod *= math.max(1,  sharedMemory.GetInt(ref offset));
                prod *= math.max(1,  sharedMemory.GetInt(ref offset));
                totalObsLength += prod;
                offset += 16; // 4bytes * (3dim prop + 1 type)
            }
            int continuousActionSize = sharedMemory.GetInt(ref offset);
            int numDiscreteBranches = sharedMemory.GetInt(ref offset);
            int numDiscreteActions = 0;
            for (int i = 0; i < numDiscreteBranches; i++)
            {
                numDiscreteActions += sharedMemory.GetInt(ref offset);
            }


            return ComputeOffsets(
                name,
                maxAgents,
                continuousActionSize,
                numDiscreteBranches,
                numDiscreteActions,
                NObs,
                totalObsLength,
                startOffset);
        }

        public static RLDataOffsets FromPolicy(Policy policy, string name, int offset)
        {
            int totalFloatObsPerAgent = 0;
            foreach (int3 shape in policy.SensorShapes)
            {
                totalFloatObsPerAgent += shape.GetTotalTensorSize();
            }
            int numDiscreteActions = numDiscreteActions = policy.DiscreteActionBranches.Sum();;
            int numDiscreteBranches = policy.DiscreteActionBranches.Length;
            int numContinuousActions = policy.ContinuousActionSize;

            return ComputeOffsets(
                name,
                policy.DecisionAgentIds.Length,
                numContinuousActions,
                numDiscreteBranches,
                numDiscreteActions,
                policy.SensorShapes.Length,
                totalFloatObsPerAgent,
                offset
            );
        }

        private static RLDataOffsets ComputeOffsets(
            string name,
            int maxAgents,
            int numContinuousActions,
            int numDiscreteBranches,
            int numDiscreteActions,
            int nbObs,
            int totalFloatObsPerAgent,
            int offset)
        {
            var dataOffsets = new RLDataOffsets();

            offset += 1 + ASCIIEncoding.ASCII.GetByteCount(name);
            dataOffsets.MaxAgents = maxAgents;
            offset += 4; // Max Agent
            offset += 4; //Num Obs
            offset += nbObs * 28; // 4 * (3 + 3 + 1); // 4 bytes, 3 shapes, 3 dim prop, 1 type
            offset += 4; // Continuous action size
            offset += 4; // Discrete action size
            offset += 4 * numDiscreteBranches; // Each branch size

            // Decision Steps Offsets
            dataOffsets.DecisionNumberAgentsOffset = offset;
            offset += 4;
            dataOffsets.DecisionObsOffset = offset;
            offset += 4 * maxAgents * totalFloatObsPerAgent;
            dataOffsets.DecisionRewardsOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.DecisionAgentIdOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.DecisionActionMasksOffset = offset;
            offset += maxAgents * numDiscreteActions;

            // Termination Steps Offsets
            dataOffsets.TerminationNumberAgentsOffset = offset;
            offset += 4;
            dataOffsets.TerminationObsOffset = offset;
            offset += 4 * maxAgents * totalFloatObsPerAgent;
            dataOffsets.TerminationRewardsOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.TerminationStatusOffset = offset;
            offset += maxAgents;
            dataOffsets.TerminationAgentIdOffset = offset;
            offset += 4 * maxAgents;

            // Actions offsets
            dataOffsets.ContinuousActionOffset = offset;
            offset += 4 * maxAgents * numContinuousActions;
            dataOffsets.DiscreteActionOffset = offset;
            offset += 4 * maxAgents * numDiscreteBranches;

            // Offset of the start of the next section
            dataOffsets.EndOfDataOffset = offset;
            return dataOffsets;
        }
    }
}
