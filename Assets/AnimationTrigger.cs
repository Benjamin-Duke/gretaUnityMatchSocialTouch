using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class AnimationTrigger : MonoBehaviour
{
    public Animator animator;
    public Rig rigToBlend;
    public AnimationCurve rigBlendCurve; // Assignée dans l'inspector

    private bool animationPlayed = false;
    // private int touchID = 1;

    void Start()
    {
        Debug.Log(rigBlendCurve == null);
        animator.SetBool("testAnim", false);
        animator.SetBool("animHit", false);
        if (rigToBlend != null)
            rigToBlend.weight = 0f;

        // Si aucune courbe n'est assignée, on en crée une par défaut
        if (rigBlendCurve == null || rigBlendCurve.length == 0)
        {
            rigBlendCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 1f),
                new Keyframe(1f, 0f)
            );
        }
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animator.SetBool("testAnim", true);
            animationPlayed = true;
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            animator.SetBool("animHit", true);
            animationPlayed = true;
        }

        if (animationPlayed)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(2);
            // Debug.Log(" state Caresse: " + stateInfo.IsName("caresse"));
            // Debug.Log("Current state hit: " + stateInfo.IsName("hit"));
            if (stateInfo.IsName("caresse"))
            {
                float t = Mathf.Clamp01(stateInfo.normalizedTime);

                if (rigToBlend != null && rigBlendCurve != null)
                {
                    float targetWeight = rigBlendCurve.Evaluate(t);
                    rigToBlend.weight = Mathf.Lerp(rigToBlend.weight, targetWeight, Time.deltaTime * 10f);
                }

                if (stateInfo.normalizedTime >= 1f)
                {
                    Debug.Log("Animation is finished");
                    animator.SetBool("testAnim", false);
                    animationPlayed = false;

                    if (rigToBlend != null)
                        rigToBlend.weight = 0f;
                }
            }

            if (stateInfo.IsName("hit"))
            {
                float t = Mathf.Clamp01(stateInfo.normalizedTime);
                // Debug.Log("Animation Time: " + t);

                if (rigToBlend != null && rigBlendCurve != null)
                {
                    float targetWeight = rigBlendCurve.Evaluate(t);
                    rigToBlend.weight = Mathf.Lerp(rigToBlend.weight, targetWeight, Time.deltaTime * 10f);
                }

                if (stateInfo.normalizedTime >= 1f)
                {
                    Debug.Log("Animation is finished");
                    animator.SetBool("animHit", false);
                    animationPlayed = false;
                }
            }
        }
    }
}
