namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Indicates the action space type of an Agent.
    /// </summary>
    public enum ActionType : int
    {
        /// <summary>
        /// The Action is expected to be a struct of int or int enums.
        /// More precisely, struct must be convertible to an array of ints by
        /// memory copy.
        /// The number of ints in the struct corresponds to the number of actions,
        /// but the range of values each action can take is given by the "Branch" size.
        /// </summary>
        DISCRETE = 0,

        /// <summary>
        /// The Action is expected to be a struct with only float members.
        /// More precisely, struct must be convertible to an array of float by
        /// memory copy.
        /// </summary>
        CONTINUOUS = 1,
    }
}
