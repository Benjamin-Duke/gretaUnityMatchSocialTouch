using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity.Interaction;
using SerializationUtilities.Attributes;
using TMPro;
using UnityEngine;
using ExperimentUtility;

public class PanelManager : MonoBehaviour
{

    public PanelType panelType;

    public TextMeshProUGUI selectionLabel;

    public TextMeshProUGUI questionLabel;

    public TextMeshProUGUI progressionLabel;

    public MeshRenderer nextButton;
    
    private Material _mat;

    private List<InteractionSlider> _sliders;
    // Start is called before the first frame update
    void Start()
    {
        if (nextButton != null)
        {
            _mat = nextButton.material;
        }
        if (panelType != PanelType.End)
            _sliders = GetComponentsInChildren<InteractionSlider>(true).ToList();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateLabels(string question, string selection, string progression = "")
    {
        if (progression != "")
        {
            progressionLabel.text = progression;
            progressionLabel.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            progressionLabel.transform.parent.gameObject.SetActive(false);
        }
        questionLabel.text = question;
        if (panelType == PanelType.Ready ||
            panelType == PanelType.Slider || panelType == PanelType.End || panelType == PanelType.Age)
            return;
        selectionLabel.text = "Selection : " + selection;
    }
    
    public void UpdateSelectionLabel(string selection)
    {
        if (panelType == PanelType.Ready ||
            panelType == PanelType.Slider || panelType == PanelType.End)
            return;
        if (panelType == PanelType.Age)
        {
            selectionLabel.text = selection;
            return;
        }
        selectionLabel.text = "Selection : " + selection;
    }

    public void Enable()
    {
        if (_sliders != null && _sliders.Count > 0)
        {
            foreach (var slider in _sliders)
            {
                slider.HorizontalSliderValue = slider.defaultHorizontalValue;
            }
        }
        gameObject.SetActive(true);
    }

    public void DisableSlider()
    {
        if (panelType != PanelType.Ready) return;
        foreach (var slider in _sliders)
        {
            slider.transform.parent.gameObject.SetActive(false);
        }
    }
    
    public void EnableSlider()
    {
        if (panelType != PanelType.Ready) return;
        foreach (var slider in _sliders)
        {
            slider.transform.parent.gameObject.SetActive(true);
        }
    }
    
    public void ChangeColor(Color color)
    {
        if (_mat.color != color)
        {
            _mat.color = color;
        }
    }

    public void InitializeLanguage(string lang)
    {
        var labels = GetComponentsInChildren<TextMeshProUGUI>(true).ToList();
        foreach (var label in labels)
        {
            if (label.CompareTag(lang) || label.CompareTag("Untagged"))
            {
                label.gameObject.SetActive(true);
                continue;
            }
            label.gameObject.SetActive(false);
        }
    }
    
    
}
