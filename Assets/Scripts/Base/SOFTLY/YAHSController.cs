using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/**
 * This class helps to send messages to YAHS
 * by allowing to configure YAHS via a JSON file.
 * There is no JSON schema yet but it follows the
 * protocol fields : https://gitlab.utc.fr/translife/projets/socialtouch/yahs/-/blob/master/doc/usage.md#configuration
 * 
 * In the JSON file you can configure wait, static and stroke touches, then assemble them in sequence, by referring to the `name` JSON key. Exemple is provided in StreamingAssets → Stimuli → haptic_config.json
 * 
 * Cache CacheSignal() to preload signals in the "dabatase".
 * 
 * You can add a signal in the dabatase by modifying it. You shoud use the Send method of each signal to send it.
 * 
 * You can still use the TouchFromParams method to send a signal directly from a list of params. (warning : if cache does not exist for the signal, you will experiment latency). 
 * But as the YAHS protocol is garbage, I think it is easier to use JSONs or YAHSTouch objects and Send methods..
 */
public class YAHSController : MonoBehaviour
{
    // JSON file to load
    public string signalsFile;

    public readonly string CACHE_ADDRESS = "/params/cache";

    // OSC controller
    private OSC _osc;

    // All signals in cache 
    public List<YAHSTouch> Database { get; private set; }

    // Make sure that Database
    // is ready before other objects start to use it
    private void Awake()
    {
        _osc = GetComponent<OSC>();
        Database = new List<YAHSTouch>();
        var json = File.ReadAllText(Application.streamingAssetsPath + "/" + signalsFile);
        var values = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(json);
        // Go through the array and deserialize by type
        foreach (var item in values)
        {
            // This is the item we want to deserialize
            var innerJson = JsonConvert.SerializeObject(item["params"]);
            YAHSTouch touch;
            switch (item["type"])
            {
                case "static":
                    touch = JsonConvert.DeserializeObject<YAHSStatic>(innerJson);
                    break;
                case "stroke":
                    touch = JsonConvert.DeserializeObject<YAHSStroke>(innerJson);
                    break;
                case "sequence":
                    touch = JsonConvert.DeserializeObject<YAHSSequence>(innerJson);
                    break;
                default:
                    touch = JsonConvert.DeserializeObject<YAHSWait>(innerJson);
                    break;
            }

            Database.Add(touch);
        }
    }

    // Take all signals in database
    // and cache them for instant trigger
    public void CacheSignals()
    {
        SendFromParams(CACHE_ADDRESS, new List<bool> {true});
        foreach (var signal in Database) signal.Send(this);
        SendFromParams(CACHE_ADDRESS, new List<bool> {false});
        Debug.Log("Caching started, you should wait a bit...");
    }

    public void Play(string signalName)
    {
        var match = Database.Find(x => x.Name == signalName);
        if (match != null)
            match.Send(this);
        else
            Debug.LogWarning("Trying to send " + signalName + " touch, not in database, not sending !");
    }

    /**
     * Send a touch to YAHS with raw params.
     * - address is the OSC "endpoint" (e.g. /touch, /params)
     * - args are the parameters (which touch, which parameter, etc)
     * See documentation : https://gitlab.utc.fr/translife/projets/socialtouch/yahs/-/blob/master/doc/usage.md
     */
    public void SendFromParams(string address, ICollection args)
    {
        var message = new OscMessage {address = address};
        message.values.AddRange(args);
        _osc.Send(message);
    }
}