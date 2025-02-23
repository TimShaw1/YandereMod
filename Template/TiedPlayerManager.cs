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
            ReviveManager.Instance.ReviveSinglePlayer(tiedPlayer.transform.position, tiedPlayer.playerScript);
            heartbeat.mute = true;
            float elapsedTime2 = 0f;

            while (elapsedTime2 < 0.6f)
            {
                elapsedTime2 += Time.deltaTime;
                yield return null;
            }
            tiedPlayer.playerScript.KillPlayer(Vector3.zero, false, CauseOfDeath.Stabbing);
        }
    }
}
