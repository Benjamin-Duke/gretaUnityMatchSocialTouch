using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.CoreModule;
using TactilePerception;

public class TouchInformationBridge : MonoBehaviour
{
	[Tooltip("GameObject with a HandTouchManager script, to subscribe to touch events from human to Camille")]
	public GameObject handTouchManager;

	public string joyFile;
	public string angerFile;
	public string hitFile;
	public string caressFile;

	[Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
	public GameObject Agent;

	private GRETAnimationManagerDEMO AgentAnim;

	[Tooltip(
		"GameObject with a DistanceInterpretation script, to subscribe to changes of distance between Camille and human")]
	public GameObject distanceInterpretation;

	[Tooltip("GameObject with a LookCamille script, to subscribe to changes of look from human to Camille")]
	public GameObject lookCamille;

	private DistanceInterpretation _distanceInterpretation;
	private HandTouchManager _handTouchManager;
	private LookCamille _lookCamille;
    // Start is called before the first frame update
    void Start()
    {
		_handTouchManager = handTouchManager.GetComponent<HandTouchManager>();
		if (_handTouchManager == null)
		{
			Debug.LogWarning("No HandTouchManager script found : won't send touch events to FAtiMA");
		}
		else
		{
			// Subscribe to all events so we can send FAtiMA events in realtime
			_handTouchManager.TouchStarted += OnTouchStarted;
			_handTouchManager.TouchChanged += OnTouchChanged;
			_handTouchManager.TouchEnded += OnTouchEnded;
		}

		AgentAnim = Agent.GetComponent<GRETAnimationManagerDEMO>();
		if (AgentAnim == null)
		{
			Debug.LogError("No GRETAnimationManager found : can't play FML files.");
		}

		_distanceInterpretation = distanceInterpretation.GetComponent<DistanceInterpretation>();
		if (_distanceInterpretation == null)
			Debug.LogWarning("No DistanceInterpretation script found : won't send proximity events to FAtiMA");
//		else
//			_distanceInterpretation.DistanceInterpretationChanged += OnDistanceChanged;
		_lookCamille = lookCamille.GetComponent<LookCamille>();
		if (_lookCamille == null) Debug.LogWarning("No LookCamille script found : won't send look events to FAtiMA");
//		else
//			_lookCamille.LookAtCamilleChanged += OnLookChanged;        
    }

	// Update is called once per frame |
    void Update()
    {
        
    }

	IEnumerator _setResetMovement(){
		AgentAnim.SetMoveTowardsTarget();
		yield return new WaitForSeconds(2.0f);
		AgentAnim.ResetMoveTowardsTarget();
	}

	#region Event subscribers
	private void OnTouchStarted(object sender, HandTouchManager.TouchEventArgs e)
	{
		if(e.Localization.ToString() == "Head" || e.ImpactVelocityInterpretation == "Strong" || e.Type.ToString() == "Hit"){
			StartCoroutine(_setResetMovement());
			AgentAnim.PlayFML(hitFile);
		}
		else {
			AgentAnim.PlayFML(joyFile);
		}
	}

	private void OnTouchChanged(object sender, HandTouchManager.TouchEventArgs e)
	{
		if(e.Localization.ToString() != "Head" && e.MeanVelocityInterpretation != "Fast" && e.Type.ToString() == "Caress"){
			StartCoroutine(_setResetMovement());
			AgentAnim.PlayFML(caressFile);
		}
	}

	private void OnTouchEnded(object sender, HandTouchManager.TouchEventArgs e)
	{
		
	}
	#endregion
}
