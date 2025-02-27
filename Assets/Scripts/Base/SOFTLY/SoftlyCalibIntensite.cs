using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SoftlyCalibIntensite : MonoBehaviour
{
    public bool debugMode;
    private YAHSController controller;
    public float intensiteDepart;
    private float _intensiteActuelle;

    public float step;
    private int _nbInversion;
    private string _lastOperation;

    public int tailleSequence;
    private string _seqActivationRaw;
    private string[] _seqActivation;
    private int _indexNextActivation;
    private string[] _seqReponses;

    private int _frequence = 0;

    private int _nbBonneRepConsecutives = 0;
    private string _filePath;
    // Start is called before the first frame update
    void Start()
    {
        _filePath = getPath();
        StreamWriter writer = new StreamWriter (_filePath);
        writer.WriteLine ("Activation,Reponse,Intensite,NbInversion,Frequence,Pas");
        writer.Flush();
        writer.Close();
        CreerSequence();
        _indexNextActivation = 0;
        controller = GetComponent<YAHSController>();
        StartCoroutine("logic");
    }

    
    // Update is called once per frame
    void Update()
    {
        
    }

    private void CreerSequence()
    {
        _seqActivation = new string[tailleSequence];
        _seqReponses = new string[tailleSequence];
        //Creer la sequence d'activation à partir d'un random bool sequence
        if (Random.value < 0.5) _seqActivationRaw = "gauche";
        else _seqActivationRaw = "droite";
        for (int i = 1; i < tailleSequence; i++)
        {
            if (Random.value < 0.5)
            {
                _seqActivationRaw = _seqActivationRaw + ",gauche";
            }
            else
            {
                _seqActivationRaw = _seqActivationRaw + ",droite";
            }

            _seqReponses[i] = "0";
        }
        Debug.Log("Sequence d'activation generee : " + _seqActivationRaw);
        _seqActivation = _seqActivationRaw.Split(',');
    }
    IEnumerator logic()
    {
        int count = 0;
        while (count < 3)
        {
            PreparerNextActivation();
            Debug.Log("NextActivation preparee");
            yield return WaitForCommand();
            Debug.Log("WaitForCommandFait");
            JouerNextActivation();
            Debug.Log("JouerNextActivationFait");
            yield return WaitForReponse();
            Debug.Log("WaitforReponseFait");
            WriteLine();
            CheckRep();
            IncNextAct();
            count++;
        }
    }

    private void IncNextAct()
    {
        _indexNextActivation++;
        if (_indexNextActivation >= tailleSequence)
        {
            //END
        }
    }
    IEnumerator WaitForReponse()
    {
        bool pressed = false;
        while (!pressed)
        {
            if (Input.GetKey(KeyCode.D))
            {
                pressed = true;
                Reponse("droite");
                Debug.Log("ReponseDroiteRegistered");
                break;
            }
            else if (Input.GetKey(KeyCode.G))
            {
                pressed = true;
                Debug.Log("ReponseGaucheRegistered");
                Reponse("gauche");
                break;
            }
            yield return null;
        }
    }

    IEnumerator WaitForCommand()
    {
        Debug.Log("EnteringWaitForCommand");
        bool pressed = false;
        while (!pressed)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                Debug.Log("Space key registered");
                pressed = true;
            }
            yield return null;
        }
    }
    public void Reponse(string reponseEntree)
    {
        _seqReponses[_indexNextActivation] = reponseEntree;
    }

    private bool CheckRep()
    {
        bool rep = false;
        if(_seqActivation[_indexNextActivation].Equals(_seqReponses[_indexNextActivation]))
        {
            if (_nbBonneRepConsecutives == 1)
            {
                _nbBonneRepConsecutives = 0;
                AugmenterIntensite();
            }
            else if (_nbBonneRepConsecutives == 0)
                _nbBonneRepConsecutives = 1;

            rep = true;
        }
        else
        {
            BaisserIntensite();
        }
        return rep;
    }
    public void AugmenterIntensite()
    {
        _intensiteActuelle += step;
        if (_lastOperation == "baisser")
        {
            AugmenterInversion();
        }
        _lastOperation = "augmenter";
    }

    public void BaisserIntensite()
    {
        _intensiteActuelle -= step;
        if (_lastOperation == "augmenter")
        {
            AugmenterInversion();
        }
        _lastOperation = "baisser";
    }

    public void AugmenterInversion()
    {
        _nbInversion += 1;
        CheckStepChange();
    }
    
    private void CheckStepChange()
    {
        if (_nbInversion == 3)
        {
            step = 0.25f;
        }
        else if (_nbInversion == 15)
        {
            //couper les elements d'index superieur a indexactivation de seqactivation et seqreponses
        }
    }

    void PreparerNextActivation()
    {
        YAHSStatic touch = new YAHSStatic();
        List<int> actuatorOne = new List<int>();
        List<int> actuatorTwo = new List<int>();
        if (_seqActivation[_indexNextActivation].Equals("droite"))
        {
            actuatorOne.Add(0);
            actuatorOne.Add(0);
            actuatorTwo.Add(0);
            actuatorTwo.Add(1);
            touch.Name = "tapElbowSide" + _indexNextActivation;
        }
        else
        {
            actuatorOne.Add(2);
            actuatorOne.Add(0);
            actuatorTwo.Add(2);
            actuatorTwo.Add(1);
            touch.Name = "tapWristSide" + _indexNextActivation;
        }
        touch.Duration = 500;
        touch.RampUp = 0;
        touch.RampDown = 0;
        touch.MinimumModulation = 0;
        touch.ModulationType = "linear";
        touch.Intensity = _intensiteActuelle;
        touch.TouchType = "buzz";
        touch.Actuators = new List<List<int>>();
        touch.Actuators.Insert(0, actuatorOne);
        touch.Actuators.Insert(1, actuatorTwo);

        if (!debugMode)
        {
            controller.SendFromParams(controller.CACHE_ADDRESS, new List<bool> {true});
            touch.Send(controller);
            controller.SendFromParams(controller.CACHE_ADDRESS, new List<bool> {false});
            Debug.Log("Caching started for one signal");
        }

        if (debugMode)
        {
            string s = "";
            var args = new List<object>
                {touch.Duration, touch.RampUp, touch.RampDown, touch.MinimumModulation, touch.ModulationType, touch.Intensity, touch.TouchType};
            foreach (var v in touch.Actuators.SelectMany(x => x)) args.Add(v);
            foreach (object o in args)
            {
                s += o.ToString();
            }
            Debug.Log("Complete touch is : " + s);
        }
    }

    public void JouerNextActivation()
    {
        if (debugMode)
        {
            if (_seqActivation[_indexNextActivation].Equals("droite"))
            {
                Debug.Log("TapElbowSide" + _indexNextActivation);
            }
            else if (_seqActivation[_indexNextActivation].Equals("gauche"))
                controller.Play("tapWristSide" + _indexNextActivation);
        }

        if (!debugMode)
        {
            if (_seqActivation[_indexNextActivation].Equals("droite"))
            {
                controller.Play("tapElbowSide" + _indexNextActivation);
            }
            else if (_seqActivation[_indexNextActivation].Equals("gauche"))
            {
                controller.Play("tapWristSide" + _indexNextActivation);
            }
        }
    }

    
    private string getPath()
    {
        #if UNITY_EDITOR
                return Application.streamingAssetsPath + "/csv/" + "test.csv";
        #elif UNITY_ANDROID
                return Application.persistentDataPath+"Saved_Inventory.csv";
        #elif UNITY_IPHONE
                return Application.persistentDataPath+"/"+"Saved_Inventory.csv";
        #else
                return Application.dataPath +"/"+"Saved_Inventory.csv";
        #endif
    }

    void WriteLine()
    {
        _filePath = getPath();
        StreamWriter writer = new StreamWriter(_filePath, true);

        //This adds a line to the target csv file
        writer.WriteLine(_seqActivation[_indexNextActivation] + ","
                          + _seqReponses[_indexNextActivation] + "," 
                          + _intensiteActuelle.ToString("0.00") + ","
                          + _nbInversion +","
                          + _frequence + ","
                          + step);
        writer.Flush();
        writer.Close();
    }
}
