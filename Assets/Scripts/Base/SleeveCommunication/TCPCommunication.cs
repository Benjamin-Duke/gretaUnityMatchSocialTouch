using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class TCPCommunication : MonoBehaviour
{
    private TcpClient socketConnection; 	
	private Thread clientReceiveThread;

	public InputAction testVibration;
	
	public String Host = "192.168.42.1";
	public Int32 Port = 1234;

//Objects to keep track of for the synchronization of the stimulus with the hand gesture
	public Transform agentTouchHand;
	public Transform touchHandTarget;

	public float vibrationDistance = 0.14f;

	private bool _activated = false;

	public bool test;

    public String[] vibreurs = new string[24] { "0.0.17", "0.0.15", "0.1.29", "0.1.27", "0.1.17", "0.1.15",
        "0.0.0", "0.0.1", "0.1.36", "0.1.37", "0.1.0", "0.1.1",
        "0.0.13", "0.0.2", "0.1.25", "0.1.38", "0.1.13", "0.1.2",
        "0.0.14", "0.0.12", "0.1.24", "0.1.26", "0.1.14", "0.1.12"
    };

    public String hit = "hit_40_i_80.txt";

    public String tap = "tap_40_i_40.txt";

    public String stroke = "stroke_p_1300_i_10.txt";

    // Use this for initialization 	
    void Start() {
		if (test)
		{
			gameObject.SetActive(false);
		}
		if (ConnectToTcpServer())
		{
			Debug.Log("socket is set up");
		}     
		
		//Testing the connection
		testVibration.performed += ctx => SendSignal("play=q-100-bras.txt,1,0,2"); //play=patternfile,iterations(-1=infinite),??,executionmode(2=interrupt everything currently playing°
		testVibration.Enable();
	}  	
	// Update is called once per frame
	void Update()
	{
		if (test) return;
		
		if (!socketConnection.Connected || socketConnection == null)
		{
			ConnectToTcpServer();
		}

		if (!_activated && Vector3.Distance(touchHandTarget.position, agentTouchHand.position) <= vibrationDistance)
		{
				SendSignal("play=simple-complet.txt,-1,0,2");
				//SendSignal("play=q-100-bras.txt,-1,0,2");
				_activated = true;
				//Debug.Log("Activating sleeve stimulus.");
		}
		else if (_activated && Vector3.Distance(touchHandTarget.position, agentTouchHand.position) > (vibrationDistance + 0.01f))
		{
			//Cancel Signal
			SendSignal("stopAll");
			_activated = false;
			//Debug.Log("Stopping sleeve stimulus.");
		}
	}  	
	/// <summary> 	
	/// Setup socket connection. 	
	/// </summary> 	
	private bool ConnectToTcpServer () { 		
		try {  			
			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();
			return true;
		} 		
		catch (Exception e) { 			
			Debug.Log("On client connect exception " + e);
			return false;
		} 	
	}  	
	/// <summary> 	
	/// Runs in background clientReceiveThread; Listens for incoming data. 	
	/// </summary>     
	private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient(Host, Port);  			
			Byte[] bytes = new Byte[1024];             
			while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incoming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
						var incommingData = new byte[length]; 						
						Array.Copy(bytes, 0, incommingData, 0, length); 						
						// Convert byte array to string message. 						
						string serverMessage = Encoding.UTF8.GetString(incommingData); 						
						Debug.Log("server message received as: " + serverMessage); 					
					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	}
	
	/// <summary> 	
	/// Send message to server using socket connection. 	
	/// </summary> 	
	public void SendSignal(string message) {         
		if (socketConnection == null) {    
			Debug.Log("Attempted to send signal but socket connection failed");
			return;
		}  		
		try {
			Debug.Log("Attempting to send signal");
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) { 				
				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(message); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Client sent his message - should be received by server");             
			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	}

    public IEnumerator PlayPattern(string pattern)
    {
        switch (pattern)
        {
            case "hit":
                SendSignal("play=" + hit + ",1,0,2");
                yield return null;
                break;
            case "tap":
                float _short = (40f + 160f) / 1000;
                float _long = (40f + 460) / 1000;

                //Play first tap
                SendSignal("play=" + tap + ",1,0,2");
                //Play second tap after tap time + 150 ms
                yield return new WaitForSeconds(_short);
                SendSignal("play=" + tap + ",1,0,2");

                // Play last three tap with 150 ms
                yield return new WaitForSeconds(_long);
                SendSignal("play=" + tap + ",1,0,2");

                //4rth tap
                yield return new WaitForSeconds(_short);
                SendSignal("play=" + tap + ",1,0,2");

                //5th tap
                yield return new WaitForSeconds(_short);
                SendSignal("play=" + tap + ",1,0,2");
                break;
            case "stroke":
                SendSignal("play=" + stroke + ",1,0,2");
                yield return null;
                break;
			case "newtap":
				yield return new WaitForSeconds(0.2f);
                SendSignal("play=newtap.txt,1,0,2");
				yield return null;
				break;
        }
    }

    public void UpdateDevice()
    {
        SendSignal("update");
    }

    private void OnApplicationQuit()
    {
	    if (socketConnection != null && socketConnection.Connected)
	    {
		    socketConnection.GetStream().Close();
		    socketConnection.Close();
		    clientReceiveThread.Abort();
	    }
            
    }
}
