using Unity.AI.MLAgents;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class BasicAgent : MonoBehaviour
{
    public MLAgentsWorldSpecs BasicSpecs;
    private MLAgentsWorld m_World;
    private Entity m_Entity;

    public float timeBetweenDecisionsAtInference;
    float m_TimeSinceDecision;
    //[HideInInspector]
    public int m_Position;
    const int k_SmallGoalPosition = 7;
    const int k_LargeGoalPosition = 17;
    public GameObject largeGoal;
    public GameObject smallGoal;
    const int k_MinPosition = 0;
    const int k_MaxPosition = 20;
    public const int k_Extents = k_MaxPosition - k_MinPosition;

    void BeginEpisode()
    {
        m_Position = 10;
        transform.position = new Vector3(m_Position - 10f, 0f, 0f);
        smallGoal.transform.position = new Vector3(k_SmallGoalPosition - 10f, 0f, 0f);
        largeGoal.transform.position = new Vector3(k_LargeGoalPosition - 10f, 0f, 0f);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        m_World = BasicSpecs.GetWorld();
        m_World.RegisterWorldWithHeuristic<int>("BASIC", () => { return 2; });
        Academy.Instance.OnEnvironmentReset += BeginEpisode;
        BeginEpisode();
    }

    void FixedUpdate()
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            StepAgent();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                StepAgent();
                m_TimeSinceDecision = 0f;
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }

    void StepAgent()
    {
        // Request a Decision for all agents
        m_World.RequestDecision(m_Entity)
            .SetObservation(0, m_Position)
            .SetReward(-0.01f);

        // Get the action
        NativeHashMap<Entity, int> actions = m_World.GenerateActionHashMap<int>(Allocator.Temp);
        int action = 0;
        actions.TryGetValue(m_Entity, out action);

        // Apply the action
        if (action == 1)
        {
            m_Position -= 1;
        }
        if (action == 2)
        {
            m_Position += 1;
        }
        if (m_Position < k_MinPosition) { m_Position = k_MinPosition; }
        if (m_Position > k_MaxPosition) { m_Position = k_MaxPosition; }
        gameObject.transform.position = new Vector3(m_Position - 10f, 0f, 0f);

        // See if the Agent terminated
        if (m_Position == k_SmallGoalPosition)
        {
            m_World.EndEpisode(m_Entity)
                .SetObservation(0, m_Position)
                .SetReward(0.1f);
            BeginEpisode();
        }

        if (m_Position == k_LargeGoalPosition)
        {
            m_World.EndEpisode(m_Entity)
                .SetObservation(0, m_Position)
                .SetReward(1f);
            BeginEpisode();
        }
    }

    void OnDestroy()
    {
        m_World.Dispose();
    }
}
