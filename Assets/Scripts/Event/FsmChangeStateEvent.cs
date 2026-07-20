using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FsmChangeStateEvent : EventBase
{
    public FsmStateType stateType;
    public int data;
}
