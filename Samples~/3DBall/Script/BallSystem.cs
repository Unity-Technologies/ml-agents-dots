using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.AI.MLAgents;
using Unity.Collections;
using Unity.Physics;


public struct AgentData : IComponentData
{
    public float3 BallResetPosition;
    public Entity BallRef;
    public int StepCount;
}

public struct Actuator : IComponentData
{
    public float2 Value;
}

public class BallSystem : JobComponentSystem
{
    private const int maxStep = 1000;
    private const int decisionPeriod = 5;
    private int stepIndex = 0;

    private struct RotateJob : IActuatorJob
    {
        public ComponentDataFromEntity<Actuator> ComponentDataFromEntity;
        public void Execute(ActuatorEvent ev)
        {
            var a = ev.GetContinuousAction<Actuator>();
            ComponentDataFromEntity[ev.Entity] = a;
        }
    }

    public Policy BallPolicy;


    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        if (!BallPolicy.IsCreated){
            return inputDeps;
        }
        stepIndex++;
        if (stepIndex % decisionPeriod == 0)
        {
            return inputDeps;
        }

        var policy = BallPolicy;

        ComponentDataFromEntity<Translation> TranslationFromEntity = GetComponentDataFromEntity<Translation>(isReadOnly: false);
        ComponentDataFromEntity<PhysicsVelocity> VelFromEntity = GetComponentDataFromEntity<PhysicsVelocity>(isReadOnly: false);
        inputDeps = Entities
        .WithNativeDisableParallelForRestriction(TranslationFromEntity)
        .WithNativeDisableParallelForRestriction(VelFromEntity)
        .ForEach((Entity entity, ref Rotation rot, ref AgentData agentData) =>
        {

            var ballPos = TranslationFromEntity[agentData.BallRef].Value;
            var ballVel = VelFromEntity[agentData.BallRef].Linear;
            var platformVel = VelFromEntity[entity];
            bool taskFailed = false;
            bool interruption = false;
            if (ballPos.y - agentData.BallResetPosition.y < -0.7f)
            {
                taskFailed = true;
                agentData.StepCount = 0;
            }
            if (agentData.StepCount > maxStep)
            {
                interruption = true;
                agentData.StepCount = 0;
            }
            if (!interruption && !taskFailed)
            {
                policy.RequestDecision(entity)
                        .SetObservation(0, rot.Value)
                        .SetObservation(1, ballPos - agentData.BallResetPosition)
                        .SetObservation(2, ballVel)
                        .SetObservation(3, platformVel.Angular)
                        .SetReward((0.1f));
            }
            if (taskFailed)
            {
                policy.EndEpisode(entity)
                    .SetObservation(0, rot.Value)
                    .SetObservation(1, ballPos - agentData.BallResetPosition)
                    .SetObservation(2, ballVel)
                    .SetObservation(3, platformVel.Angular)
                    .SetReward(-1f);
            }
            else if (interruption)
            {
                policy.InterruptEpisode(entity)
                    .SetObservation(0, rot.Value)
                    .SetObservation(1, ballPos - agentData.BallResetPosition)
                    .SetObservation(2, ballVel)
                    .SetObservation(3, platformVel.Angular)
                    .SetReward((0.1f));
            }
            if (interruption || taskFailed)
            {
                VelFromEntity[agentData.BallRef] = new PhysicsVelocity();
                TranslationFromEntity[agentData.BallRef] = new Translation { Value = agentData.BallResetPosition };
                rot.Value = quaternion.identity;
            }
            agentData.StepCount++;

        }).Schedule(inputDeps);

        var reactiveJob = new RotateJob
        {
            ComponentDataFromEntity = GetComponentDataFromEntity<Actuator>(isReadOnly: false)
        };
        inputDeps = reactiveJob.Schedule(policy, inputDeps);

        inputDeps = Entities.ForEach((ref Actuator act, ref Rotation rotation) =>
        {
            var rot = math.mul(rotation.Value, quaternion.Euler(0.05f * new float3(act.Value.x, 0, act.Value.y)));
            rotation.Value = rot;
        }).Schedule(inputDeps);

        return inputDeps;
    }

    protected override void OnDestroy()
    {
        BallPolicy.Dispose();
    }
}
