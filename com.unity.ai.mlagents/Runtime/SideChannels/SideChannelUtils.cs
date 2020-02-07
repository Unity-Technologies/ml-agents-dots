using System.IO;
using System;

namespace Unity.AI.MLAgents.SideChannels
{
    internal static class SideChannelUtils
    {
        /// <summary>
        /// Separates the data received from Python into individual messages for each registered side channel.
        /// </summary>
        /// <param name="sideChannels">A dictionary of channel type to channel.</param>
        /// <param name="dataReceived">The byte array of data received from Python.</param>
        public static void ProcessSideChannelData(SideChannel[] sideChannels, byte[] dataReceived)
        {
            if (dataReceived == null)
            {
                return;
            }
            if (dataReceived.Length == 0)
            {
                return;
            }
            using (var memStream = new MemoryStream(dataReceived))
            {
                using (var binaryReader = new BinaryReader(memStream))
                {
                    while (memStream.Position < memStream.Length)
                    {
                        int channelType = 0;
                        byte[] message = null;
                        try
                        {
                            channelType = binaryReader.ReadInt32();
                            var messageLength = binaryReader.ReadInt32();
                            message = binaryReader.ReadBytes(messageLength);
                        }
                        catch (Exception ex)
                        {
                            throw new MLAgentsException(
                                "There was a problem reading a message in a SideChannel. Please make sure the " +
                                "version of MLAgents in Unity is compatible with the Python version. Original error : "
                                + ex.Message);
                        }
                        bool consumed = false;
                        foreach (var channel in sideChannels)
                        {
                            if (channel.ChannelType() == channelType)
                            {
                                channel.OnMessageReceived(message);
                                consumed = true;
                            }
                        }
                        if (!consumed)
                        {
                            UnityEngine.Debug.Log(string.Format(
                                "Unknown side channel data received. Channel type "
                                + ": {0}", channelType));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grabs the messages that the registered side channels will send to Python at the current step
        /// into a singe byte array.
        /// </summary>
        /// <param name="sideChannels"> A dictionary of channel type to channel.</param>
        /// <returns></returns>
        public static byte[] GetSideChannelMessage(SideChannel[] sideChannels)
        {
            using (var memStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memStream))
                {
                    foreach (var sideChannel in sideChannels)
                    {
                        var messageList = sideChannel.MessageQueue;
                        foreach (var message in messageList)
                        {
                            binaryWriter.Write(sideChannel.ChannelType());
                            binaryWriter.Write(message.Length);
                            binaryWriter.Write(message);
                        }
                        sideChannel.MessageQueue.Clear();
                    }
                    return memStream.ToArray();
                }
            }
        }
    }
}
