using Unity.Entities;

namespace ECS_MLAgents_v0.Core
{
    public interface IAgentSystem
    {
        /// <summary>
        /// If true, the AgentSystem will perform on the agents
        /// </summary>
        bool Enabled { get; set; }
        
        /// <summary>
        /// The IAgentDecision that will be used to update the Actuators of compatible Entities.
        /// </summary>
        IAgentDecision Decision { get; set; }
        
        /// <summary>
        /// This method defines what are the required ComponentType that are needed on an Entity
        /// to be affected by the AgentSystem. Note : This will reset any filter previously set.
        /// </summary>
        /// <param name="t"> The ComponentType that are required on the Entities.</param>
        void SetNewComponentGroup(params ComponentType[] t);
        
        /// <summary>
        /// Allows the creation of a filter on the Entities affected by the AgentSystem. 
        /// </summary>
        /// <param name="filter"> A ISharedComponentData instance used for filtering</param>
        /// <typeparam name="T"> The type of the ISharedComponentData filter</typeparam>
        void SetFilter<T>(T filter) where T : struct, ISharedComponentData;
        
        /// <summary>
        /// Allows the creation of a filter on the Entities affected by the AgentSystem. 
        /// </summary>
        /// <param name="filterA">The first ISharedComponentData instance used for filtering
        /// </param>
        /// <param name="filterB">The second ISharedComponentData instance used for filtering
        /// </param>
        /// <typeparam name="T0">The type of the first ISharedComponentData filter</typeparam>
        /// <typeparam name="T1">The type of the second ISharedComponentData filter</typeparam>
        void SetFilter<T0, T1>(T0 filterA, T1 filterB)
            where T0 : struct, ISharedComponentData
            where T1 : struct, ISharedComponentData;
        
        /// <summary>
        /// Resets the filter previously set on this AgentSystem
        /// </summary>
        void ResetFilter();
        
        IDecisionRequester DecisionRequester { get; set; }
    }
}
