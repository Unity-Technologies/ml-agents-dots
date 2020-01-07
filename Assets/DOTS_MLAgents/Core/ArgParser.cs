using System.IO;


namespace DOTS_MLAgents.Core
{

    public static class ArgParser
    {
        // Used to read Python-provided environment parameters
        public static string ReadSharedMemoryPathFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--memory-path")
                {
                    return args[i + 1];
                }
            }
#if UNITY_EDITOR
            // Try connecting on the default editor port
            var path = Path.Combine(Path.GetTempPath(), "ml-agents", "default");
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