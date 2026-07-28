using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct BoardGamePlayerData
{
    public PlayerType PlayerType;
    public int ClientId;
    public int SeatId;
    public int Score;
    public string Name;
}
