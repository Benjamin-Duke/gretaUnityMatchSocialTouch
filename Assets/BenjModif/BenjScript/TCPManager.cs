// TCPManager.cs - Gère la connexion TCP et l'envoi des données
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections.Generic; // pour Queue<>
using System.Threading;           // pour Thread
using FMODUnity;


public class TCPManager : MonoBehaviour
{
    // Singleton pour accéder facilement au manager
    public static TCPManager Instance { get; private set; }
    
    // Configuration du serveur
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    
    // Référence au client TCP
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;

    private Queue<string> messageQueue = new Queue<string>();
    private Thread sendingThread;
    private bool keepRunning = true;

    void Awake()
    {
        // Implémentation du singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Établir la connexion au démarrage
        ConnectToServer();
        sendingThread = new Thread(ProcessQueue);
        sendingThread.Start();
    }

    public void EnqueueData(string data)
    {
        lock (messageQueue)
        {
            messageQueue.Enqueue(data);
        }
    }

    private void ProcessQueue()
    {
        while (keepRunning)
        {
            string dataToSend = null;

            lock (messageQueue)
            {
                if (messageQueue.Count > 0)
                {
                    dataToSend = messageQueue.Dequeue();
                }
            }

            if (dataToSend != null && isConnected)
            {
                try
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(dataToSend);
                    stream.Write(buffer, 0, buffer.Length);
                    Debug.Log("Données envoyées: " + dataToSend);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Erreur lors de l'envoi des données: " + e.Message);
                    isConnected = false;
                }
            }

            Thread.Sleep(5); // petite pause pour éviter que le thread tourne à vide
        }
    }


    
    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connexion au serveur Python établie");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Impossible de se connecter au serveur Python: " + e.Message);
            isConnected = false;
        }
    }
    
    // Méthode publique pour envoyer des données
    public void SendData(string data)
    {
        if (!isConnected)
        {
            Debug.LogWarning("Tentative d'envoi sans connexion. Tentative de reconnexion...");
            ConnectToServer();
            if (!isConnected) return;
        }
        
        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);
            Debug.Log("Données envoyées: " + data);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur lors de l'envoi des données: " + e.Message);
            isConnected = false;
        }
    }
    
    // Fermer proprement la connexion
    void OnApplicationQuit()
    {
        keepRunning = false;
        if (sendingThread != null && sendingThread.IsAlive)
            sendingThread.Join(); // attendre que le thread finisse

        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }

}