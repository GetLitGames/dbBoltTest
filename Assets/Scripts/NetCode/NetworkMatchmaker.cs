using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;
using System;
using UdpKit;
using Bolt.Matchmaking;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// TODO:
/// - only load game scene/start a game after 30s queue time (testing) or 8/8 players in queue
/// 
/// Overarching goal of Matchmaker:
/// -> Player clicks "join queue"
/// -> Attempt RandomJoinSession
/// -> On fail, attempt CreateSession (requires re-connect as server)
/// -> On any success (as server or client), begin queue timer
/// -> Logic to handle safely cancelling queue
/// -> Logic to handle if queue time exceeds a threshold, start a game filled with bots
/// </summary>

[BoltGlobalBehaviour]
public class NetworkMatchmaker : GlobalEventListener
{
    public static Stopwatch QueueTime { get; } = new Stopwatch();
    public static bool isBusy { get; internal set; }
    public static bool isConnectingToQueue { get; internal set; }
    public static bool hasFoundGame { get; internal set; }
    public static string QueueMessage { get; internal set; }

    bool isSessionHost;

    public static string GetPlayersInQueue() { return BoltMatchmaking.CurrentSession.ConnectionsCurrent + "/" + BoltMatchmaking.CurrentSession.ConnectionsMax; }

    public static void StartGameWithLobbiedPlayers(string nickname)
    {
        var action = OnMatchMakerStartGame.Create(GlobalTargets.Everyone);
        action.Nickname = nickname;
        action.Send();
        FindObjectOfType<NetworkMatchmaker>().OnEvent(action);
    }

    public void StartGame()
    {
        print("startGame called o.o");
        QueueMessage = "Starting Game...";
        if (QueueTime.IsRunning && BoltMatchmaking.CurrentSession != null)
        {
            print("Long queue time, starting game with players in lobby, backfilled with bots o.o");
            QueueMessage = "Long queue time, starting game with players in lobby, backfilled with bots";
            isBusy = false;
            hasFoundGame = true;
            QueueTime.Stop();
            FindObjectOfType<NetworkMatchmaker>().StopAllCoroutines();
            if (BoltNetwork.IsServer) { BoltNetwork.LoadScene("Arena"); }
        }
    }

    #region Unity Scene Events
    IEnumerator CheckSessionStatus()
    {
        //in queue...
        yield return new WaitUntil(() => BoltMatchmaking.CurrentSession != null);
        QueueTime.Restart();
        print("In Queue!  " + BoltMatchmaking.CurrentSession.Id + ": " + BoltMatchmaking.CurrentSession.ConnectionsCurrent + "/" + BoltMatchmaking.CurrentSession.ConnectionsMax);

        //left queue...
        yield return new WaitUntil(() => BoltMatchmaking.CurrentSession == null);
        QueueTime.Stop();
        QueueMessage = string.Empty;
        print("Queue cancelled...");

        yield return new WaitForSecondsRealtime(1f);
        //isBusy = BoltNetwork.IsConnected;

        yield break; //force-exit coroutine
    }

    public void BtnQueueMatchmaking()
    {
        if (isBusy) { return; }

        isBusy = true;
        isConnectingToQueue = true;
        isSessionHost = false;
        QueueMessage = string.Empty;
        RuntimeNetworkEventsListener.Cleanup();
        print("Starting bolt as CLIENT");
        BoltLauncher.StartClient();
    }

    public void BtnCancelQueue()
    {
        print("cancelling queue (disconnecting)...");
        BoltLauncher.Shutdown();
        isBusy = false;
        isConnectingToQueue = false;
        QueueMessage = string.Empty;
        QueueTime.Stop();
    }
    #endregion


    #region Bolt Events
    /// <summary>
    /// Called on BoltLauncher.StartClient/StartServer
    /// </summary>
    public override void BoltStartDone()
    {
        isBusy = true;
        isConnectingToQueue = true;
        if (isSessionHost) //likely the only one in queue/no queue exists, lets make one happen o.o
        {
            print("Bolt connection live! creating queue...");
            BoltMatchmaking.CreateSession("DB Game Session " + BoltNetwork.SessionList.Count, sceneToLoad: SceneManager.GetSceneByBuildIndex(1).name);
        }
        else
        {
            print("Bolt connection live! Joining queue...");
            BoltMatchmaking.JoinRandomSession();
        }

        //cleanup + handle queue time
        isSessionHost = false;
        StopAllCoroutines();
        StartCoroutine(CheckSessionStatus());
    }

    #region Bolt Failiures
    /// <summary>
    /// Called after failing to join a random queue, and are likely the only player in queue, so they will host
    /// </summary>
    public override void SessionConnectFailed(UdpSession session, IProtocolToken token, UdpSessionError errorReason)
    {
        isBusy = true;
        QueueMessage = string.Empty;
        QueueTime.Stop();

        print("<b> Failed to connect to session: </b>" + errorReason);
        if (errorReason != UdpSessionError.Ok) //session full, no sessions found, or otherwise failed to connect
        {
            //disconnect from queue as client
            print("Creating a new session for queue... (closing current connection and re-connecting)");
            BoltLauncher.Shutdown();
            isBusy = true;

            //re-connect to queue as server
            print("Starting new bolt session as SERVER");
            isSessionHost = true;
            isBusy = true;
            BoltLauncher.StartServer();
        }
    }

    /// <summary>
    /// Untested instance, likely will get called if CCU limit reached, no internet
    /// or some other unpredictable reason
    /// </summary>
    public override void SessionCreationFailed(UdpSession session, UdpSessionError errorReason)
    {
        QueueTime.Stop();
        QueueMessage = string.Empty;

        print("<b> Failed to create session '" + session.Id + "': </b>" + errorReason);
    }
    #endregion

    public override void OnEvent(OnMatchMakerStartGame evnt)
    {
        print("oh, hello " + evnt.Nickname);
        RuntimeNetworkEventsListener.Players.Add(new NetworkPlayer(FindObjectOfType<MenuManager>().nickname.text, BoltNetwork.Server));

        foreach (var connection in BoltNetwork.Clients)
        {
            RuntimeNetworkEventsListener.Players.Add(new NetworkPlayer(evnt.Nickname, connection));
        }

        StartGame();
    }

    ////below is useful to find all sessions (not sure when it gets called), and can join a specific or first one found
    ////current plan with matchmaking shouldnt need to rely on this o.o
    //public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
    //{
    //    foreach(var session in sessionList)
    //    {
    //        UdpSession s = session.Value;

    //        if(s.Source == UdpSessionSource.Photon)
    //        {
    //            print(s.Id + ": <b>" + s.ConnectionsCurrent + "</b> players");
    //        }
    //    }
    //}
    #endregion
}
