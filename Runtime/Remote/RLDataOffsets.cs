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
            var dataOffsets = new RLDataOffsets();
            name = mem.GetString(ref offset);

            int maxAgents = mem.GetInt(ref offset);
            dataOffsets.MaxAgents = maxAgents;
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

            // Decision Steps Offsets
            dataOffsets.DecisionNumberAgentsOffset = offset;
            offset += 4;
            dataOffsets.DecisionObsOffset = offset;
            offset += 4 * maxAgents * totalObsLength;
            dataOffsets.DecisionRewardsOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.DecisionAgentIdOffset = offset;
            offset += 4 * maxAgents;
            if (!isContinuous)
            {
                dataOffsets.DecisionActionMasksOffset = offset;
                offset += maxAgents * totalNumberMasks;
            }

            // Termination Steps Offsets
            dataOffsets.TerminationNumberAgentsOffset = offset;
            offset += 4;
            dataOffsets.TerminationObsOffset = offset;
            offset += 4 * maxAgents * totalObsLength;
            dataOffsets.TerminationRewardsOffset = offset;
            offset += 4 * maxAgents;
            dataOffsets.TerminationStatusOffset = offset;
            offset += maxAgents;
            dataOffsets.TerminationAgentIdOffset = offset;
            offset += 4 * maxAgents;

            // Actions offsets
            dataOffsets.ActionOffset = offset;
            offset += 4 * maxAgents * actionSize;
            dataOffsets.EndOfDataOffset = offset;


            return dataOffsets;
        }

        public static RLDataOffsets FromWorld(MLAgentsWorld world, string name, int offset)
        {
            var dataOffsets = new RLDataOffsets();
            offset = 1 + ASCIIEncoding.ASCII.GetByteCount(name);
            int maxAgents = world.AgentIds.Length;

            // TODO
            return dataOffsets;
        }
    }
}
