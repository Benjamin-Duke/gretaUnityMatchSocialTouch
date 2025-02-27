using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TouchType
{
    Unknown = 0,
    Pat = 1,
    Hit = 2,
    Caress = 3,
    Rub = 4,
    Scratch = 5,
    Brush = 6,
    Press = 7
}

public class SequenceInterpreter
{
    #region Custom struct

    public struct Interpretation3Class
    {
        // Struct containing 3 partition classes. In our situation, we only used 3 classes per variable so we can use this specifically.
        // For custom parameters requiring more or less than 3 classes, it is still possible to use a group of InterpretationClass,
        // see the DistanceInterpretation for a simple example.
        // Keep in mind that adding a new variable parameter or changing the class number of an existing parameter
        // will require an update of the interpretation table (changing its dimensions, or having a series of different tables).

        public float min, max;
        public readonly float v1;
        public readonly float v2;
        public readonly float v3;
        public readonly float v4;
        public InterpretationClass class1, class2, class3;
        public string name;

        public Interpretation3Class(
            float newMin, float newMax, float newV1, float newV2, float newV3, float newV4,
            string newName, string class1Name, string class2Name, string class3Name)
        {
            min = newMin;
            max = newMax;
            v1 = newV1;
            v2 = newV2;
            v3 = newV3;
            v4 = newV4;
            name = newName;
            class1 = new InterpretationClass
            {
                parameters = new Vector4(Mathf.NegativeInfinity, Mathf.NegativeInfinity, v1, v2),
                className = class1Name
            };
            class2 = new InterpretationClass
            {
                parameters = new Vector4(v1, v2, v3, v4),
                className = class2Name
            };
            class3 = new InterpretationClass
            {
                parameters = new Vector4(v3, v4, Mathf.Infinity, Mathf.Infinity),
                className = class3Name
            };
        }

        // Get the name of the class with has the maximum confidence
        public string MaximumConfidenceClassName(float value)
        {
            var rawResults = ComputeTripletClass(value);
            var confidenceList = new List<float> {rawResults.x, rawResults.y, rawResults.z};
            var classNames = new List<string> {class1.className, class2.className, class3.className};
            var maxConfidenceIndex = confidenceList.IndexOf(confidenceList.Max());
            return classNames[maxConfidenceIndex];
        }


        // Function calling the evaluation of each class and concatenating the results into a single vector3
        public Vector3 ComputeTripletClass(float value)
        {
            return new Vector3(class1.EvaluateClass(value), class2.EvaluateClass(value), class3.EvaluateClass(value));
        }
    }

    #endregion


    #region Interpretation variables

    public static int typeNb { get; } = Enum.GetNames(typeof(TouchType)).Length;

    // Fuzzy interpretation table
    // Its 3 dimensions matches, in order, the three intepretation parameters:
    // - total duration
    // - mean velocity
    // - mean force
    private static readonly TouchType[,,] table = new TouchType[3, 3, 3]
    {
        {
            {TouchType.Brush, TouchType.Pat, TouchType.Press},
            {TouchType.Brush, TouchType.Pat, TouchType.Hit},
            {TouchType.Pat, TouchType.Hit, TouchType.Hit}
        },

        {
            {TouchType.Brush, TouchType.Pat, TouchType.Press},
            {TouchType.Caress, TouchType.Rub, TouchType.Rub},
            {TouchType.Rub, TouchType.Scratch, TouchType.Scratch}
        },

        {
            {TouchType.Brush, TouchType.Press, TouchType.Press},
            {TouchType.Caress, TouchType.Caress, TouchType.Rub},
            {TouchType.Rub, TouchType.Scratch, TouchType.Scratch}
        }
    };

    // Second fuzzy interpretation table
    // It takes into account the initial impact velocity
    private static readonly TouchType[] table2 = new TouchType[3]
    {
        TouchType.Brush, TouchType.Pat, TouchType.Hit
    };

    // Check the excel table to preview the class partition
    public static Interpretation3Class
        durationClasses =
            new Interpretation3Class(0f, 2f, 0.1f, 0.6f, 0.8f, 1.6f, "Total Duration", "Short", "Medium", "Long"),
        meanVelocityClasses =
            new Interpretation3Class(0f, 1f, 0.02f, 0.03f, 0.1f, 0.4f, "Mean Velocity", "Static", "CTOptimal", "Fast"),
        meanForceClasses =
            new Interpretation3Class(0f, 1f, 0.1f, 0.5f, 0.5f, 0.9f, "Mean Force", "Low", "Medium", "High"),
        impactVelocityClasses = new Interpretation3Class(0f, 4f, 0.2f, 0.7f, 0.8f, 3.5f, "Impact Velocity", "Weak",
            "Medium", "Strong");

