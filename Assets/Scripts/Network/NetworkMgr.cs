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
    public void SendMessageToAllClients(uint id, IMessage message)
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
    public void SendMessageToAllClients(MessageId id, IMessage message)
    {
        SendMessageToAllClients((uint)id, message);
    }

    public void SendMessageToHost(uint id, IMessage message)
    {
        if (!NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("只有客户端才能发送消息给服务器");
            return;
        }

        byte[] bytes = message.ToByteArray();
        NgoMgr.Instance.SendMessageToServerRpc(id, bytes);
    }

    public void SendMessageToHost(MessageId id, IMessage message)
    {
        SendMessageToHost((uint)id, message);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    public void OnReceiveMessage(uint id, byte[] data)
    {
        MessageId messageId = (MessageId)id;
        Debug.Log($"收到消息: {messageId}, 数据长度: {data.Length}");

        IMessage message = null;
        switch (messageId)
        {
            case MessageId.GameResultNtf:
                message = GameResultNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<GameResultNtf>(message as GameResultNtf));
                break;
            case MessageId.HostFsmChangeNtf:
                message = HostFsmChangeNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<HostFsmChangeNtf>(message as HostFsmChangeNtf));
                break;
            case MessageId.ClientFsmChangeNtf:
                message = ClientFsmChangeNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ClientFsmChangeNtf>(message as ClientFsmChangeNtf));
                break;
            case MessageId.ReplaceHumanByAIPlayerNtf:
                message = ReplaceHumanByAIPlayerNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ReplaceHumanByAIPlayerNtf>(message as ReplaceHumanByAIPlayerNtf));
                break;
            case MessageId.UpdateLobbyInfoNtf:
                message = UpdateLobbyInfoNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<UpdateLobbyInfoNtf>(message as UpdateLobbyInfoNtf));
                break;
            case MessageId.DealCardsNtf:
                message = DealCardsNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<DealCardsNtf>(message as DealCardsNtf));
                break;
            case MessageId.ChangePlayerTurnNtf:
                message = ChangePlayerTurnNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ChangePlayerTurnNtf>(message as ChangePlayerTurnNtf));
                break;
            case MessageId.ClientProvideLocalInfoNtf:
                message = ClientProvideLocalInfoNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ClientProvideLocalInfoNtf>(message as ClientProvideLocalInfoNtf));
                break;
            case MessageId.ClientChangePlayerReadyNtf:
                message = ClientChangePlayerReadyNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ClientChangePlayerReadyNtf>(message as ClientChangePlayerReadyNtf));
                break;
            case MessageId.ClientLeaveGameNtf:
                message = ClientLeaveGameNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ClientLeaveGameNtf>(message as ClientLeaveGameNtf));
                break;
            case MessageId.HostLeaveGameNtf:
                message = HostLeaveGameNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<HostLeaveGameNtf>(message as HostLeaveGameNtf));
                break;
            case MessageId.ClientEnterGameSceneNtf:
                message = ClientEnterGameSceneNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ClientEnterGameSceneNtf>(message as ClientEnterGameSceneNtf));
                break;
            case MessageId.ShowPopupContentNtf:
                message = ShowPopupContentNtf.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<ShowPopupContentNtf>(message as ShowPopupContentNtf));
                break;
            case MessageId.PlayerDoActionReq:
                message = PlayerActionRequest.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<PlayerActionRequest>(message as PlayerActionRequest));
                break;
            case MessageId.PlayerDoActionRsp:
                message = PlayerActionResponse.Parser.ParseFrom(data);
                EventMgr.Instance?.Trigger(new ReceiveMessageEvent<PlayerActionResponse>(message as PlayerActionResponse));
                break;
            default:
                break;
        }
    }
}
