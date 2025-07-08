using UnityEngine;

public class HeadDirectionAndGaze_CameraBased : MonoBehaviour
{
    public Camera vrCamera;                   // Assigne ici ta Main Camera (XR Rig)
    public float yawThreshold = 10f;          // Sensibilité gauche/droite
    public float gazeDistance = 10f;          // Distance max du regard
    public Transform targetParent; // à assigner dans l'inspecteur

    void Update()
    {
        if (vrCamera == null)
        {
            Debug.LogWarning("Caméra non assignée !");
            return;
        }

        // --- 1. Détection gauche / droite ---
        float yaw = NormalizeAngle(vrCamera.transform.eulerAngles.y);
        // Debug.Log("Angle de la tête (Yaw) : " + yaw);

        // if (yaw > yawThreshold)
        // {
        //     Debug.Log("Tête tournée à DROITE");
        // }
        // else if (yaw < -yawThreshold)
        // {
        //     Debug.Log("Tête tournée à GAUCHE");
        // }
        // else
        // {
        //     Debug.Log("Tête CENTRÉE");
        // }

        // --- 2. Raycast droit devant ---
        Ray gazeRay = new Ray(vrCamera.transform.position, vrCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(gazeRay, out hit, gazeDistance))
        {
            // Vérifie si le collider touché est enfant de targetParent
            if (hit.collider.transform.IsChildOf(targetParent))
            {
                Debug.DrawRay(gazeRay.origin, gazeRay.direction * hit.distance, Color.green);
                Debug.Log("Regarde enfant de " + targetParent.name + " : " + hit.collider.gameObject.name);
            }
        }
        else
        {
            Debug.DrawRay(gazeRay.origin, gazeRay.direction * gazeDistance, Color.red);
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
