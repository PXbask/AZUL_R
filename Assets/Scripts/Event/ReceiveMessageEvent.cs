using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiveMessageEvent<T> : EventBase where T : IMessage
{
    public T Message;

    public ReceiveMessageEvent(T message)
    {
        Message = message;
    }
}
