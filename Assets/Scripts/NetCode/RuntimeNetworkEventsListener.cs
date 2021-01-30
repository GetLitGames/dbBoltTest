using Bolt;
using Bolt.Matchmaking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour]
public class RuntimeNetworkEventsListener : GlobalEventListener
{
    public static List<NetworkPlayer> Players = new List<NetworkPlayer>();

    public override void SceneLoadLocalDone(string scene)
    {
        print("it did the thing o.o");
    }

    /// <summary>
    /// Before trying any additional network logic after a disconnect or re-queue or something
    /// You should call this function to make sure all static variables are cleaned up properly
    /// </summary>
    public static void Cleanup()
    {
        Players.Clear();
    }
}
