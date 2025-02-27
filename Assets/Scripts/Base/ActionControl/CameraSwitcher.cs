using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class CameraShortcut
    {
        public Camera camera;
        //public AudioListener al;
        public KeyCode key;
    }

    [SerializeField]
    public List<CameraShortcut> cameraShortcuts = new List<CameraShortcut>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (CameraShortcut cs in cameraShortcuts)
        {
            if (Input.GetKey(cs.key))
             {
                foreach (CameraShortcut cs2 in cameraShortcuts)
                {
                    cs2.camera.enabled = cs2.camera == cs.camera;
                    //cs2.al.enabled = cs2.al == cs.al;
                }
                break;
            }
        }
    }
}
