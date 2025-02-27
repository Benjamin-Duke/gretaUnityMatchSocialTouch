// OSC Jack - Open Sound Control plugin for Unity
// https://github.com/keijiro/OscJack

using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace OscJack
{
    [AddComponentMenu("OSCNE")]
    public sealed class OSCNE : MonoBehaviour
    {
        #region Editable fields

        [SerializeField] string _ipAddress = "127.0.0.1";
        [SerializeField] int _udpPort = 9100;
        [SerializeField] string _oscAddress = "/yeux_unity";
        [SerializeField] string _dataSource = null;
        [SerializeField] string _propertyName = "";
        [SerializeField] bool _keepSending = false;

        #endregion


        #region Internal members
        /*public int[] array_regard = new int[7] { 0, 0, 0, 0, 0, 0,0 };
        public string[] label_2 = new string[7] { "CagetteC1", "CagetteC2", "CagetteC3", "CagetteC4", "Poitrine" , "Tete", "Liste" };
        public int indice = 0;*/
        /* int maxValue;
         int maxIndex;*/

        public int[] array_cagettes = new int[4] { 0, 0, 0, 0 };
        public string[] label_cagettes = new string[4] { "CagetteC1", "CagetteC2", "CagetteC3", "CagetteC4" };
        public int indice = 0;
        int maxValue;
        int maxIndex;

        OscClient _client;
        PropertyInfo _propertyInfo;
        string data = "";
        List<string> lista = new List<string>();
        void UpdateSettings()
        {
            _client = OscMaster.GetSharedClient(_ipAddress, _udpPort);

            if (_dataSource != null && !string.IsNullOrEmpty(_propertyName))
                _propertyInfo = _dataSource.GetType().GetProperty(_propertyName);
            else
                _propertyInfo = null;
            _dataSource = "string";
        }

        #endregion

        #region MonoBehaviour implementation

        void Start()
        {
            UpdateSettings();
        }

        void OnValidate()
        {
            if (Application.isPlaying) UpdateSettings();
        }

        void Update()
        {

            //Debug.Log("Update");
            if (_dataSource == "string")
            {
                //Debug.Log("[INFO]:" + "haha");
                Send((string)"");
            }
        }

        #endregion

        #region Sender methods

        int _intValue = Int32.MaxValue;

        public void Send(int data)
        {
            if (!_keepSending && data == _intValue) return;
            _client.Send(_oscAddress, data);
            _intValue = data;
        }

        float _floatValue = Single.MaxValue;

        public void Send(float data)
        {
            if (!_keepSending && data == _floatValue) return;
            _client.Send(_oscAddress, data);
            _floatValue = data;
        }

        Vector2 _vector2Value = new Vector2(Single.MaxValue, 0);

        public void Send(Vector2 data)
        {
            if (!_keepSending && data == _vector2Value) return;
            _client.Send(_oscAddress, data.x, data.y);
            _vector2Value = data;
        }

        Vector3 _vector3Value = new Vector3(Single.MaxValue, 0, 0);

        public void Send(Vector3 data)
        {
            if (!_keepSending && data == _vector3Value) return;
            _client.Send(_oscAddress, data.x, data.y, data.z);
            _vector3Value = data;
        }

        Vector4 _vector4Value = new Vector4(Single.MaxValue, 0, 0, 0);

        public void Send(Vector4 data)
        {
            if (!_keepSending && data == _vector4Value) return;
            _client.Send(_oscAddress, data.x, data.y, data.z, data.w);
            _vector4Value = data;
        }

        Vector2Int _vector2IntValue = new Vector2Int(Int32.MaxValue, 0);

        public void Send(Vector2Int data)
        {
            if (!_keepSending && data == _vector2IntValue) return;
            _client.Send(_oscAddress, data.x, data.y);
            _vector2IntValue = data;
        }

        Vector3Int _vector3IntValue = new Vector3Int(Int32.MaxValue, 0, 0);

        public void Send(Vector3Int data)
        {
            if (!_keepSending && data == _vector3IntValue) return;
            _client.Send(_oscAddress, data.x, data.y, data.z);
            _vector3IntValue = data;
        }

        string _stringValue = string.Empty;

        public void Send(string data)
        {


            //_client.Send(_oscAddress,"OSCNE 3 MARCHE <=================================");
            /*
            if (!_keepSending && data == _stringValue) return;
            data = "";
            _stringValue = "";
            _stringValue = RecursiveBones(GameObject.Find("CharacterIngrid").gameObject);
            GameObject g=GameObject.Find("CharacterIngrid").gameObject;
            Component[] components = g.gameObject.GetComponents(typeof(Component));
            foreach (Component component in components)
            {
                Debug.Log(component.ToString());
            }
            
            GameObject g = GameObject.Find("CharacterIngrid").gameObject;
            string text = "";
            IBM.Watsson.Examples.ExampleStreaming ex = g.GetComponent<IBM.Watsson.Examples.ExampleStreaming>();
            text = ex.results;
            _stringValue = text + ",";
            //_client.Send(_oscAddress, _stringValue);

            //Debug.Log("Message sent");

            Debug.Log("dialogue = " +text  + "     " + _oscAddress);
            _client.Send(_oscAddress, text);

            */
            GameObject g = GameObject.Find("[GazeTrail]").gameObject;
            GameObject head = GameObject.Find("Head").gameObject;

            var gaze = "placeholder";

            if (gaze != "false") // ================================================================================================> FAIRE QUE CA S ACTIVE SEULEMENT LORS DES CHOIX POUR PLUS DE PERFORMANCE
            {
                if (gaze == "CagetteC1")
                    array_cagettes[0] += 1;
                if (gaze == "CagetteC2")
                    array_cagettes[1] += 1;
                if (gaze == "CagetteC3")
                    array_cagettes[2] += 1;
                if (gaze == "CagetteC4")
                    array_cagettes[3] += 1;
                indice += 1;

                if (indice == 3)
                {
                    maxValue = array_cagettes.Max();
                    maxIndex = array_cagettes.ToList().IndexOf(maxValue);
                    array_cagettes = new int[4] { 0, 0, 0, 0 };
                    indice = 0;

                    print(label_cagettes[maxIndex]);
                    if  (maxValue != 0)
                        _client.Send(_oscAddress, label_cagettes[maxIndex]);
                    else
                        _client.Send(_oscAddress, "decor");
                }
            }


            /*if (gaze.LatestHitObject != false & autorisation.peut_envoyer)
            {
                if (gaze.LatestHitObject.name == "CagetteC1")
                    array_regard[0] += 1;
                if (gaze.LatestHitObject.name == "CagetteC2")
                    array_regard[1] += 1;
                if (gaze.LatestHitObject.name == "CagetteC3")
                    array_regard[2] += 1;
                if (gaze.LatestHitObject.name == "CagetteC4")
                    array_regard[3] += 1;
                if (gaze.LatestHitObject.name == "Poitrine")
                    array_regard[4] += 1;
                if (gaze.LatestHitObject.name == "Tete")
                    array_regard[5] += 1;
              *//*  if (gaze.LatestHitObject.name == "mur_droite")
                    array_regard[6] += 1;
                if (gaze.LatestHitObject.name == "mur_gauche")
                    array_regard[7] += 1;*//*
                if (gaze.LatestHitObject.name == "Liste")
                    array_regard[6] += 1;


                indice += 1;

                if (indice == 3)
                {
                    // trouver la plus grosse occurence et determiner à quelle target elle correspond et envoyer cette derniere
                    maxValue = array_regard.Max();
                    maxIndex = array_regard.ToList().IndexOf(maxValue);
                    array_regard = new int[7] { 0, 0, 0, 0, 0, 0, 0 };
                    indice = 0;

                    print(label_2[maxIndex]);

                    _client.Send(_oscAddress, label_2[maxIndex]);
                    if (label_2[maxIndex] == "Tete"){
                        StartCoroutine(autorisation.simulation_eyes());
                    }
                    _client.Send(_oscAddress, label_2[maxIndex]);

                }*/
            //_client.Send(_oscAddress, "null");
            //position = gaze.LatestHitObject.nom;
            //coordonnees = gaze.LatestHitObject.coordonnee;
            //Debug.Log("==========================================>" + gaze.LatestHitObject.name + gaze.LatestHitObject.position.ToString());
            //_stringValue = gaze.LatestHitObject.name +"/"+ gaze.LatestHitObject.position.ToString(); //////// IMPORTANT

            /*if (gaze.GetNameObject() != "null") { 
                string _string_value = gaze.GetNameObject();
                Debug.Log("===========================================>"+_stringValue);
                _client.Send(_oscAddress, _stringValue);
        }
        }*/

        }

        #endregion
    
        public String RecursiveBones(GameObject parent)
        {
            int i = 0;
            //Debug.Log("Bool:"+parent.name+"  " + lista.Contains(parent.name));
            if ((parent.name.Contains("Character") || parent.name.Contains("#")) && !lista.Contains(parent.name))
            {
                //Debug.Log("Object Added "+ parent.name);
                data = data + "," + parent.name;
                lista.Add(parent.name);
            }
            //Debug.Log("[INFO NUMBER]:" + parent.transform.childCount + "   " + parent.name);
            while (parent.transform.childCount != 0)
            {
                //Debug.Log("[INFO]:" + parent.transform.GetChild(i).gameObject.name);
                RecursiveBones(parent.transform.GetChild(i).gameObject);
                i++;
                if (parent.name == "Head")
                {
                    //Debug.Log("HEAD Found" + parent);
                }
                if (i == parent.transform.childCount)
                {

                    return data;
                }

            }

            return data;
        }
    }


}

