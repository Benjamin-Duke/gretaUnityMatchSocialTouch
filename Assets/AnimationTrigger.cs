using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using FMODUnity;

public class AnimationTrigger : MonoBehaviour
{
    public Animator animator;
    public Rig rigToBlend;
    //public AnimationCurve rigBlendCurve;

    private bool animationPlayed = false;
    private string currentAnimStateName = "";
    private float blendTimer = 0f;
    //private float blendDuration = 1f; 

    void Start()
    {
        animator.SetBool("animCaresse", false);
        animator.SetBool("animFrot", false);
        animator.SetBool("animHit", false);
        animator.SetBool("animTap", false);
        animator.SetBool("animFree", false);

        if (rigToBlend != null)
            rigToBlend.weight = 0f;

        // if (rigBlendCurve == null || rigBlendCurve.length == 0)
        // {
        //     rigBlendCurve = new AnimationCurve(
        //         new Keyframe(0f, 0f),
        //         new Keyframe(0.3f, 1f),
        //         new Keyframe(0.8f, 1f),
        //         new Keyframe(1f, 0f)
        //     );
        // }

    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            animator.SetBool("animCaresse", true);
            currentAnimStateName = "caresse";
            animationPlayed = true;
            blendTimer = 0f;
        }

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            animator.SetBool("animFrot", true);
            currentAnimStateName = "frot";
            animationPlayed = true;
            blendTimer = 0f;
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            animator.SetBool("animTap", true);
            currentAnimStateName = "tap";
            animationPlayed = true;
            blendTimer = 0f;
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            animator.SetBool("animHit", true);
            currentAnimStateName = "hit";
            animationPlayed = true;
            blendTimer = 0f;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animator.SetBool("animFree", true);
            currentAnimStateName = "libre";
            animationPlayed = true;
            blendTimer = 0f;
        }

        if (animationPlayed)
        {
            UpdateRigWeight();
        }


    }

    void UpdateRigWeight()
    {
        int layerIndex = 2;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);

        //Debug.Log("Current Animation State: " + stateInfo.IsName(currentAnimStateName) + " - " + currentAnimStateName);

        if (stateInfo.IsName(currentAnimStateName))
        {
            // float t = Mathf.Clamp01(stateInfo.normalizedTime);

            // // Évalue la courbe pour déterminer le poids cible
            // float targetWeight = rigBlendCurve.Evaluate(t);

            // // Blend en douceur sans aller-retour brusque
            // rigToBlend.weight = Mathf.Lerp(rigToBlend.weight, targetWeight, Time.deltaTime * 5f);

            Debug.Log("Animation in progress: " + currentAnimStateName);
            Debug.Log("Normalized Time: " + stateInfo.normalizedTime);
            if (stateInfo.normalizedTime >= 0.95f)
            {
                Debug.Log("Animation finished: " + currentAnimStateName);
                animator.SetBool("animCaresse", false);
                animator.SetBool("animFrot", false);
                animator.SetBool("animTap", false);
                animator.SetBool("animHit", false);
                animator.SetBool("animFree", false);
                animationPlayed = false;
            }
        }
        else
        {
            // Si l'état a changé, on désactive en douceur
            //rigToBlend.weight = Mathf.Lerp(rigToBlend.weight, 0f, Time.deltaTime * 5f);
        }
    }
}

