using Unity.Entities;

namespace ECS_MLAgents_v0.Core
{
    /*
     * This is the ComponentDataWrapper for the Agent Component. It allows to attach an Agent Component
     * to a GameObject in the Unity Editor.
     */
    public class AgentComponent : ComponentDataWrapper<Agent> { }
}
