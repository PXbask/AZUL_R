using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessageId : uint
{
    None = 0,

    Ntf = 1,
    GameResultNtf = Ntf + 1,
}
