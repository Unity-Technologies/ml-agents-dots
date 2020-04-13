namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Always created by Python
    /// </summary>
    internal class MasterSharedMem : BaseSharedMem
    {
        private const int k_MajorVersion = 0;
        private const int k_MinorVersion = 3;
        private const int k_BugVersion = 0;

        private const int k_Size = 16;

        public MasterSharedMem(string fileName) : base(fileName, false) {}

        public bool Active
        {
            get
            {
                if (!CanEdit)
                {
                    return false;
                }
                return !GetBool(15);
            }
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
            if (CanEdit)
            {
                SetBool(15, true);
            }
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
            if (!CanEdit)
            {
                return false;
            }
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

        public bool CheckVersion()
        {
            int major = GetInt(0);
            SetInt(0, k_MajorVersion);
            int minor = GetInt(4);
            SetInt(4, k_MinorVersion);
            int bug = GetInt(8);
            SetInt(8, k_BugVersion);
            if (major != k_MajorVersion || minor != k_MinorVersion || bug != k_BugVersion)
            {
                return false;
            }
            return true;
        }
    }
}
