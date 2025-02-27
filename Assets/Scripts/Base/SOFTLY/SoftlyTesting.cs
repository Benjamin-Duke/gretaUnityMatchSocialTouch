using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoftlyTesting : MonoBehaviour
{
    // Communication with SOFTLY/YAHS
    public GameObject yahs;

    // File containing configuration
    public string configFile;

    // AnimationManager
    private GRETAnimationManager _gretaAnim;

    // Used to send commands to YAHS
    private YAHSController _yahsController;
    // Start is called before the first frame update
    void Start()
    {
        // Load components
        _gretaAnim = GetComponent<GRETAnimationManager>();

        _yahsController = yahs.GetComponent<YAHSController>();
        
        //Here add signals to the database of yahsController if necessary

        // Cache signals
        _yahsController.CacheSignals();
    }

    public void TestSignal(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("BasicSin");
    }
    public void TestSignal2(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("TouchSoft2");
    }
    public void TestSignal3(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("TouchSoft3");
    }
    public void TestSignal4(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("TouchSoft4");
    }
    public void TestSignal5(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("TouchSoft5");
    }
    
    public void TestSignal6(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("Trying to send to Softly");
        _yahsController.Play("TouchSoft6");
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
