using System;
using UnityEngine;
using Unity.Entities;

namespace Unity.AI.MLAgents.SideChannels
{
    /// <summary>
    /// Side channel that supports modifying attributes specific to the Unity Engine.
    /// </summary>
    internal class EngineConfigurationChannel : SideChannel
    {
        private enum ConfigurationType : int
        {
            ScreenResolution = 0,
            QualityLevel = 1,
            TimeScale = 2,
            TargetFrameRate = 3,
            CaptureFrameRate = 4
        }

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
        protected override void OnMessageReceived(IncomingMessage msg)
        {
            var messageType = (ConfigurationType)msg.ReadInt32();
            switch (messageType)
            {
                case ConfigurationType.ScreenResolution:
                    var width = msg.ReadInt32();
                    var height = msg.ReadInt32();
                    Screen.SetResolution(width, height, false);
                    break;
                case ConfigurationType.QualityLevel:
                    var qualityLevel = msg.ReadInt32();
                    QualitySettings.SetQualityLevel(qualityLevel, true);
                    break;
                case ConfigurationType.TimeScale:
                    var timeScale = msg.ReadFloat32();
                    timeScale = Mathf.Clamp(timeScale, 1f, 100f);
                    Time.timeScale = timeScale;
                    SetSimulationGroupTime();
                    break;
                case ConfigurationType.TargetFrameRate:
                    var targetFrameRate = msg.ReadInt32();
                    Application.targetFrameRate = targetFrameRate;
                    break;
                case ConfigurationType.CaptureFrameRate:
                    var captureFrameRate = msg.ReadInt32();
                    Time.captureFramerate = captureFrameRate;
                    SetSimulationGroupTime();
                    break;
                default:
                    Debug.LogWarning(
                        "Unknown engine configuration received from Python. Make sure" +
                        " your Unity and Python versions are compatible.");
                    break;
            }
        }

        private void SetSimulationGroupTime()
        {
            var simGroup = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SimulationSystemGroup>();
            if (Time.captureDeltaTime > 0)
            {
                TimeUtils.EnableFixedRateWithRepeat(simGroup, Time.captureDeltaTime, (int)Time.timeScale + 1);
            }
        }
    }
}
