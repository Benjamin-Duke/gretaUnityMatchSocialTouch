using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DemoController : MonoBehaviour
{
    [System.Serializable]
    public class FMLShortcut
    {
        public string FML;
        //public AudioListener al;
        public KeyCode key;
    }

    [SerializeField]
    public List<FMLShortcut> fmlShortcuts = new List<FMLShortcut>();

    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject Agent;

    //public bool debug = false;

    public bool lookSet = false;
    private bool moveSet = false;

    private GRETAnimationManagerDEMO AgentAnim;
    private AudioSource AgentAS;

    // Start is called before the first frame update
    void Start()
    {
        AgentAnim = Agent.GetComponent<GRETAnimationManagerDEMO>();
        if (AgentAnim == null)
        {
            Debug.LogError("No GRETAnimationManager found : can't play FML files.");
        }
        AgentAS = Agent.GetComponentInChildren<AudioSource>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Input.anyKeyDown) return;
        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            if (lookSet)
            {
                AgentAnim.ResetHeadLookTarget();
                lookSet = false;
            }
            else
            {
                AgentAnim.SetHeadLookTarget();
                lookSet = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            if (moveSet)
            {
                AgentAnim.ResetMoveTowardsTarget();
                moveSet = false;
            }
            else
            {
                AgentAnim.SetMoveTowardsTarget();
                moveSet = true;
            }
        }
        else
        {
            foreach (var fs in fmlShortcuts.Where(fs => Input.GetKey(fs.key)))
            {
                AgentAnim.PlayFML(fs.FML);
                break;
            }
        }



    }
}
