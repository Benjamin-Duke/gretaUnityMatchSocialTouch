using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * This file describes different touches modes
 */
public abstract class YAHSTouch
{
    public readonly string SEQUENCE_ADDRESS = "/sequence";
    public readonly string STATIC_ADDRESS = "/touch/static";
    public readonly string STROKE_ADDRESS = "/touch/stroke";
    public readonly string WAIT_ADDRESS = "/touch/wait";
    public string Name { get; set; }
    public int Duration { get; set; }
    public float MinimumModulation { get; set; }
    public string ModulationType { get; set; }
    public float Intensity { get; set; }
    public string TouchType { get; set; }

    // Send a message to YAHS : each class
    // prepares its args and send a message to YAHS
    // If cache is true, don't play signal and cache it
    public abstract void Send(YAHSController controller);
}

[Serializable]
public class YAHSStatic : YAHSTouch
{
    public int RampUp { get; set; }
    public int RampDown { get; set; }
    public List<List<int>> Actuators { get; set; }

    public override void Send(YAHSController controller)
    {
        var args = new List<object>
            {Duration, RampUp, RampDown, MinimumModulation, ModulationType, Intensity, TouchType};
        foreach (var v in Actuators.SelectMany(x => x)) args.Add(v);
        controller.SendFromParams(STATIC_ADDRESS, args);
    }
}

[Serializable]
public class YAHSStroke : YAHSTouch
{
    public List<float> Start { get; set; }
    public List<float> End { get; set; }
    public float MinimumFullIntensityFactor { get; set; }

    public override void Send(YAHSController controller)
    {
        var args = new List<object>
        {
            Start[0], Start[1], End[0], End[1], Duration, Intensity, TouchType, MinimumModulation,
            MinimumFullIntensityFactor, ModulationType
        };
        controller.SendFromParams(STROKE_ADDRESS, args);
    }
}

[Serializable]
public class YAHSWait : YAHSTouch
{
    public override void Send(YAHSController controller)
    {
        var args = new List<object> {Duration};
        controller.SendFromParams(WAIT_ADDRESS, args);
    }
}

[Serializable]
public class YAHSSequence : YAHSTouch
{
    // Refers to base.Name of other subclasses :)
    // So you need to reference existing signals
    // in the YAHSController Database before
    // sending a sequence
    public List<string> Signals { get; set; }

    public override void Send(YAHSController controller)
    {
        var realSignals = new List<YAHSTouch>();
        // Fetch real signals
        foreach (var signalName in Signals)
        {
            var signal = controller.Database.Find(x => x.Name == signalName);
            if (signal != null)
                realSignals.Add(signal);
            else
                Debug.LogWarning("Signal " + signalName + " in sequence " + Name +
                                 " not found in database, ignore it!");
        }

        controller.SendFromParams(SEQUENCE_ADDRESS, new List<bool> {true});
        foreach (var signal in realSignals) signal.Send(controller);

        controller.SendFromParams(SEQUENCE_ADDRESS, new List<bool> {false});
    }
}