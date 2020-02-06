using System.Text;
using UnityEngine;

namespace Unity.AI.MLAgents.SideChannels
{
    public class StringLogSideChannel : SideChannel
    {
        public override int ChannelType()
        {
            return (int)SideChannelType.UserSideChannelStart + 1;
        }

        public override void OnMessageReceived(byte[] data)
        {
            var receivedString = Encoding.ASCII.GetString(data);
            Debug.Log("From Python : " + receivedString);
        }

        public void SendDebugStatementToPython(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
            {
                var stringToSend = type.ToString() + ": " + logString + "\n" + stackTrace;
                var encodedString = Encoding.ASCII.GetBytes(stringToSend);
                base.QueueMessageToSend(encodedString);
            }
        }
    }
}