using UnityEngine;
using System;
using System.Text;
using Unity.AI.MLAgents;
using Unity.AI.MLAgents.SideChannels;
using Unity.Entities;

public class RegisterStringLogSideChannel : MonoBehaviour
{
    StringLogSideChannel stringChannel;
    public void Awake()
    {
        stringChannel = new StringLogSideChannel();

        Academy.Instance.SubscribeSideChannel(stringChannel);

        Application.logMessageReceived += stringChannel.SendDebugStatementToPython;
    }

    public void OnDestroy()
    {
        Application.logMessageReceived -= stringChannel.SendDebugStatementToPython;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogError("This is a fake error. Space bar was pressed.");
        }
    }
}


public class StringLogSideChannel : SideChannel
{
    public StringLogSideChannel()
    {
        ChannelId = new Guid("621f0a70-4f87-11ea-a6bf-784f4387d1f7");
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
