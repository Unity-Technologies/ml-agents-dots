namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Only C# can add new data, but both C# and python can edit it
    /// </summary>
    internal class DataSharedMem : BaseSharedMem
    {
        private int m_SideChannelBufferSize;
        private int m_RlDataBufferSize;
        public DataSharedMem(
            string fileName,
            bool createFile,
            DataSharedMem copyFrom,
            int sideChannelBufferSize,
            int rlDataBufferSize) : base(fileName, createFile, sideChannelBufferSize + sideChannelBufferSize)
        {
            m_SideChannelBufferSize = sideChannelBufferSize;
            m_RlDataBufferSize = rlDataBufferSize;
            if (createFile && copyFrom != null)
            {
                SideChannelData = copyFrom.SideChannelData;
                RlData = copyFrom.RlData;
                // Copy the dict of offsets or refresh them
                // 1 - Regenerate the offsets and add a method to add a new world offset
                // 2 - Copy the offsets from the previous DataSharedMem and move the offsets around when needed
            }
        }

        public byte[] SideChannelData
        {
            get
            {
                int length = GetInt(0);
                return GetBytes(4, length);
            }
            set
            {
                int length = value.Length;
                SetInt(0, length);
                SetBytes(4, value);
            }
        }

        public byte[] RlData
        {
            get { GetBytes(m_SideChannelBufferSize, m_RlDataBufferSize); }
            set {
                SetBytes(m_SideChannelBufferSize, value);
                // TODO : Refresh offsets ?
                }
        }
    }
}
