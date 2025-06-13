using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class AnimationTrigger : MonoBehaviour
{
    public Animator animator;
    public Rig rigToBlend;

    private string currentAnimStateName = "";
    private bool animationPlayed = false;
    

    void Start()
    {
        ResetAllBools();

        if (rigToBlend != null)
            rigToBlend.weight = 0f;
    }

    void Update()
    {
        // Déclenchement manuel via clavier
        if (!animationPlayed)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame) TriggerManual("caresse");
            if (Keyboard.current.vKey.wasPressedThisFrame) TriggerManual("frot");
            if (Keyboard.current.bKey.wasPressedThisFrame) TriggerManual("tap");
            if (Keyboard.current.nKey.wasPressedThisFrame) TriggerManual("hit");
            if (Keyboard.current.spaceKey.wasPressedThisFrame) TriggerManual("libre");
        }

        if (animationPlayed)
        {
            UpdateRigWeight();
        }
    }

    void TriggerManual(string animName)
    {
        Debug.Log($"Animation manuelle déclenchée : {animName}");
        PlayAnimation(animName);
    }

    // Appelé automatiquement par d'autres scripts
    public void PlayAnimation(string animName)
    {
        ResetAllBools();
        var gretaAnimator = FindObjectOfType<GretaCharacterAnimator>();
        if (gretaAnimator != null)
            gretaAnimator.useBapAnimation = false;
        switch (animName.ToLower())
        {
            case "caresse": animator.SetBool("animCaresse", true); break;
            case "frot": animator.SetBool("animFrot", true); break;
            case "tap": animator.SetBool("animTap", true); break;
            case "hit": animator.SetBool("animHit", true); break;
            case "libre": animator.SetBool("animFree", true); break;
        }

        currentAnimStateName = animName;
        animationPlayed = true;
    }

    void UpdateRigWeight()
    {
        int layerIndex = 2;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);

        if (stateInfo.IsName(currentAnimStateName))
        {
            //Debug.Log("Temps d'animation : " + stateInfo.normalizedTime);
            if (stateInfo.normalizedTime >= 0.95f)
            {
                Debug.Log("Animation terminée : " + currentAnimStateName);
                ResetAllBools();
                animationPlayed = false;
                var gretaAnimator = FindObjectOfType<GretaCharacterAnimator>();
                if (gretaAnimator != null)
                    gretaAnimator.useBapAnimation = true;

            }
        }
    }

    void ResetAllBools()
    {
        animator.SetBool("animCaresse", false);
        animator.SetBool("animFrot", false);
        animator.SetBool("animTap", false);
        animator.SetBool("animHit", false);
        animator.SetBool("animFree", false);
    }

    public bool IsIdle()
    {
        return !animationPlayed;
    }
}
