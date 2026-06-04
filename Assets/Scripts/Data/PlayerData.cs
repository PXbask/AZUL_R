using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerType
{
    None,
    Human,
    AI,
}

public class PlayerData
{
    public PlayerType PlayerType;
    public int ClientId;
    public int GameId;
    public string Name;
    public bool IsReady;
}
