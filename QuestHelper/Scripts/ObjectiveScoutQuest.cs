using UnityEngine;

class ObjectiveScoutQuest : ObjectiveRandomGotoNPC
{
    public override void SetPosition(Vector3 position, Vector3 size) => this.FinalizePoint((int)position.x, (int)position.y, (int)position.z);

    public override BaseObjective Clone()
    {
        ObjectiveScoutQuest ObjectiveScoutQuest = new ObjectiveScoutQuest();
        CopyValues(ObjectiveScoutQuest);
        ObjectiveScoutQuest.position = position;
        ObjectiveScoutQuest.positionSet = positionSet;
        ObjectiveScoutQuest.completionDistance = completionDistance;
        return ObjectiveScoutQuest;
    }
}
