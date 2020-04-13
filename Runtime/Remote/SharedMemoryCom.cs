using System;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor; // TODO : Delete
#endif
using UnityEngine;// TODO : Delete


namespace Unity.AI.MLAgents
{
    internal unsafe class SharedMemoryCom : IDisposable
    {
        private const float k_TimeOutInSeconds = 15;


        private string m_BaseFileName;
        private int m_CurrentFileNumber = 1;
        private MasterSharedMem m_MasterSharedMem;
        private DataSharedMem m_DataSharedMem;

        public bool Active;

        public SharedMemoryCom(string filePath)
        {
            if (filePath == null)
            {
                Active = false;
                return;
            }
            m_BaseFileName = filePath;
            m_MasterSharedMem = new MasterSharedMem(filePath);
            if (!m_MasterSharedMem.Active || !m_MasterSharedMem.CheckVersion())
            {
                m_MasterSharedMem.Close(); // Python will delete it if it needs it
                Active = false;
                return;
            }
            m_DataSharedMem = new DataSharedMem(
                m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                false,
                null,
                m_MasterSharedMem.SideChannelBufferSize,
                m_MasterSharedMem.RLDataBufferSize);

            m_MasterSharedMem.UnblockPython();
            Active = true;
        }

        public byte[] ReadAndClearSideChannelData()
        {
            return m_DataSharedMem.SideChannelData;
        }

        public void WriteSideChannelData(byte[] data)
        {
            int oldCapacity = m_MasterSharedMem.SideChannelBufferSize;
            if (data.Length > oldCapacity - 4) // 4 is the int for the size of the data
            {
                // Add extra capacity with a simple heuristic
                int newCapacity = ArrayUtils.IncreaseArraySizeHeuristic(data.Length);
                m_CurrentFileNumber += 1;
                m_MasterSharedMem.FileNumber = m_CurrentFileNumber;
                byte[] rlData = m_DataSharedMem.RlData;
                m_DataSharedMem.Close();
                m_DataSharedMem = new DataSharedMem(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    true,
                    null,
                    newCapacity,
                    m_MasterSharedMem.RLDataBufferSize
                );
                m_MasterSharedMem.SideChannelBufferSize = newCapacity;
                m_DataSharedMem.RlData = rlData;
            }
            m_DataSharedMem.SideChannelData = data;
        }

        /// <summary>
        /// Writes the data of a world into the shared memory file.
        /// </summary>
        public void WriteWorld(string worldName, MLAgentsWorld world)
        {
            if (!m_MasterSharedMem.Active)
            {
                return;
            }
            if (m_DataSharedMem.ContainsWorld(worldName))
            {
                m_DataSharedMem.WriteWorld(worldName, world);
            }
            else
            {
                // The world needs to register
                int oldTotalCapacity = m_MasterSharedMem.RLDataBufferSize;
                int worldMemorySize = RLDataOffsets.FromWorld(world, worldName, 0).EndOfDataOffset;
                m_CurrentFileNumber += 1;
                m_MasterSharedMem.FileNumber = m_CurrentFileNumber;
                byte[] channelData = m_DataSharedMem.SideChannelData;
                byte[] rlData = m_DataSharedMem.RlData;
                m_DataSharedMem.Close();
                m_DataSharedMem = new DataSharedMem(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    true,
                    null,
                    m_MasterSharedMem.SideChannelBufferSize,
                    oldTotalCapacity + worldMemorySize
                );
                m_MasterSharedMem.RLDataBufferSize = oldTotalCapacity + worldMemorySize;
                if (channelData != null)
                {
                    m_DataSharedMem.SideChannelData = channelData;
                }
                if (rlData != null)
                {
                    m_DataSharedMem.RlData = rlData;
                }
                // TODO Need to write the offsets
                m_DataSharedMem.WriteWorldSpecs(worldName, world);
                m_DataSharedMem.WriteWorld(worldName, world);
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
            while (m_MasterSharedMem.Active && m_MasterSharedMem.Blocked)
            {
#if UNITY_EDITOR
                if (iteration % checkTimeoutIteration == 0)
                {
                    if (1e-7 * (DateTime.Now.Ticks - t0) > k_TimeOutInSeconds)
                    {
                        Debug.LogError("Timeout");
                        Active = false;
                        m_MasterSharedMem.Delete();
                        m_DataSharedMem.Delete();
                        EditorApplication.isPlaying = false;
                    }
                }
#endif
            }
            if (!m_MasterSharedMem.Active)
            {
                Debug.LogError("Communication was closed.");
                Active = false;
                m_MasterSharedMem.Delete();
                m_DataSharedMem.Delete();
                QuitUnity();
                return;
            }
            while (m_CurrentFileNumber < m_MasterSharedMem.FileNumber)
            {
                var tmpData = m_DataSharedMem;
                m_CurrentFileNumber += 1;
                m_DataSharedMem = new DataSharedMem(
                    m_BaseFileName.PadRight(m_BaseFileName.Length + m_CurrentFileNumber, '_'),
                    false,
                    tmpData,
                    m_MasterSharedMem.SideChannelBufferSize,
                    m_MasterSharedMem.RLDataBufferSize);
                tmpData.Delete();
            }
        }

        public bool ReadAndClearResetCommand()
        {
            return m_MasterSharedMem.ReadAndClearResetCommand();
        }

        public void SetUnityReady()
        {
            m_MasterSharedMem.MarkUnityBlocked();
            m_MasterSharedMem.UnblockPython();
        }

        /// <summary>
        /// Loads the action data form the shared memory file to the world
        /// </summary>
        public void LoadWorld(string worldName, MLAgentsWorld world)
        {
            m_DataSharedMem.ReadWorld(worldName, world);
        }

        public void Dispose()
        {
            Active = false;
            m_MasterSharedMem.Close();
            m_DataSharedMem.Delete();
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
