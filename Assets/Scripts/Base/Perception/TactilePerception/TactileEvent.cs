using UnityEngine;

public struct TactileEvent
{
    public float timeStamp;
    public int frame;
    public string tactileCell;
    public string handCollider;
    public float velocity;
    public float force;
    public Vector2 localisationParameters;
    public AnatomyParameters.AnatomyType cellType;

    public TactileEvent(float newTime, int newFrame, string newTactileCell, float newVelocity, float newForce,
        Vector2 newLocalisation, AnatomyParameters.AnatomyType newCellType, string handPartName = "hand")
    {
        timeStamp = newTime;
        frame = newFrame;
        tactileCell = newTactileCell;
        handCollider = handPartName;
        velocity = newVelocity;
        force = newForce;
        localisationParameters = newLocalisation;
        cellType = newCellType;
    }

    // public bool isStimulatedCell(string candidateCell){
    // 	return tactileCell.Equals (candidateCell);
    // }
}