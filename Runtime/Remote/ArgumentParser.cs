using System.IO;


namespace Unity.AI.MLAgents
{
    internal static class ArgumentParser
    {
        private const string k_MemoryFileArgument = "--memory-path";
        private const string k_MemoryFilesDirectory = "ml-agents";
        private const string k_DefaultFileName = "default";

        /// <summary>
        /// Used to read Python-provided environment parameters. Will return the
        /// string corresponding to the path to the shared memory file.
        /// </summary>
        /// <returns> Path to the shared memroy file or null if no file is available</returns>
        public static string ReadSharedMemoryPathFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == k_MemoryFileArgument)
                {
                    return args[i + 1];
                }
            }
#if UNITY_EDITOR
            // Try connecting on the default shared memory file
            var path = Path.Combine(
                Path.GetTempPath(),
                k_MemoryFilesDirectory,
                k_DefaultFileName);
            if (File.Exists(path))
            {
                return path;
            }
            return null;
#else
            // This is an executable, so we don't try to connect if no argument was passed.
            return null;
#endif
        }
    }
}
