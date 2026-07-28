using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FsmChangeStateEvent : EventBase
{
    public FsmStateType stateType;
    public INetworkSerializable data;
}
