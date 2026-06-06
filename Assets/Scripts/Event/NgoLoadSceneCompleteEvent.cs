using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NgoLoadSceneCompleteEvent : EventBase
{
    public ulong ClientId;
    public string SceneName;
}
