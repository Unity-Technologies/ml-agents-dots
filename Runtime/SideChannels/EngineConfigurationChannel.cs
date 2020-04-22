using System;
using UnityEngine;
using Unity.Entities;

namespace Unity.AI.MLAgents.SideChannels
{
    /// <summary>
    /// Side channel that supports modifying attributes specific to the Unity Engine.
    /// </summary>
    public class EngineConfigurationChannel : SideChannel
    {
        const string k_EngineConfigId = "e951342c-4f7e-11ea-b238-784f4387d1f7";

        /// <summary>
        /// Initializes the side channel. The constructor is internal because only one instance is
        /// supported at a time, and is created by the Academy.
        /// </summary>
        internal EngineConfigurationChannel()
        {
            ChannelId = new Guid(k_EngineConfigId);
        }

        /// <inheritdoc/>
        public override void OnMessageReceived(IncomingMessage msg)
        {
            var width = msg.ReadInt32();
            var height = msg.ReadInt32();
            var qualityLevel = msg.ReadInt32();
            var timeScale = msg.ReadFloat32();
            var targetFrameRate = msg.ReadInt32();

            timeScale = Mathf.Clamp(timeScale, 0.01f, 100);

            Screen.SetResolution(width, height, false);
            QualitySettings.SetQualityLevel(qualityLevel, true);
            Time.timeScale = timeScale;
            Time.captureFramerate = 60;
            Application.targetFrameRate = targetFrameRate;


            var simGroup = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SimulationSystemGroup>();

#if UNITY_EDITOR
            TimeUtils.EnableFixedRateWithCatchUp(simGroup, 1 / 60f, 1f);
#else
            TimeUtils.EnableFixedRateWithCatchUp(simGroup, 1 / 60f, timeScale);
#endif
        }
    }
}
