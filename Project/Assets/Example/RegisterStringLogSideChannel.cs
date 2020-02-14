using UnityEngine;
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