    #endregion

    #region Interpretation methods

    public static float[] InterpretSequence(
        TactileSequence sequence,
        float durationOfSequence = -1,
        float meanVelocity = -1,
        float meanForce = -1,
        float initialImpactVelocity = -1,
        bool debug = false)
    {
        if (durationOfSequence < 0) durationOfSequence = sequence.totalDuration;
        if (meanVelocity < 0) meanVelocity = sequence.meanVelocity;
        if (meanForce < 0) meanForce = sequence.meanForce;
        if (initialImpactVelocity < 0) initialImpactVelocity = sequence.initialImpactVelocity;

        Vector3
            durationResult = durationClasses.ComputeTripletClass(durationOfSequence),
            meanVelocityResult = meanVelocityClasses.ComputeTripletClass(meanVelocity),
            meanForceResult = meanForceClasses.ComputeTripletClass(meanForce),
            impactVelocityResult = impactVelocityClasses.ComputeTripletClass(initialImpactVelocity);

        // array of float containing the result per touch type (order is identical to the enum)
        var rawResult = EvaluateTable(durationResult, meanVelocityResult, meanForceResult, ProbabilisticNorm,
            MaxCoNorm);
        rawResult = EvaluateTable2(rawResult, durationResult, impactVelocityResult, MinNorm, MaxCoNorm);
        // we choose the touch type with the best score
        float maxValue = 0;
        var index = -1;
        for (var i = 0; i < typeNb; i++)
            if (maxValue < rawResult[i])
            {
                maxValue = rawResult[i];
                index = i;
            }

        sequence.sequenceTouchType = (TouchType) index;
        if (debug)
        {
            var resultMessage = "Raw results:\n";
            for (var i = 0; i < typeNb; i++) resultMessage += (TouchType) i + ": " + rawResult[i] + "\n";
            resultMessage += "Best confidence is " + sequence.sequenceTouchType + " with a value of " + maxValue;
            Debug.Log(resultMessage);
        }

        return rawResult;
    }

    // Function evaluating the complete table from the 3 variables depscription
    public static float[] EvaluateTable(Vector3 triplet1, Vector3 triplet2, Vector3 triplet3, TNorm tNorm,
        TCoNorm tCoNorm)
    {
        var interpretationResult = new float[typeNb];
        for (var i = 0; i < typeNb; i++) interpretationResult[i] = 0;
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++)
        for (var k = 0; k < 3; k++)
        {
            // interpreting the touch type as an index.
            var typeIndex = (int) table[i, j, k];
            // union of the new result with the current value
            interpretationResult[typeIndex] = tCoNorm(
                new[]
                {
                    // previous result for the same type
                    interpretationResult[typeIndex],
                    // intersection of the variable classes
                    tNorm(new[] {triplet1[i], triplet2[j], triplet3[k]})
                });
        }

        return interpretationResult;
    }

    public static float[] EvaluateTable2(float[] interpretationResult, Vector3 durationResult, Vector3 impactResult,
        TNorm tNorm, TCoNorm tCoNorm)
    {
        for (var i = 0; i < 3; i++)
        {
            var typeIndex = (int) table2[i];
            interpretationResult[typeIndex] = tCoNorm(
                new[]
                {
                    // previous result for the same type
                    interpretationResult[typeIndex],
                    // intersection of the variable classes, we only consider the short class of durationResult
                    tNorm(new[] {durationResult[0], impactResult[i]})
                }
            );
        }

        return interpretationResult;
    }

    #endregion

    #region TNorm methods

    // Delegate for an intersection operator
    public delegate float TNorm(float[] args);

    public static float MinNorm(float[] args)
    {
        return Mathf.Min(args);
    }

    public static float ProbabilisticNorm(float[] args)
    {
        if (args.Length == 0) return 0;
        var result = args[0];
        if (args.Length == 1) return result;
        for (var i = 1; i < args.Length; i++) result *= args[i];
        return result;
    }

    #endregion

    #region TCoNorm methods

    // Delegate for an union operator
    public delegate float TCoNorm(float[] args);

    public static float MaxCoNorm(float[] args)
    {
        return Mathf.Max(args);
    }

    public static float ProbabilisticCoNorm(float[] args)
    {
        if (args.Length == 0) return 0;
        var result = args[0];
        if (args.Length == 1) return result;
        for (var i = 1; i < args.Length; i++) result = result + args[i] - result + args[i];
        return result;
    }

    #endregion
}
