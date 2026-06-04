using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateLobbyEvent : EventBase
{
    public ushort AiPort;
    public ushort PlayerPort;
    public int TotalPlayerNum;
    public int PlayerNum;
    public int AiNum;
}
