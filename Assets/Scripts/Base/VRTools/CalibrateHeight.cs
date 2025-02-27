using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class CalibrateHeight : MonoBehaviour
{
    public InputAction calibrateAction;
    public float targetHeight = 1.9f;
    
    
    // Start is called before the first frame update
    void Start()
    {
        calibrateAction.performed += ctx => AdjustHeight();
        calibrateAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void AdjustHeight()
    {
        if (Camera.main == null) return;
        var initPos = Camera.main.transform.position;
        var offset = targetHeight - initPos.y;
        if (Mathf.Abs(offset) >= 0.05)
        {
            var xr = GetComponent<XROrigin>();
            if (xr == null) return;
            xr.CameraYOffset += offset;
        }
    }
}
