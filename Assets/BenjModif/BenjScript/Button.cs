using UnityEngine;
using UnityEngine.Events;

public class ButtonVR : MonoBehaviour
{
    public GameObject buttonObject; // utilisé pour l'effet d'échelle
    public AudioSource clickSound;
    public UnityEvent onButtonPressed;

    public string buttonID; // "Option1", "Option2", "Next", "End", etc.
    public Color defaultColor = Color.white;
    public Color lockedColor = Color.red;

    private Renderer rend;

    private ButtonManager buttonManager;
    private SimpleTetraminoSnap snapManager;
    private TimerManagerB timeManager;

    void Start()
    {
        rend = GetComponent<Renderer>();
        SetColor(defaultColor);
        snapManager = FindObjectOfType<SimpleTetraminoSnap>();
        buttonManager = FindObjectOfType<ButtonManager>();
        timeManager = FindObjectOfType<TimerManagerB>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Index"))
        {
            Debug.Log($"[ButtonVR] Appui détecté sur le bouton {buttonID} par {other.name}");
            // Vérifier si le bouton peut être appuyé
            if (buttonManager != null && ButtonManager.CanPress(this))
            {
                PressButton();
                ButtonManager.RegisterPress(this);
            }
            else if (snapManager != null)
            {
                if (buttonID == "Undo")
                {
                    PressButton();
                    snapManager.UndoLastAction();
                }
                else if (buttonID == "Reset")
                {
                    PressButton();
                    snapManager.ResetAllTetraminos();
                }
                else if (buttonID == "Start")
                {
                    PressButton();
                    if (timeManager.gameStarted == false)
                        timeManager.StartStopTimer();
                }

            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        buttonObject.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    void PressButton()
    {
        buttonObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        //clickSound.Play();

        // Si c'est "End", vérifier si un choix a été fait avant de permettre l'appui
        if (buttonID == "End")
        {
            if (!FindObjectOfType<UIManager>().HasMadeChoiceOnCurrentPanel())
            {
                Debug.LogWarning("[ButtonVR] Vous devez faire un choix avant d'appuyer sur 'End'.");
                return; // Ne rien faire si le choix n'a pas été fait
            }
        }

        // Change la couleur uniquement si ce n'est pas un bouton "Next" ou "End"
        if (buttonID != "Next" && buttonID != "End" && buttonID != "Undo" && buttonID != "Reset" && buttonID != "Start") 
        {
            SetColor(lockedColor);
        }

        onButtonPressed?.Invoke();
    }

    public void SetColor(Color color)
    {
        if (rend != null)
        {
            rend.material.color = color;
        }
    }

    public void ResetVisual()
    {
        SetColor(defaultColor);
        buttonObject.transform.localScale = new Vector3(1f, 1f, 1f);
    }
}
