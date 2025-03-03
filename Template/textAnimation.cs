using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class textAnimation : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;

    public AudioSource glitchSound;

    private float timer = 7.8f;

    void OnEnable()
    {
        textMeshPro.color = new Color(0f, 0f, 0f, 0f);
        StartCoroutine(TextEffect());
    }

    private IEnumerator TextEffect()
    {
        yield return new WaitForSeconds(timer);
        timer = 0.1f;
        glitchSound.Play();
        textMeshPro.color = new Color(0.9f, 0.4f, 0);
        yield return new WaitForSeconds(0.05f);
        //textMeshPro.margin = new Vector4(-800, 800, 0, 0);
        textMeshPro.color = new Color(0.75f, 0.2f, 0);
        yield return new WaitForSeconds(0.05f);
        textMeshPro.color = new Color(0.85f, 0.3f, 0);
        yield return new WaitForSeconds(0.05f);
        //textMeshPro.margin = new Vector4(-1200, 1200, 0, 0);
        yield return new WaitForSeconds(0.05f);
        textMeshPro.color = new Color(0.65f, 0, 0);
        yield return null;
    }
}
