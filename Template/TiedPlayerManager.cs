using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using yandereMod;

public class TiedPlayerManager : MonoBehaviour
{
    public Camera tiedCamera;

    public DeadBodyInfo tiedPlayer;

    public GameObject yandereNoAI;

    public AudioSource heartbeat;

    public AudioSource tyingSFX;

    public AudioSource killSFX;

    public static TiedPlayerManager instance;

    public Volume volume;

    private void Start()
    {
        //StartCoroutine(WaitThenKill());
        instance = this;
        tyingSFX.PlayOneShot(tyingSFX.clip);
        StartCoroutine(OpenEyes());

        yandereNoAI = FindFirstObjectByType<YandereEyeTarget>().gameObject;
    }

    private System.Collections.IEnumerator OpenEyes()
    {
        yield return new WaitForSeconds(7.5f);
        ColorAdjustments colorAdjustments;
        volume.profile.TryGet<ColorAdjustments>(out colorAdjustments);

        float elapsedTime = 0f;
        Color startColor = colorAdjustments.colorFilter.value;
        Color targetColor = Color.white;

        while (elapsedTime < 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / 0.5f; // Normalize to [0, 1]
            colorAdjustments.colorFilter.value = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Ensure the final value is exactly white
        colorAdjustments.colorFilter.value = targetColor;

        Task.Factory.StartNew(() => AzureSTT.Main());

        elapsedTime = 0f;

        while (elapsedTime < 60f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (gameObject.activeInHierarchy == true)
            WaitThenKill(true);
    }

    public void ShowText(string text)
    {
        StartCoroutine(TestText(text));
    }

    private System.Collections.IEnumerator TestText(string text)
    {
        yield return new WaitForSeconds(0.5f);
        var textAnim = FindFirstObjectByType<textAnimation>();
        textAnim.gameObject.SetActive(false);
        textAnim.textMeshPro.text = text;
        textAnim.gameObject.SetActive(true);
    }

    public void Kill(bool kill)
    {
        StartCoroutine(WaitThenKill(kill));
        AzureSTT.speechRecognizer.StopContinuousRecognitionAsync();
        AzureSTT.num_gens = 0;
    }

    private System.Collections.IEnumerator WaitThenKill(bool kill)
    {
        float elapsedTime = 0f;
        float duration = 1f; // Move over 5 seconds
        Vector3 startPosition = yandereNoAI.transform.position;
        Vector3 targetPosition = tiedCamera.transform.position;

        // Keep the original Y position
        targetPosition.y = startPosition.y;

        yandereNoAI.GetComponent<Animator>().SetFloat("speedMultiplier", 3.0f);
        killSFX.PlayOneShot(killSFX.clip);

        while (elapsedTime < duration - 0.2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            yandereNoAI.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        yandereAI.WriteToConsole("Cam: " + tiedCamera + " Player: " + tiedPlayer);

        if (tiedCamera != null)
            tiedCamera.enabled = false;

        killSFX.Stop();

        if (tiedPlayer != null)
        {
            tiedPlayer.playerScript.deadBody.gameObject.SetActive(true);
            ReviveManager.Instance.ReviveSinglePlayer(RoundManager.FindMainEntrancePosition(), tiedPlayer.playerScript, kill);
            heartbeat.mute = true;
        }

        NetworkingClass.Instance.disableYandereRoomServerRpc();
    }




}
