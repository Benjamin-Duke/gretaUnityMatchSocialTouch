using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TactileSequence
{
    public string inferredTouchType;
    public float initialImpactVelocity;

    public bool isComplete;
    public float lastTime;
    public float meanForce;
    public float meanVelocity;

    public float[] rawTypeResult;

    public SortedList<int, List<TactileEvent>> sequence = new SortedList<int, List<TactileEvent>>();

    public TouchType sequenceTouchType;
    public float startTime;
    public List<TactileEvent> tactileEventList;
    public float totalDuration;

    public TactileSequence(float initialTime, float initialSpeed)
    {
        startTime = initialTime;
        initialImpactVelocity = initialSpeed;
        tactileEventList = new List<TactileEvent>();
    }

    public TactileSequence(TactileSequence toCopy)
    {
        isComplete = toCopy.isComplete;
        totalDuration = toCopy.totalDuration;
        startTime = toCopy.startTime;
        lastTime = toCopy.lastTime;
        meanVelocity = toCopy.meanVelocity;
        meanForce = toCopy.meanForce;
        inferredTouchType = toCopy.inferredTouchType;
        sequenceTouchType = toCopy.sequenceTouchType;
        initialImpactVelocity = toCopy.initialImpactVelocity;
        rawTypeResult = toCopy.rawTypeResult;
        sequence = new SortedList<int, List<TactileEvent>>(toCopy.sequence);
        tactileEventList = new List<TactileEvent>(toCopy.tactileEventList);
    }

    public void AddEvent(int frameOfEvent, TactileEvent evt)
    {
        if (!sequence.ContainsKey(frameOfEvent))
            sequence.Add(frameOfEvent, new List<TactileEvent>());
        sequence[frameOfEvent].Add(evt);
    }


    public void CompleteSequence(float finishingTime)
    {
        ComputeSequenceValues();
        totalDuration = finishingTime - startTime;
        lastTime = finishingTime;
        isComplete = true;
    }

    public void Clear()
    {
        isComplete = false;
        totalDuration = new float();
        startTime = new float();
        lastTime = new float();
        initialImpactVelocity = new float();
        meanForce = -1;
        meanVelocity = -1;
        inferredTouchType = "";
        sequenceTouchType = TouchType.Unknown;
        rawTypeResult = null;
        sequence.Clear();
        tactileEventList = new List<TactileEvent>();
    }

    public void ComputeSequenceValues()
    {
        var tempFinishTime = tactileEventList[tactileEventList.Count - 1].timeStamp;
        totalDuration = tempFinishTime - startTime;
        meanForce = 0;
        meanVelocity = 0;
        //We take the palm as our reference for the mean velocities and forces.
        var palmEventsList = tactileEventList.Where(tactileEvent => tactileEvent.handCollider == "L_Palm").ToList();
        foreach (var tactileEvent in palmEventsList)
        {
            meanForce += tactileEvent.force;
            meanVelocity += tactileEvent.velocity;
        }

        meanForce /= palmEventsList.Count;
        // We exclude the initial velocity of the mean velocity to reduce the variance.
        meanVelocity = palmEventsList.Count > 1 ? (meanVelocity-palmEventsList.FirstOrDefault().velocity) / (palmEventsList.Count - 1) : 0;
        
    }

    // Interpret the sequence and return the touch type which has the maximum confidence
    public TouchType InterpretCompleteSequence()
    {
        if (!isComplete) ComputeSequenceValues();
        rawTypeResult = SequenceInterpreter.InterpretSequence(this);
        return (TouchType) rawTypeResult.ToList().IndexOf(rawTypeResult.Max());
    }

    public void InterpretFromIndex(int startIndex, int endIndex)
    {
        if (startIndex > endIndex || startIndex >= tactileEventList.Count || startIndex < 0 ||
            endIndex >= tactileEventList.Count)
        {
            if (Debug.isDebugBuild) Debug.LogError("Index limits given in InterpretFromIndex make no sense.");
            return;
        }

        float
            partialTime = tactileEventList[endIndex].timeStamp - tactileEventList[startIndex].timeStamp,
            startVelocity = tactileEventList[startIndex].velocity,
            partialMeanForce = 0,
            partialMeanVelocity = 0;

        for (var i = startIndex; i <= endIndex; i++)
        {
            partialMeanForce += tactileEventList[i].force;
            partialMeanVelocity += tactileEventList[i].velocity;
        }

        partialMeanForce /= endIndex - startIndex + 1;
        partialMeanVelocity /= endIndex - startIndex + 1;

        rawTypeResult =
            SequenceInterpreter.InterpretSequence(this, partialTime, partialMeanVelocity, partialMeanForce,
                startVelocity);
    }


    // Interpret the sequence using n = eventNb events from the end of the sequence. Can be used to interpet while the sequence is being written. 
    public void InterpretFromLastEvent(int eventNb)
    {
        if (eventNb >= tactileEventList.Count) InterpretCompleteSequence();
        InterpretFromIndex(tactileEventList.Count - 1 - eventNb, tactileEventList.Count - 1);
    }
}