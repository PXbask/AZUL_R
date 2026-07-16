using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTable : MonoBehaviour
{
    [SerializeField]
    public int GameId;

    [SerializeField]
    public Transform SeatTrans;

    [SerializeField]
    public Transform BoardTrans;

    [SerializeField]
    public TableDataBinding DataBinding;

    public void Init()
    {
        DataBinding.Init(GameId);
    }
}
