using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 管理网络消息传输的功能
/// </summary>
public class NetworkMgr : MonoSingleton<NetworkMgr>
{
    /// <summary>
    /// 发送消息给所有客户端
    /// </summary>
    public void SendMessageToAllClient(uint id, IMessage message)
    {
        if(!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("只有服务器才能发送消息给所有客户端");
            return;
        }

        byte[] bytes = message.ToByteArray();
        NgoMgr.Instance.SendMessageToAllClientRpc(id, bytes);
    }

    /// <summary>
    /// 发送消息给所有客户端
    /// </summary>
    public void SendMessageToAllClient(MessageId id, IMessage message)
    {
        SendMessageToAllClient((uint)id, message);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    public void OnReceiveMessage(uint id, byte[] data)
    {
        MessageId messageId = (MessageId)id;
        switch (messageId)
        {
            case MessageId.GameResultNtf:
                var message = GameResultNtf.Parser.ParseFrom(data);
                EventMgr.Instance.Trigger(new ReceiveMessageEvent<GameResultNtf>(message));
                break;
            default:
                break;
        }
    }
}
