using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public TMP_Text players;
    
    void Start()
    {
        
    }
    
    void Update()
    {
        players.text = "Connected Players [" + RuntimeNetworkEventsListener.Players.Count + "]:\n";
        for(int i = 0; i < RuntimeNetworkEventsListener.Players.Count; i++)
        {
            string id = RuntimeNetworkEventsListener.Players[i].Player != null ? RuntimeNetworkEventsListener.Players[i].Player.ConnectionId.ToString() : i.ToString();
            players.text += (RuntimeNetworkEventsListener.Players[i].Player != null ? "|" : "") + "<b>" + id + "</b>: " + RuntimeNetworkEventsListener.Players[i].Nickname + "\n";
        }
        players.text += "=======================\n";
        foreach (var connection in BoltNetwork.Connections)
        {
            players.text += connection != null ? connection.ConnectionId.ToString() : "<null>";
        }
    }
}
