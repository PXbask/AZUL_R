using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessageId : uint
{
    None = 0,

    Ntf = 1,
    GameResultNtf = Ntf + 1,
    HostFsmChangeNtf = Ntf + 2,
    ClientFsmChangeNtf = Ntf + 3,
    ReplaceHumanByAIPlayerNtf = Ntf + 4,
    UpdateLobbyInfoNtf = Ntf + 5,
    DealCardsNtf = Ntf + 6,
    ChangePlayerTurnNtf = Ntf + 7,
    ClientProvideLocalInfoNtf = Ntf + 8,
    ClientChangePlayerReadyNtf = Ntf + 9,
    ClientLeaveGameNtf = Ntf + 10,
    HostLeaveGameNtf = Ntf + 11,
    ClientEnterGameSceneNtf = Ntf + 12,
    ShowPopupContentNtf = Ntf + 13,

    Request = 1000,
    PlayerDoActionReq = Request + 1,

    Response = 2000,
    PlayerDoActionRsp = Response + 1,
}
