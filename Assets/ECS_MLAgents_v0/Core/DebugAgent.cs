//#define DEBUG_AGENT
#if DEBUG_AGENT
using UnityEngine;
#endif

namespace ECS_MLAgents_v0.Core
{    
    /*
     * A class for debugging. The messages will only be printed when the define symbol DEBUG_AGENT
     * is on.
     */
    public class Logger
    {
        private string _prefix;
        
        /// <summary>
        /// Constructor for the Logger object.
        /// </summary>
        /// <param name="prefix">The prefix that will be printed at the begining of each message
        /// logged by the Logger instance</param>
        public Logger(string prefix)
        {
            _prefix = prefix;
        }
        
        /// <summary>
        /// Logs the message provided as input using the UnityEngine Debug.Log call.
        /// </summary>
        /// <param name="message"></param>
        public void Log(object message)
        {
            #if DEBUG_AGENT
            Debug.Log(_prefix +" : "+ message);
            #endif
        }
    }
}