using System.Collections.Generic;
using UnityEngine;

class ObjectiveScoutQuest : ObjectiveRandomGotoNPC
{
    protected new enum GotoStates
    {
        NoPosition,
        WaitingForPoint,
        TryComplete,
        Completed
    }

    protected new bool positionSet;
    protected new float distance = 50f;
    private float lastDistance = -1f;
    protected float currentDistance;
    protected new float completionDistance = 10f;
    protected new Vector3 position;
    protected new string icon = "ui_game_symbol_quest";
    protected new bool completeWithinRange = true;
    public static new string PropDistance = "distance";
    public static new string PropCompletionDistance = "completion_distance";

    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Distance;

    public override bool UpdateUI => base.ObjectiveState != ObjectiveStates.Failed;

    public override bool NeedsNPCSetPosition => false;

    public override void SetPosition(Vector3 position, Vector3 size) => this.FinalizePoint((int)position.x, (int)position.y, (int)position.z);

    public override bool SetupPosition(EntityNPC ownerNPC = null, EntityPlayer player = null, List<Vector2> usedPOILocations = null, int entityIDforQuests = -1)
    {
        return this.GetPosition(ownerNPC) != Vector3.zero;
    }

    protected new Vector3 GetPosition(EntityNPC ownerNPC = null)
    {
        Log.Out("[QH] ObjectiveScoutQuest-GetPosition");
        if (base.OwnerQuest.GetPositionData(out this.position, Quest.PositionDataTypes.Location))
        {
            Log.Out("[QH] ObjectiveScoutQuest-GetPosition-1");
            base.OwnerQuest.Position = this.position;
            this.positionSet = true;
            base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, this.NavObjectName);
            base.CurrentValue = 2;
            return this.position;
        }

        if (base.OwnerQuest.GetPositionData(out this.position, Quest.PositionDataTypes.TreasurePoint))
        {
            Log.Out("[QH] ObjectiveScoutQuest-GetPosition-2");
            this.positionSet = true;
            base.OwnerQuest.SetPositionData(Quest.PositionDataTypes.Location, base.OwnerQuest.Position);
            //base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.TreasurePoint, NavObjectName, 1);
            base.CurrentValue = 2;
            return this.position;
        }

        if (base.Value != null && base.Value != "" && !StringParsers.TryParseFloat(base.Value, out distance) && base.Value.Contains("-"))
        {
            string[] array = base.Value.Split('-');
            float num = StringParsers.ParseFloat(array[0]);
            float num2 = StringParsers.ParseFloat(array[1]);
            distance = GameManager.Instance.World.GetGameRandom().RandomFloat * (num2 - num) + num;
        }

        EntityAlive entityAlive = ((ownerNPC == null) ? ((EntityAlive)base.OwnerQuest.OwnerJournal.OwnerPlayer) : ((EntityAlive)ownerNPC));
        if (base.OwnerQuest.Position == Vector3.zero)
        {
            base.OwnerQuest.Position = entityAlive.position;
        }
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            Log.Out("[QH] ObjectiveScoutQuest-GetPosition-3-current position: x:" + entityAlive.position.x + ", z:" + entityAlive.position.z);
            Vector3i vector3i = CalculateRandomPoint(ownerNPC.entityId, distance, base.OwnerQuest.ID);
            if (!GameManager.Instance.World.CheckForLevelNearbyHeights(vector3i.x, vector3i.z, 5) || GameManager.Instance.World.GetWaterAt(vector3i.x, vector3i.z))
            {
                Log.Out("[QH] ObjectiveScoutQuest-GetPosition-3-1");
                return Vector3.zero;
            }

