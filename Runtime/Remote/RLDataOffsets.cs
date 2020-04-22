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
        public int ActionOffset;

        // End of file
        public int EndOfDataOffset;

        public static RLDataOffsets FromMem(BaseSharedMem mem, int offset, out string name)
        {
            var startOffset = offset;
            name = mem.GetString(ref offset);
            int maxAgents = mem.GetInt(ref offset);
            bool isContinuous = mem.GetBool(ref offset);
            int actionSize = mem.GetInt(ref offset);
            int totalNumberMasks = 0;
            if (!isContinuous)
            {
                for (int i = 0; i < actionSize; i++)
                {
                    totalNumberMasks += mem.GetInt(ref offset);
                }
            }
            int NObs = mem.GetInt(ref offset);
            int totalObsLength = 0; // The number of floats contained in an Agent's observations
            for (int i = 0; i < NObs; i++)
            {
                int prod = 1; // For observation i, what is the number of floats in this obs
                prod *= math.max(1,  mem.GetInt(ref offset));
                prod *= math.max(1,  mem.GetInt(ref offset));
                prod *= math.max(1,  mem.GetInt(ref offset));
                totalObsLength += prod;
            }

            return ComputeOffsets(
                name,
                maxAgents,
                isContinuous,
                actionSize,
                NObs,
                totalObsLength,
                totalNumberMasks,
                startOffset);
        }

        public static RLDataOffsets FromWorld(MLAgentsWorld world, string name, int offset)
        {
            bool isContinuous = world.ActionType == ActionType.CONTINUOUS;
            int totalFloatObsPerAgent = 0;
            foreach (int3 shape in world.SensorShapes)
            {
                totalFloatObsPerAgent += shape.GetTotalTensorSize();
            }
            int totalNumberOfMasks = 0;
            if (!isContinuous)
            {
                totalNumberOfMasks = world.DiscreteActionBranches.Sum();
            }

            return ComputeOffsets(
                name,
                world.DecisionAgentIds.Length,
                isContinuous,
                world.ActionSize,
                world.SensorShapes.Length,
                totalFloatObsPerAgent,
                totalNumberOfMasks,
                offset
            );
        }

        private static RLDataOffsets ComputeOffsets(
            string name,
            int maxAgents,
            bool isContinuous,
            int actionSize,
            int nbObs,
            int totalFloatObsPerAgent,
            int totalNumberOfMasks,
            int offset)
        {
            var dataOffsets = new RLDataOffsets();

            offset += 1 + ASCIIEncoding.ASCII.GetByteCount(name);
            dataOffsets.MaxAgents = maxAgents;
            offset += 4; // Max Agent
            offset += 1; //Action Type
            offset += 4; //Action Size
            if (!isContinuous)
            {
                offset += 4 * actionSize; // discrete action branches
            }
            offset += 4; //Num Obs
            offset += nbObs * 4 * 3;

            // Decision Steps Offsets
            dataOffsets.DecisionNumberAgentsOffset = offset;
            offset += 4;
            dataOffsets.DecisionObsOffset = offset;
            offset += 4 * maxAgents * totalFloatObsPerAgent;
            dataOffsets.DecisionRewardsOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.DecisionAgentIdOffset = offset;
            offset += 4 * maxAgents;
            if (!isContinuous)
            {
                dataOffsets.DecisionActionMasksOffset = offset;
                offset += maxAgents * totalNumberOfMasks;
            }

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
            dataOffsets.ActionOffset = offset;
            offset += 4 * maxAgents * actionSize;

            // Offset of the start of the next section
            dataOffsets.EndOfDataOffset = offset;
            return dataOffsets;
        }
    }
}
