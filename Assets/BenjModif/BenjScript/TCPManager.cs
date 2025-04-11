// TCPManager.cs - Gère la connexion TCP et l'envoi des données
using System.Net.Sockets;
using System.Text;
using UnityEngine;

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
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        Debug.Log("Connexion TCP fermée");
    }
}