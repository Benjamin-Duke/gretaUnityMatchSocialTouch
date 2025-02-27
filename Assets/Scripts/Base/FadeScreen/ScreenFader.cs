using ExperimentUtility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenFader : MonoBehaviour
{
    public bool fadeOnStart = true;
    public float fadeDuration = 2f;
    public Color fadeColor;
    private Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        if(fadeOnStart)
            FadeIn();
    }

    public void PlayFadeIn(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        FadeIn();
    }

    public void PlayFadeOut(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        FadeOut();
    }

    public void FadeIn()
    {
        Fade(1, 0);
    }

    public void FadeOut()
    {
        Fade(0, 1);
    }

    public void Fade(float alphaIn, float alphaOut)
    {
        StartCoroutine(FadeRoutine(alphaIn, alphaOut)); 
    }

    public IEnumerator FadeRoutine(float alphaIn, float alphaOut, float duration = -1)
    {
        float timer = 0;
        float actualDuration;
        if (duration > 0)
        {
            actualDuration = duration;
        }
        else
        {
            actualDuration = fadeDuration;
        }
        while(timer <= actualDuration)
        {
            Color newColor = fadeColor;
            newColor.a = Mathf.Lerp(alphaIn, alphaOut, timer/actualDuration);

            rend.material.SetColor("_Color", newColor);

            timer += Time.deltaTime;
            yield return null;
        }
        Color newColor2 = fadeColor;
        newColor2.a = alphaOut;
        rend.material.SetColor("_Color", newColor2);
    }
}