            World world = GameManager.Instance.World;
            if (vector3i.y > 0 && world.IsPositionInBounds(position) && !world.IsPositionWithinPOI(position, 5))
            {
                Log.Out("[QH] ObjectiveScoutQuest-GetPosition-3-2");
                FinalizePoint(vector3i.x, vector3i.y, vector3i.z);
                return position;
            }
        }
        else
        {
            Log.Out("[QH] ObjectiveScoutQuest-GetPosition-4");
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageQuestTreasurePoint>().Setup(ownerNPC.entityId, distance, 1, base.OwnerQuest.QuestCode));
            base.CurrentValue = 1;
        }

        return Vector3.zero;
    }

    public override BaseObjective Clone()
    {
        Log.Out("[QH] ObjectiveScoutQuest-Clone");
        ObjectiveScoutQuest ObjectiveScoutQuest = new ObjectiveScoutQuest();
        CopyValues(ObjectiveScoutQuest);
        ObjectiveScoutQuest.position = position;
        ObjectiveScoutQuest.positionSet = positionSet;
        ObjectiveScoutQuest.completionDistance = completionDistance;
        return ObjectiveScoutQuest;
    }

    public static new Vector3i CalculateRandomPoint(int entityID, float distance, string questID)
    {
        Log.Out("[QH] ObjectiveScoutQuest-CalculateRandomPoint");
        World world = GameManager.Instance.World;
        EntityAlive entityAlive = world.GetEntity(entityID) as EntityAlive;
        Vector3 vector = new Vector3(world.GetGameRandom().RandomFloat * 2f + -1f, 0f, world.GetGameRandom().RandomFloat * 2f + -1f);
        vector.Normalize();
        Vector3 vector2 = entityAlive.position + vector * distance;
        int x = (int)vector2.x;
        int z = (int)vector2.z;
        int y = (int)world.GetHeightAt(vector2.x, vector2.z);
        Vector3i vector3i = new Vector3i(x, y, z);
        Vector3 vector3 = new Vector3(vector3i.x, vector3i.y, vector3i.z);
        if (world.IsPositionInBounds(vector3) && (!(entityAlive is EntityPlayer) || world.CanPlaceBlockAt(vector3i, GameManager.Instance.GetPersistentLocalPlayer())) && !world.IsPositionWithinPOI(vector3, 2))
        {
            Log.Out("[QH] ObjectiveScoutQuest-CalculateRandomPoint-1");
            if (!world.CheckForLevelNearbyHeights(vector2.x, vector2.z, 5) || world.GetWaterAt(vector2.x, vector2.z))
            {
                Log.Out("[QH] ObjectiveScoutQuest-CalculateRandomPoint-2");
                return new Vector3i(0, -99999, 0);
            }

            return vector3i;
        }
        Log.Out("[QH] ObjectiveScoutQuest-CalculateRandomPoint-3");
        return new Vector3i(0, -99999, 0);
    }

    //public new void FinalizePoint(int x, int y, int z)
    //{
    //    Log.Out("[QH] ObjectiveScoutQuest-FinalizePoint");
    //    position = new Vector3(x, y, z);
    //    base.OwnerQuest.SetPositionData(Quest.PositionDataTypes.Location, position);
    //    base.OwnerQuest.Position = position;
    //    positionSet = true;
    //    base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, NavObjectName);
    //    base.CurrentValue = 2;
    //}

    public new void FinalizePoint(int x, int y, int z)
    {
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer || base.CurrentValue == 1)
        {
            position = new Vector3(x, y, z);
            base.OwnerQuest.SetPositionData(Quest.PositionDataTypes.TreasurePoint, position);
            if (base.OwnerQuest.DataVariables.ContainsKey("treasurecontainer"))
            {
                base.OwnerQuest.DataVariables["treasurecontainer"] = string.Format("{0},{1},{2}", x, y, z);
            }
            else
            {
                base.OwnerQuest.DataVariables.Add("treasurecontainer", string.Format("{0},{1},{2}", x, y, z));
            }
            if (base.OwnerQuest.OwnerJournal != null && base.OwnerQuest.OwnerJournal.OwnerPlayer != null)
            {
                base.OwnerQuest.Position = position;
                positionSet = true;
                base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.TreasurePoint, NavObjectName);
                base.CurrentValue = 2;
            }
            else
            {
                base.OwnerQuest.Position = position;
            }
        }
        else
        {
            if (base.OwnerQuest.GetPositionData(out position, Quest.PositionDataTypes.TreasurePoint))
            {
                return;
            }
            position = new Vector3(x, y, z);
            base.OwnerQuest.SetPositionData(Quest.PositionDataTypes.TreasurePoint, position);
            if (base.OwnerQuest.DataVariables.ContainsKey("treasurecontainer"))
            {
                base.OwnerQuest.DataVariables["treasurecontainer"] = string.Format("{0},{1},{2}", x, y, z);
            }
            else
            {
                base.OwnerQuest.DataVariables.Add("treasurecontainer", string.Format("{0},{1},{2}", x, y, z));
            }
            if (base.OwnerQuest.OwnerJournal != null && base.OwnerQuest.OwnerJournal.OwnerPlayer != null)
            {
                base.OwnerQuest.Position = position;
                positionSet = true;
                base.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.TreasurePoint, NavObjectName);
                base.CurrentValue = 2;
            }
            else
            {
                base.OwnerQuest.Position = position;
            }
        }
    }

    public override void Update(float deltaTime)
    {
        if (!this.positionSet)
        {
            Log.Out("[QH] ObjectiveScoutQuest-Update-0");
            GetPosition();
        }
        if (this.OwnerQuest.Active && this.OwnerQuest.MapObject == null && this.OwnerQuest.NavObject == null)
        {
            Log.Out("[QH] ObjectiveScoutQuest-Update-1");
            //this.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.TreasurePoint, this.NavObjectName);
            this.OwnerQuest.HandleMapObject(Quest.PositionDataTypes.Location, this.NavObjectName);
        }

        switch (base.CurrentValue)
        {
            case 0:
                //Log.Out("[QH] ObjectiveScoutQuest-Update-2");
                _ = GetPosition() != Vector3.zero;
                break;
            case 2:
                {
                    //Log.Out("[QH] ObjectiveScoutQuest-Update-3");
                    //Log.Out("[QH] ObjectiveScoutQuest-Update-3-Position: x:" + position.x + ", z:" + position.z + ", y:" + position.y);
                    EntityPlayerLocal ownerPlayer2 = base.OwnerQuest.OwnerJournal.OwnerPlayer;
                    if (base.OwnerQuest.NavObject != null && base.OwnerQuest.NavObject.TrackedPosition != position)
                    {
                        base.OwnerQuest.NavObject.TrackedPosition = position;
                    }

                    Vector3 a2 = ownerPlayer2.position;
                    distance = Vector3.Distance(a2, position);
                    if (distance < completionDistance && base.OwnerQuest.CheckRequirements())
                    {
                        base.CurrentValue = 3;
                        Refresh();
                    }

                    break;
                }
            case 3:
                {
                    //Log.Out("[QH] ObjectiveScoutQuest-Update-4");
                    //Log.Out("[QH] ObjectiveScoutQuest-Update-4-Position: x:" + position.x + ", z:" + position.z + ", y:" + position.y);
                    if (completeWithinRange)
                    {
                        QuestEventManager.Current.RemoveObjectiveToBeUpdated(this);
                        break;
                    }

                    EntityPlayerLocal ownerPlayer = base.OwnerQuest.OwnerJournal.OwnerPlayer;
                    if (base.OwnerQuest.NavObject != null && base.OwnerQuest.NavObject.TrackedPosition != position)
                    {
                        base.OwnerQuest.NavObject.TrackedPosition = position;
                    }

                    Vector3 a = ownerPlayer.position;
                    distance = Vector3.Distance(a, position);
                    if (distance > completionDistance)
                    {
                        base.CurrentValue = 2;
                        Refresh();
                    }

                    break;
                }
            case 1:
                //Log.Out("[QH] ObjectiveScoutQuest-Update-5");
                break;
        }
    }

    public override void Refresh()
    {
        Log.Out("[QH] ObjectiveScoutQuest-Refresh");
        this.Complete = this.CurrentValue == (byte)3;
        if (!this.Complete)
        {
            Log.Out("[QH] ObjectiveScoutQuest-Refresh-1");
            return;
        }
        this.OwnerQuest.CheckForCompletion(playObjectiveComplete: this.PlayObjectiveComplete);

    }

    public override bool SetLocation(Vector3 pos, Vector3 size)
    {
        //Log.Out("[QH] ObjectiveScoutQuest-SetLocation");
        FinalizePoint((int)pos.x, (int)pos.y, (int)pos.z);
        return true;
    }

    public override void ParseProperties(DynamicProperties properties)
    {
        //Log.Out("[QH] ObjectiveScoutQuest-ParseProperties");
        base.ParseProperties(properties);
        if (properties.Values.ContainsKey(PropDistance))
        {
            base.Value = properties.Values[PropDistance];
        }

        if (properties.Values.ContainsKey(PropCompletionDistance))
        {
            completionDistance = StringParsers.ParseFloat(properties.Values[PropCompletionDistance]);
        }
    }

    public override string ParseBinding(string bindingName)
    {
        //string iD = base.ID;
        //string value = base.Value;
        switch (bindingName)
        {
            case "distance":
                {
                    if (base.OwnerQuest.QuestGiverID == -1)
                    {
                        break;
                    }
                    EntityNPC entityNPC2 = GameManager.Instance.World.GetEntity(base.OwnerQuest.QuestGiverID) as EntityNPC;
                    if (entityNPC2 != null)
                    {
                        Vector3 vector4 = base.OwnerQuest.Position;
                        vector4.y = 0f;
                        vector4.y = 0f;
                        Vector3 a = entityNPC2.position;
                        a.y = 0f;
                        currentDistance = Vector3.Distance(a, vector4);
                        return ValueDisplayFormatters.Distance(currentDistance);
                    }
                    break;
                }
            case "direction":
                {
                    if (base.OwnerQuest.QuestGiverID == -1)
                    {
                        break;
                    }
                    EntityNPC entityNPC = GameManager.Instance.World.GetEntity(base.OwnerQuest.QuestGiverID) as EntityNPC;
                    if (entityNPC != null)
                    {
                        Vector3 vector = base.OwnerQuest.Position;
                        vector.y = 0f;
                        Vector3 vector2 = entityNPC.position;
                        vector2.y = 0f;
                        return ValueDisplayFormatters.Direction(GameUtils.GetDirByNormal(new Vector2(vector.x - vector2.x, vector.z - vector2.z)));
                    }
                    break;
                }
        }
        return "";
    }
}
