using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayer
{
    public string Nickname { get; }
    public BoltConnection Player { get; }

    public NetworkPlayer(string nickname, BoltConnection player)
    {
        Nickname = nickname;
        Player = player;
        Debug.Log("Player <b>'" + nickname + "'</b> assigned and stored");
    }
}
