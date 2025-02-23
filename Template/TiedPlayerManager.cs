using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using yandereMod;

public class TiedPlayerManager : MonoBehaviour
{
    public Camera tiedCamera;

    public DeadBodyInfo tiedPlayer;

    public GameObject yandereNoAI;

    public AudioSource heartbeat;

    private void Start()
    {
        StartCoroutine(WaitThenKill());
    }

    private System.Collections.IEnumerator WaitThenKill()
    {
        float elapsedTime = 0f;

        while (elapsedTime < 5f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yandereAI.WriteToConsole("Cam: " + tiedCamera + " Player: " + tiedPlayer);

        if (tiedCamera != null)
            tiedCamera.enabled = false;

        if (tiedPlayer != null)
        {
            tiedPlayer.playerScript.deadBody.gameObject.SetActive(true);
            ReviveManager.Instance.ReviveSinglePlayer(RoundManager.FindMainEntrancePosition(), tiedPlayer.playerScript, true);
            heartbeat.mute = true;
        }

        NetworkingClass.Instance.disableYandereRoomServerRpc();
    }

    
}
