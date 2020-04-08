namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Always created by Python
    /// </summary>
    internal class MasterSharedMem : BaseSharedMem
    {
        private const int k_Size = 16;

        public MasterSharedMem(string fileName) : base(fileName, false) {}

        public bool Active
        {
            get { return GetBool(15); }
        }

        public int FileNumber
        {
            get { return GetInt(16); }
            set { SetInt(16, value); }
        }

        public bool Blocked
        {
            get { return GetBool(12); }
        }

        new public void Close()
        {
            SetBool(15, true);
            base.Close();
        }

        public void MarkUnityBlocked()
        {
            SetBool(12, true);
        }

        public void UnblockPython()
        {
            SetBool(13, false);
        }

        public bool ReadAndClearResetCommand()
        {
            var result = GetBool(14);
            SetBool(14, false);
            return result;
        }

        public int SideChannelBufferSize
        {
            get { return GetInt(20); }
            set { SetInt(20, value); }
        }

        public int RLDataBufferSize
        {
            get { return GetInt(24); }
            set { SetInt(24, value); }
        }
    }
}
