namespace Unity.AI.MLAgents
{
    internal enum RemoteCommand : sbyte
    {
        DEFAULT = 0,
        RESET = 1,
        CHANGE_FILE = 2,
        CLOSE = 3
    }

    public enum WorldCommand
    {
        DEFAULT,
        RESET,
        CLOSE
    }
}
