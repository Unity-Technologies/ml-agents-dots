using System;
#if UNITY_EDITOR
using UnityEditor; // TODO : Delete
#endif
using UnityEngine;// TODO : Delete


namespace Unity.AI.MLAgents
{
    internal unsafe class SharedMemoryCommunicator : IDisposable
    {
        private const float k_TimeOutInSeconds = 15;


        private string m_BaseFileName;
        private int m_CurrentFileNumber = 1;
        private SharedMemoryHeader m_SharedMemoryHeader;
        private SharedMemoryBody m_ShareMemoryBody;

        public bool Active;

        public SharedMemoryCommunicator(string filePath)
        {
            if (filePath == null)
            {
                Active = false;
                return;
            }
            m_BaseFileName = filePath;
            m_SharedMemoryHeader = new SharedMemoryHeader(filePath);
            if (!m_SharedMemoryHeader.Active || !m_SharedMemoryHeader.CheckVersion())
            {
                m_SharedMemoryHeader.Close(); // Python will delete it if it needs it
                Active = false;
                return;
            }
            m_ShareMemoryBody = new SharedMemoryBody(
                m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                false,
                null,
                m_SharedMemoryHeader.SideChannelBufferSize,
                m_SharedMemoryHeader.RLDataBufferSize);

            m_SharedMemoryHeader.UnblockPython();
            Active = true;
        }

        public byte[] ReadAndClearSideChannelData()
        {
            return m_ShareMemoryBody.SideChannelData;
        }

        public void WriteSideChannelData(byte[] data)
        {
            int oldCapacity = m_SharedMemoryHeader.SideChannelBufferSize;
            if (data.Length > oldCapacity - 4) // 4 is the int for the size of the data
            {
                // Add extra capacity with a simple heuristic
                int newCapacity = ArrayUtils.IncreaseArraySizeHeuristic(data.Length);
                m_CurrentFileNumber += 1;
                m_SharedMemoryHeader.FileNumber = m_CurrentFileNumber;
                byte[] rlData = m_ShareMemoryBody.RlData;
                m_ShareMemoryBody.Close();
                m_ShareMemoryBody = new SharedMemoryBody(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    true,
                    null,
                    newCapacity,
                    m_SharedMemoryHeader.RLDataBufferSize
                );
                m_SharedMemoryHeader.SideChannelBufferSize = newCapacity;
                m_ShareMemoryBody.RlData = rlData;
            }
            m_ShareMemoryBody.SideChannelData = data;
        }

        /// <summary>
        /// Writes the data of a world into the shared memory file.
        /// </summary>
        public void WriteWorld(string worldName, MLAgentsWorld world)
        {
            if (!m_SharedMemoryHeader.Active)
            {
                return;
            }
            if (m_ShareMemoryBody.ContainsWorld(worldName))
            {
                m_ShareMemoryBody.WriteWorld(worldName, world);
            }
            else
            {
                // The world needs to register
                int oldTotalCapacity = m_SharedMemoryHeader.RLDataBufferSize;
                int worldMemorySize = RLDataOffsets.FromWorld(world, worldName, 0).EndOfDataOffset;
                m_CurrentFileNumber += 1;
                m_SharedMemoryHeader.FileNumber = m_CurrentFileNumber;
                byte[] channelData = m_ShareMemoryBody.SideChannelData;
                byte[] rlData = m_ShareMemoryBody.RlData;
                m_ShareMemoryBody.Close();
                m_ShareMemoryBody = new SharedMemoryBody(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    true,
                    null,
                    m_SharedMemoryHeader.SideChannelBufferSize,
                    oldTotalCapacity + worldMemorySize
                );
                m_SharedMemoryHeader.RLDataBufferSize = oldTotalCapacity + worldMemorySize;
                if (channelData != null)
                {
                    m_ShareMemoryBody.SideChannelData = channelData;
                }
                if (rlData != null)
                {
                    m_ShareMemoryBody.RlData = rlData;
                }
                // TODO Need to write the offsets
                m_ShareMemoryBody.WriteWorldSpecs(worldName, world);
                m_ShareMemoryBody.WriteWorld(worldName, world);
            }
        }

        /// <summary>
        /// True if Python is ready and False if Python timed out.
        /// </summary>
        public void WaitForPython()
        {
#if UNITY_EDITOR
            int iteration = 0;
            int checkTimeoutIteration = 20000000;
            var t0 = DateTime.Now.Ticks;
#endif
            while (m_SharedMemoryHeader.Active && m_SharedMemoryHeader.Blocked)
            {
#if UNITY_EDITOR
                if (iteration % checkTimeoutIteration == 0)
                {
                    if (1e-7 * (DateTime.Now.Ticks - t0) > k_TimeOutInSeconds)
                    {
                        Debug.LogError("Timeout");
                        Active = false;
                        m_SharedMemoryHeader.Delete();
                        m_ShareMemoryBody.Delete();
                        EditorApplication.isPlaying = false;
                    }
                }
#endif
            }
            if (!m_SharedMemoryHeader.Active)
            {
                Debug.LogError("Communication was closed.");
                Active = false;
                m_SharedMemoryHeader.Delete();
                m_ShareMemoryBody.Delete();
                QuitUnity();
                return;
            }
            while (m_CurrentFileNumber < m_SharedMemoryHeader.FileNumber)
            {
                var tmpData = m_ShareMemoryBody;
                m_CurrentFileNumber += 1;
                m_ShareMemoryBody = new SharedMemoryBody(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    false,
                    tmpData,
                    m_SharedMemoryHeader.SideChannelBufferSize,
                    m_SharedMemoryHeader.RLDataBufferSize);
                tmpData.Delete();
            }
        }

        public bool ReadAndClearResetCommand()
        {
            return m_SharedMemoryHeader.ReadAndClearResetCommand();
        }

        public void SetUnityReady()
        {
            m_SharedMemoryHeader.MarkUnityBlocked();
            m_SharedMemoryHeader.UnblockPython();
        }

        /// <summary>
        /// Loads the action data form the shared memory file to the world
        /// </summary>
        public void LoadWorld(string worldName, MLAgentsWorld world)
        {
            m_ShareMemoryBody.ReadWorld(worldName, world);
        }

        public void Dispose()
        {
            Active = false;
            m_SharedMemoryHeader.Close();
            m_ShareMemoryBody.Delete();
        }

        private void QuitUnity()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
