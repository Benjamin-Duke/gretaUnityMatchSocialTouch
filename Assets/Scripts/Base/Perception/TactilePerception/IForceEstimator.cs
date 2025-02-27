using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WellFormedNames;

namespace TactilePerception
{
    public interface IForceEstimator 
    {
        // Start is called before the first frame update
        float GetEstimatedForce(string boneName, float inverseStiffness);
    }
}

