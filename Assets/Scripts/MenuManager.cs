using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public TMP_Text queueStatus, queueMsg;
    public Button queueBtn, cancelBtn;
    public TMP_InputField nickname;

    void Update()
    {
        nickname.interactable = NetworkMatchmaker.isBusy == false;
        queueBtn.interactable = NetworkMatchmaker.isBusy == false && !string.IsNullOrEmpty(nickname.text);
        queueBtn.GetComponent<TMP_Text>().text = NetworkMatchmaker.isBusy ? "Searching..." : "Enter Queue";

        if (NetworkMatchmaker.QueueTime.IsRunning)
        {
            queueStatus.text = "In Queue [" + NetworkMatchmaker.QueueTime.Elapsed.Minutes.ToString("00") + ":" + NetworkMatchmaker.QueueTime.Elapsed.Seconds.ToString("00") + "]\n" + NetworkMatchmaker.GetPlayersInQueue();
            queueBtn.gameObject.SetActive(false);
            cancelBtn.gameObject.SetActive(true);

            if(NetworkMatchmaker.QueueTime.Elapsed.Seconds > 10 && !NetworkMatchmaker.hasFoundGame && string.IsNullOrEmpty(NetworkMatchmaker.QueueMessage))
            {
                NetworkMatchmaker.StartGameWithLobbiedPlayers(nickname.text);
                print("wow, this is a long ass queue... Whatever lets just start a game with current players, balanced by bots");
            }
        }
        else
        {
            queueStatus.text = string.Empty;
            queueBtn.gameObject.SetActive(true);
            cancelBtn.gameObject.SetActive(false);

            if (NetworkMatchmaker.isBusy && !NetworkMatchmaker.QueueTime.IsRunning) { queueBtn.GetComponent<TMP_Text>().text = NetworkMatchmaker.isConnectingToQueue ? "Joining Queue..." : "Cancelling"; }
        }

        queueMsg.text = NetworkMatchmaker.QueueMessage;
    }
}
