// CubeCollisionDetector.cs - Gère la détection des collisions
using UnityEngine;

public class CubeCollisionDetector : MonoBehaviour
{
    // ID du cube
    public int cubeID;
    
    // Méthode appelée lors d'une collision
    void OnTriggerEnter(Collider collision)
    {
    // Vérifier si la collision implique la balle
        Debug.Log("Collision avec la balle détectée sur le cube " + cubeID);
        
        // Utiliser le TCPManager pour envoyer l'ID du cube
        if (TCPManager.Instance != null)
        {
            TCPManager.Instance.SendData(cubeID.ToString());
        }
        else
        {
            Debug.LogError("TCPManager non trouvé dans la scène");
        }
    }
}