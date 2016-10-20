using GentrifiedSkylines.Redirection;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace GentrifiedSkylines.Detours
{
    [TargetType(typeof(HumanAI))]
    internal class HumanAIDetour : CitizenAI
    {
        public override Color GetColor(ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode)
        {
            if (infoMode != InfoManager.InfoMode.Transport)
                return base.GetColor(instanceID, ref data, infoMode);
            if ((data.m_flags & CitizenInstance.Flags.WaitingTaxi) != CitizenInstance.Flags.None)
                return Singleton<TransportManager>.instance.m_properties.m_transportColors[5];
            if ((int)data.m_path != 0 && (data.m_flags & (CitizenInstance.Flags.WaitingTransport | CitizenInstance.Flags.EnteringVehicle)) != CitizenInstance.Flags.None)
            {
                ushort num = Singleton<NetManager>.instance.m_nodes.m_buffer[(int)Singleton<NetManager>.instance.m_segments.m_buffer[(int)Singleton<PathManager>.instance.m_pathUnits.m_buffer[(long)data.m_path].GetPosition((int)data.m_pathPositionIndex >> 1).m_segment].m_startNode].m_transportLine;
                if ((int)num != 0)
                    return Singleton<TransportManager>.instance.m_lines.m_buffer[(int)num].GetColor();
            }
            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
        }

        protected TransferManager.TransferReason GetLeavingReason(uint citizenID, ref Citizen data)
        {
            switch (data.WealthLevel)
            {
                case Citizen.Wealth.Low:
                    return TransferManager.TransferReason.LeaveCity0;

                case Citizen.Wealth.Medium:
                    return TransferManager.TransferReason.LeaveCity1;

                case Citizen.Wealth.High:
                    return TransferManager.TransferReason.LeaveCity2;

                default:
                    return TransferManager.TransferReason.LeaveCity0;
            }
        }

        protected void FindVisitPlace(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason)
        {
            Singleton<TransferManager>.instance.AddIncomingOffer(reason, new TransferManager.TransferOffer()
            {
                Priority = Singleton<SimulationManager>.instance.m_randomizer.Int32(8U),
                Citizen = citizenID,
                Position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)sourceBuilding].m_position,
                Amount = 1,
                Active = true
            });
        }

        public bool StartMoving(uint citizenID, ref Citizen data, ushort sourceBuilding, ushort targetBuilding)
        {
            CitizenManager instance1 = Singleton<CitizenManager>.instance;
            if ((int)targetBuilding == (int)sourceBuilding || (int)targetBuilding == 0 || (Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)targetBuilding].m_flags & Building.Flags.Active) == Building.Flags.None)
                return false;
            if ((int)data.m_instance != 0)
            {
                this.m_info.m_citizenAI.SetTarget(data.m_instance, ref instance1.m_instances.m_buffer[(int)data.m_instance], targetBuilding);
                data.CurrentLocation = Citizen.Location.Moving;
                return true;
            }
            if ((int)sourceBuilding == 0)
            {
                sourceBuilding = data.GetBuildingByLocation();
                if ((int)sourceBuilding == 0)
                    return false;
            }
            ushort instance2;
            if (!instance1.CreateCitizenInstance(out instance2, ref Singleton<SimulationManager>.instance.m_randomizer, this.m_info, citizenID))
                return false;
            this.m_info.m_citizenAI.SetSource(instance2, ref instance1.m_instances.m_buffer[(int)instance2], sourceBuilding);
            this.m_info.m_citizenAI.SetTarget(instance2, ref instance1.m_instances.m_buffer[(int)instance2], targetBuilding);
            data.CurrentLocation = Citizen.Location.Moving;
            return true;
        }

        /*
         public const byte FLAG_CREATED = 1;
  public const byte FLAG_IS_HEAVY = 16;
  public const byte FLAG_IGNORE_BLOCKED = 32;
  public const byte FLAG_STABLE_PATH = 64;
  public const byte FLAG_RANDOM_PARKING = 128;
  public const byte FLAG_QUEUED = 1;
  public const byte FLAG_CALCULATING = 2;
  public const byte FLAG_READY = 4;
  public const byte FLAG_FAILED = 8;
  */

        public override void SimulationStep(ushort instanceID, ref CitizenInstance data, Vector3 physicsLodRefPos)
        {
            if ((data.m_flags & CitizenInstance.Flags.WaitingPath) != CitizenInstance.Flags.None)                               //If citizen is waiting path
            {
                byte num = Singleton<PathManager>.instance.m_pathUnits.m_buffer[(long)data.m_path].m_pathFindFlags;                 //num = path flags
                if (((int)num & 4) != 0)                                                                                            //if flag ready is true
                {
                    this.Spawn(instanceID, ref data);                                                                                   //spawn this
                    data.m_pathPositionIndex = byte.MaxValue;                                                                           //index = byte.maxvalue
                    data.m_flags &= ~CitizenInstance.Flags.WaitingPath;                                                                 //set flag WaitingPath to false
                    data.m_flags &= ~CitizenInstance.Flags.TargetFlags;                                                                 //set flag TargetFlags to false
                    this.PathfindSuccess(instanceID, ref data);                                                                         //PathFind Succcess
                }
                else if (((int)num & 8) != 0)                                                                                       //else (if flag ready is false andflag Flag_Failed is true)
                {
                    data.m_flags &= ~CitizenInstance.Flags.WaitingPath;                                                                 //set flag WaitingPath to true
                    data.m_flags &= ~CitizenInstance.Flags.TargetFlags;                                                                 //set flag TargetFlags to true
                    Singleton<PathManager>.instance.ReleasePath(data.m_path);
                    data.m_path = 0U;                                                                                                   //empty m_path
                    this.PathfindFailure(instanceID, ref data);                                                                         //PathFind Failure
                    return;                                                                                                             //END SIMULATION STEP
                }
            }
            base.SimulationStep(instanceID, ref data, physicsLodRefPos);                                                            //Run simulationstep of CitizenAI *******
            CitizenManager instance1 = Singleton<CitizenManager>.instance;                                                          //instance1 = Citizen Manager
            VehicleManager instance2 = Singleton<VehicleManager>.instance;                                                          //instance2 = Vehicle Manager
            ushort num1 = 0;                                                                                                        //num1 = 0
            if ((int)data.m_citizen != 0)                                                                                           //if this citizen's ID is not 0
                num1 = instance1.m_citizens.m_buffer[(long)data.m_citizen].m_vehicle;                                                   //num1 = this citizen's vehicle
            if ((int)num1 != 0)                                                                                                     //if num1 is not 0 (is now valid)
            {
                VehicleInfo info = instance2.m_vehicles.m_buffer[(int)num1].Info;                                                       //info is the vehicle manager's buffer of num1 (car ID)'s info
                if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)                                                              //if info's vehicle type is Bicycle
                {
                    info.m_vehicleAI.SimulationStep(num1, ref instance2.m_vehicles.m_buffer[(int)num1], num1, ref instance2.m_vehicles.m_buffer[(int)num1], 0);
                    //Run the VehicleAI's simulation step (car ID, its buffer (a vehicle object), carID (Leader), Vehicle Object (leader), 0 (physics)  .
                    num1 = (ushort)0;                                                                                                       //num1 (reset vehicleID)
                }
            }
            if ((int)num1 != 0 || (data.m_flags & (CitizenInstance.Flags.Character | CitizenInstance.Flags.WaitingPath)) != CitizenInstance.Flags.None)
                //if num1 (vehicleID) is empty or if either the Character flag or the WaitingPath flag are true
                return;                                                                                                                 //END SIMULATION STEP
            data.m_flags &= ~CitizenInstance.Flags.TargetFlags;                                                                     //TargetFlags = false
            this.ArriveAtDestination(instanceID, ref data, false);                                                                  //ArriveAtDestination(failed)
            instance1.ReleaseCitizenInstance(instanceID);                                                                           //Release this citizen
        }

        protected virtual void PathfindSuccess(ushort instanceID, ref CitizenInstance data)
        {
            bool flag = false;
            uint num = data.m_citizen;
            if ((int)num != 0)                                                                                                                      //if this citizen is valid
            {
                CitizenManager instance = Singleton<CitizenManager>.instance;
                flag = (instance.m_citizens.m_buffer[(long)num].m_flags & Citizen.Flags.DummyTraffic) != Citizen.Flags.None;                            //flag true if DummyTraffic is true
                instance.m_citizens.m_buffer[(long)num].m_flags &= ~Citizen.Flags.MovingIn;                                                             //set Citizen.Flag MovingIn to true
            }
            BuildingManager instance1 = Singleton<BuildingManager>.instance;
            if ((int)data.m_sourceBuilding != 0)                                                                                                        //if sourcebuilding is valid
            {
                BuildingAI.PathFindType type = !flag ? BuildingAI.PathFindType.LeavingHuman : BuildingAI.PathFindType.LeavingDummy;
                instance1.m_buildings.m_buffer[(int)data.m_sourceBuilding].Info.m_buildingAI.PathfindSuccess(data.m_sourceBuilding, ref instance1.m_buildings.m_buffer[(int)data.m_sourceBuilding], type);
            }
            if ((int)data.m_targetBuilding == 0)
                return;
            BuildingAI.PathFindType type1 = !flag ? BuildingAI.PathFindType.EnteringHuman : BuildingAI.PathFindType.EnteringDummy;
            instance1.m_buildings.m_buffer[(int)data.m_targetBuilding].Info.m_buildingAI.PathfindSuccess(data.m_targetBuilding, ref instance1.m_buildings.m_buffer[(int)data.m_targetBuilding], type1);
        }

        protected virtual void PathfindFailure(ushort instanceID, ref CitizenInstance data)
        {
            VehicleInfo vehicleInfo = this.GetVehicleInfo(instanceID, ref data, false);
            if (vehicleInfo != null && vehicleInfo.m_vehicleType != VehicleInfo.VehicleType.Bicycle)
            {
                bool flag = false;
                uint num = data.m_citizen;
                if ((int)num != 0)
                    flag = (Singleton<CitizenManager>.instance.m_citizens.m_buffer[(long)num].m_flags & Citizen.Flags.DummyTraffic) != Citizen.Flags.None;
                BuildingManager instance = Singleton<BuildingManager>.instance;
                if ((int)data.m_sourceBuilding != 0)
                {
                    BuildingAI.PathFindType type = !flag ? BuildingAI.PathFindType.LeavingHuman : BuildingAI.PathFindType.LeavingDummy;
                    instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].Info.m_buildingAI.PathfindFailure(data.m_sourceBuilding, ref instance.m_buildings.m_buffer[(int)data.m_sourceBuilding], type);
                }
                if ((int)data.m_targetBuilding != 0)
                {
                    BuildingAI.PathFindType type = !flag ? BuildingAI.PathFindType.EnteringHuman : BuildingAI.PathFindType.EnteringDummy;
                    instance.m_buildings.m_buffer[(int)data.m_targetBuilding].Info.m_buildingAI.PathfindFailure(data.m_targetBuilding, ref instance.m_buildings.m_buffer[(int)data.m_targetBuilding], type);
                }
            }
            this.ArriveAtDestination(instanceID, ref data, false);
            Singleton<CitizenManager>.instance.ReleaseCitizenInstance(instanceID);
        }

        protected virtual void Spawn(ushort instanceID, ref CitizenInstance data)
        {
            data.Spawn(instanceID);
        }

        /*
        None = 0,
        Created = 1,
        Deleted = 2,
        Underground = 4,
        CustomName = 8,
        Character = 16,
        BorrowCar = 32,
        HangAround = 64,
        InsideBuilding = 128,
        WaitingPath = 256,
        WaitingTransport = 512,
        TryingSpawnVehicle = 1024,
        EnteringVehicle = 2048,
        BoredOfWaiting = 4096,
        CannotUseTransport = 8192,
        Panicking = 16384,
        OnPath = 32768,
        SittingDown = 65536,
        AtTarget = 131072,
        RequireSlowStart = 262144,
        Transition = 524288,
        RidingBicycle = 1048576,
        OnBikeLane = 2097152,
        WaitingTaxi = 4194304,
        CannotUseTaxi = 8388608,
        CustomColor = 16777216,
        TargetFlags = SittingDown | Panicking | HangAround,
        All = -1,
        */

        [RedirectMethod]
        public override void SimulationStep(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, bool lodPhysics)
        {
            //Debug.Log(CitizenManager.instance.GetDefaultCitizenName(citizenData.m_citizen) + ", leaving from " + BuildingManager.instance.GetBuildingName(citizenData.m_sourceBuilding, new InstanceID { Building = citizenData.m_sourceBuilding }) + ", is headed to target " + BuildingManager.instance.GetBuildingName(citizenData.m_targetBuilding, new InstanceID { Building = citizenData.m_targetBuilding }) + ".");
            //Debug.Log(CitizenManager.instance.GetDefaultCitizenName(citizenData.m_citizen) + ", leaving from " + BuildingManager.instance.m_buildings.m_buffer[(int)citizenData.m_sourceBuilding].m_position.x + "/" + BuildingManager.instance.m_buildings.m_buffer[(int)citizenData.m_sourceBuilding].m_position.z + " is traveling to " + BuildingManager.instance.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].m_position.x + "/" + BuildingManager.instance.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].m_position.z + ".");
            //Debug.Log(CitizenManager.instance.GetDefaultCitizenName(citizenData.m_citizen) + ", is traveling to " + citizenData.m_targetPos.x + "/" + citizenData.m_targetPos.z + ". and is at " + frameData.m_position.x + "/" + frameData.m_position.z + ".");

            /*
            citizenData.m_path
            citizenData.m_targetDir
            citizenData.m_targetPos
            PathManager.instance.m_properties
            frameData.m_insideBuilding
            frameData.m_position
            frameData.m_velocity
            */

            uint num1 = Singleton<SimulationManager>.instance.m_currentFrameIndex;                                                                                                      //num1 = current frame
            Vector3 vector3_1 = (Vector3)citizenData.m_targetPos - frameData.m_position;                                                                                                //vector3_1 = targetpos - citizen's frame location
            float f = lodPhysics || (double)citizenData.m_targetPos.w <= 1.0 / 1000.0 ? vector3_1.sqrMagnitude : VectorUtils.LengthSqrXZ(vector3_1);                                    //set physics
            float sqrMagnitude1 = frameData.m_velocity.sqrMagnitude;                                                                                                                    //sqrMagnitude1 = square lendth of the frame vector
            float minSqrDistance = Mathf.Max(sqrMagnitude1 * 3f, 3f);                                                                                                                   //minSqrDistance = max (sqrMagnitude1 * 3 , 3)
            if (lodPhysics && (long)(num1 >> 4 & 3U) == (long)((int)instanceID & 3))                                                                                                    //if physics and if ???
                minSqrDistance *= 4f;                                                                                                                                                       //multiply minSqrDistance by 4
            bool flag1 = false;                                                                                                                                                         //flag1 = false
            if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) != CitizenInstance.Flags.None)                                                                         //if the citizen is trying to spawn a vehicle
            {
                bool flag2 = true;                                                                                                                                                          //flag2 is true
                if ((int)++citizenData.m_waitCounter == (int)byte.MaxValue || (int)citizenData.m_path == 0)                                                                                 //if the citizen has no path or if their wait counter is approaching max
                    flag2 = false;                                                                                                                                                              //flag2 is false
                if (flag2)                                                                                                                                                                  //if flag is true
                {
                    PathUnit.Position position;
                    flag2 = Singleton<PathManager>.instance.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position);                //flag2 (bool) set to validity of citizen's location
                    if (flag2)                                                                                                                                                                  //if flag2 is still true
                        flag2 = this.SpawnVehicle(instanceID, ref citizenData, position);                                                                                                           //flag2 is set to the success of spawning a vehicle
                }
                if (!flag2)                                                                                                                                                                 //if flag2 is false
                {
                    citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;                                                                                                           //set flag TryingSpawnVehicle is false
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;                                                                                                               //set flag Bored of Waiting to false
                    citizenData.m_waitCounter = (byte)0;                                                                                                                                        //reset Wait counter
                    this.InvalidPath(instanceID, ref citizenData);                                                                                                                              //report an invalid path
                }
            }
            else if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTransport) != CitizenInstance.Flags.None)                                                                      //else if flag WaitingTransport is true
            {
                bool flag2 = true;                                                                                                                                                          //flag2 is true
                if ((int)citizenData.m_waitCounter < (int)byte.MaxValue)                                                                                                                    //if wait counter is less than 255
                {
                    if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)                                                                                                      //have a 33%-50% chance of in
                        ++citizenData.m_waitCounter;
                }
                else if ((citizenData.m_flags & CitizenInstance.Flags.BoredOfWaiting) == CitizenInstance.Flags.None)                                                                        //else if flag BoredofWaiting is false
                {
                    citizenData.m_flags |= CitizenInstance.Flags.BoredOfWaiting;                                                                                                                //set flag BoredOfWaiting to true
                    citizenData.m_waitCounter = (byte)0;                                                                                                                                        //reset wait counter
                }                                                                                                                                                                           //else (wait counter exceeded AND bored of waiting is true)
                else
                {
                    citizenData.m_flags &= ~CitizenInstance.Flags.WaitingTransport;                                                                                                             //flag WaitingTransport is false
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;                                                                                                               //flag BoredOfWaiting is false
                    citizenData.m_flags |= CitizenInstance.Flags.CannotUseTransport;                                                                                                            //flag CannotUseTransport is true
                    citizenData.m_waitCounter = (byte)0;                                                                                                                                        //reset wait counter
                    flag2 = false;                                                                                                                                                              //flag2 is false
                    this.InvalidPath(instanceID, ref citizenData);                                                                                                                              //report invalid path
                }
                if (flag2 && (double)f < (double)minSqrDistance)                                                                                                                            //if flag2 is true and f (physics) < minSqrDistance
                {
                    if ((long)(num1 >> 4 & 7U) == (long)((int)instanceID & 7))                                                                                                                  //if num1 (currentframeindex) is equal to the instanceID ???
                        citizenData.m_targetPos = this.GetTransportWaitPosition(instanceID, ref citizenData, ref frameData, minSqrDistance);                                                        //set the target position to GetTransportWaitPosition **
                    vector3_1 = (Vector3)citizenData.m_targetPos - frameData.m_position;                                                                                                        //vector3_1 = target position - frame position (current position?)
                    f = lodPhysics || (double)citizenData.m_targetPos.w <= 1.0 / 1000.0 ? vector3_1.sqrMagnitude : VectorUtils.LengthSqrXZ(vector3_1);                                          //something with physics
                }
            }
            else if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTaxi) != CitizenInstance.Flags.None)                                                                           //else if flag WaitingTaxi is true
            {
                bool flag2 = false;                                                                                                                                                         //flag2 is false
                if ((int)citizenData.m_citizen != 0)                                                                                                                                        //if citizen is valid
                {
                    flag2 = (int)Singleton<CitizenManager>.instance.m_citizens.m_buffer[(long)citizenData.m_citizen].m_vehicle != 0;                                                            //flag2 is the validity of the citizen's vehicle
                    if (!flag2 && (long)(num1 >> 4 & 15U) == (long)((int)instanceID & 15))                                                                                                       //if vehicle is valid and if frame instance is equal to the citizen instance
                        Singleton<TransferManager>.instance.AddIncomingOffer(TransferManager.TransferReason.Taxi, new TransferManager.TransferOffer()                                               //AddIncomingOFfer(reason: taxi)
                        {
                            Priority = 7,                                                                                                                                                               //Sets attributes of transfer offer
                            Citizen = citizenData.m_citizen,
                            Position = frameData.m_position,
                            Amount = 1,
                            Active = false
                        });
                }
                if ((int)citizenData.m_waitCounter < (int)byte.MaxValue)                                                                                                                    //if wait counter isn't exceeded
                {
                    if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)                                                                                                      //33%-50% chance of incrementing counter
                        ++citizenData.m_waitCounter;
                }
                else if ((citizenData.m_flags & CitizenInstance.Flags.BoredOfWaiting) == CitizenInstance.Flags.None)                                                                        //else if flag BoredOfWaiting is false
                {
                    citizenData.m_flags |= CitizenInstance.Flags.BoredOfWaiting;                                                                                                                //flag BoredOfWaiting is true
                    citizenData.m_waitCounter = (byte)0;                                                                                                                                        //reset wait counter
                }
                else if (!flag2)                                                                                                                                                            //else if flag2 is false
                {
                    citizenData.m_flags &= ~CitizenInstance.Flags.WaitingTaxi;                                                                                                                  //flag WaitingTaxi is false
                    citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;                                                                                                               //flag BoredOfWaiting is false
                    citizenData.m_flags |= CitizenInstance.Flags.CannotUseTaxi;                                                                                                                 //flag CannotUseTaxi is true
                    citizenData.m_waitCounter = (byte)0;                                                                                                                                        //reset wait counter
                    this.InvalidPath(instanceID, ref citizenData);                                                                                                                              //report invalid path
                }
            }
            else if ((citizenData.m_flags & CitizenInstance.Flags.EnteringVehicle) != CitizenInstance.Flags.None)                                                                       //else if flag EnteringVehicle is true
            {
                if ((double)f < (double)minSqrDistance)                                                                                                                                     //if f (physics) is less than minSqrDistance
                {
                    citizenData.m_targetPos = this.GetVehicleEnterPosition(instanceID, ref citizenData, minSqrDistance);                                                                        //set target position to GetVechileEnterPosition (citizen instance, minSqrDistance
                    vector3_1 = (Vector3)citizenData.m_targetPos - frameData.m_position;                                                                                                        //vector3_1 is target position - frame (instance?) position
                    f = lodPhysics || (double)citizenData.m_targetPos.w <= 1.0 / 1000.0 ? vector3_1.sqrMagnitude : VectorUtils.LengthSqrXZ(vector3_1);                                          //something with physics
                }
            }
            else if ((double)f < (double)minSqrDistance)                                                                                                                                //else if f (physics) is less than minSqrDistance
            {
                if ((int)citizenData.m_path != 0)                                                                                                                                           //if citizen has a path
                {
                    if ((citizenData.m_flags & CitizenInstance.Flags.WaitingPath) == CitizenInstance.Flags.None)                                                                                //if flag WaitingPathis false
                    {
                        citizenData.m_targetPos = this.GetPathTargetPosition(instanceID, ref citizenData, ref frameData, minSqrDistance);                                                           //target position = GetPathTargetPosition (ID, citizen instance, minSqrDistance
                        if ((citizenData.m_flags & CitizenInstance.Flags.OnPath) == CitizenInstance.Flags.None)                                                                                     //if flag OnPath is false
                            citizenData.m_targetPos.w = 1f;                                                                                                                                             //target position's w = 1
                    }
                }
                else
                {                                                                                                                                                                           //else (if citizen lacks a path)
                    if ((citizenData.m_flags & CitizenInstance.Flags.RidingBicycle) != CitizenInstance.Flags.None)                                                                              //if flag RidingBicycle is true
                    {
                        if ((int)citizenData.m_citizen != 0)                                                                                                                                        //if citizen is valid
                            Singleton<CitizenManager>.instance.m_citizens.m_buffer[(long)citizenData.m_citizen].SetVehicle(citizenData.m_citizen, (ushort)0, 0U);                                       //set this citizen's vehicle to 0
                        citizenData.m_flags &= ~CitizenInstance.Flags.RidingBicycle;                                                                                                                //flag RidingBicycle is false
                    }
                    citizenData.m_flags &= ~(CitizenInstance.Flags.OnPath | CitizenInstance.Flags.OnBikeLane);                                                                                  //??? probably means that both flags OnPath and OnBikeLane are set to false?
                    if ((int)citizenData.m_targetBuilding != 0 && ((citizenData.m_flags & CitizenInstance.Flags.AtTarget) == CitizenInstance.Flags.None || (long)(num1 >> 4 & 15U) == (long)((int)instanceID & 15)))
                        //if target building is valid and if (flag AtTarget is false or if some condition comparing frame instance and citizen ID is met)
                        this.GetBuildingTargetPosition(instanceID, ref citizenData, minSqrDistance);                                                                                                //GetBuildingTargetPosition (iD, citizen instanec and minSqrDistance)
                    if ((citizenData.m_flags & CitizenInstance.Flags.Panicking) == CitizenInstance.Flags.None)                                                                                  //if flag Panicking is false
                        flag1 = true;                                                                                                                                                               //flag1 is true
                }
                vector3_1 = (Vector3)citizenData.m_targetPos - frameData.m_position;                                                                                                            //Vector3_1 = tgarget position - frame (instance?) position
                f = lodPhysics || (double)citizenData.m_targetPos.w <= 1.0 / 1000.0 ? vector3_1.sqrMagnitude : VectorUtils.LengthSqrXZ(vector3_1);                                              //physics command
            }
            float num2 = this.m_info.m_walkSpeed;                                                                                                                                               //num2 = walk speed
            float b = 2f;                                                                                                                                                                       //b = 2
            if ((citizenData.m_flags & CitizenInstance.Flags.HangAround) != CitizenInstance.Flags.None)                                                                                         //if flag HangAround is true
                num2 = Mathf.Max(num2 * 0.5f, 1f);                                                                                                                                                  //num2 = max(num2 / 2 , 1)
            else if ((citizenData.m_flags & CitizenInstance.Flags.RidingBicycle) != CitizenInstance.Flags.None)                                                                                 //else if flag RidingBicycle is true (meaning that flag HangAround is false)
            {
                if ((citizenData.m_flags & CitizenInstance.Flags.OnBikeLane) != CitizenInstance.Flags.None)                                                                                         //if flag OnBikeLane is true
                    num2 *= 2f;                                                                                                                                                                         //num2 *=
                else
                    num2 *= 1.5f;
                //else
                //num2 *= 1.5
            }
            if ((double)sqrMagnitude1 > 0.00999999977648258)                                                                                                                                        //if sqrMagnitude1 > 0.999999 (could mean >= 1 or a loss of precision (>1)
                frameData.m_position += frameData.m_velocity * 0.5f;                                                                                                                                    //position += velocity / 2
            Vector3 vector3_2;
            if ((double)f < 1.0)                                                                                                                                                                    //
            {
                vector3_2 = Vector3.zero;
                if ((citizenData.m_flags & CitizenInstance.Flags.EnteringVehicle) != CitizenInstance.Flags.None)
                {
                    if (this.EnterVehicle(instanceID, ref citizenData))
                        return;
                }
                else if (flag1)
                {
                    if (this.ArriveAtTarget(instanceID, ref citizenData))
                        return;
                    citizenData.m_flags |= CitizenInstance.Flags.AtTarget;
                    if (Singleton<SimulationManager>.instance.m_randomizer.Int32(256U) == 0)
                        citizenData.m_targetSeed = (byte)Singleton<SimulationManager>.instance.m_randomizer.Int32(256U);
                }
                else
                    citizenData.m_flags &= ~CitizenInstance.Flags.AtTarget;
            }
            else
            {
                float num3 = Mathf.Sqrt(f);
                float num4 = Mathf.Sqrt(sqrMagnitude1);
                float num5 = Mathf.Max(0.0f, Vector3.Dot(vector3_1, frameData.m_velocity) / Mathf.Max(1f, num4 * num3));
                float num6 = Mathf.Max(0.5f, num2 * num5 * num5 * num5);
                vector3_2 = vector3_1 * Mathf.Min(0.577f, num6 / num3);
                citizenData.m_flags &= ~CitizenInstance.Flags.AtTarget;
                if ((citizenData.m_flags & CitizenInstance.Flags.RequireSlowStart) != CitizenInstance.Flags.None && (int)citizenData.m_waitCounter < 8)
                {
                    ++citizenData.m_waitCounter;
                    frameData.m_velocity = Vector3.zero;
                    return;
                }
            }
            frameData.m_underground = (citizenData.m_flags & CitizenInstance.Flags.Underground) != CitizenInstance.Flags.None;
            frameData.m_insideBuilding = (citizenData.m_flags & CitizenInstance.Flags.InsideBuilding) != CitizenInstance.Flags.None;
            frameData.m_transition = (citizenData.m_flags & CitizenInstance.Flags.Transition) != CitizenInstance.Flags.None;
            if ((double)f < 1.0 && flag1 && (citizenData.m_flags & CitizenInstance.Flags.SittingDown) != CitizenInstance.Flags.None)
            {
                citizenData.m_flags |= CitizenInstance.Flags.RequireSlowStart;
                citizenData.m_waitCounter = (byte)0;
                frameData.m_velocity = ((Vector3)citizenData.m_targetPos - frameData.m_position) * 0.5f;
                frameData.m_position += frameData.m_velocity * 0.5f;
                if ((double)citizenData.m_targetDir.sqrMagnitude <= 0.00999999977648258)
                    return;
                frameData.m_rotation = Quaternion.LookRotation(VectorUtils.X_Y(citizenData.m_targetDir));
            }
            else
            {
                citizenData.m_flags &= ~CitizenInstance.Flags.RequireSlowStart;
                Vector3 vector3_3 = vector3_2 - frameData.m_velocity;
                float magnitude = vector3_3.magnitude;
                vector3_3 *= b / Mathf.Max(magnitude, b);
                frameData.m_velocity += vector3_3;
                frameData.m_velocity -= Mathf.Max(0.0f, Vector3.Dot(frameData.m_position + frameData.m_velocity - (Vector3)citizenData.m_targetPos, frameData.m_velocity)) / Mathf.Max(0.01f, frameData.m_velocity.sqrMagnitude) * frameData.m_velocity;
                float sqrMagnitude2 = frameData.m_velocity.sqrMagnitude;
                bool flag2 = !lodPhysics && (double)citizenData.m_targetPos.w > 1.0 / 1000.0 && ((double)sqrMagnitude2 > 0.00999999977648258 || (double)sqrMagnitude1 > 0.00999999977648258);
                ushort buildingID = !flag2 ? (ushort)0 : Singleton<BuildingManager>.instance.GetWalkingBuilding(frameData.m_position + frameData.m_velocity * 0.5f);
                if ((double)sqrMagnitude2 > 0.00999999977648258)
                {
                    if (!lodPhysics)
                    {
                        Vector3 zero = Vector3.zero;
                        float pushDivider = 0.0f;
                        this.CheckCollisions(instanceID, ref citizenData, frameData.m_position, frameData.m_position + frameData.m_velocity, buildingID, ref zero, ref pushDivider);
                        if ((double)pushDivider > 0.00999999977648258)
                        {
                            Vector3 vector3_4 = Vector3.ClampMagnitude(zero * (1f / pushDivider), Mathf.Sqrt(sqrMagnitude2) * 0.9f);
                            frameData.m_velocity += vector3_4;
                        }
                    }
                    frameData.m_position += frameData.m_velocity * 0.5f;
                    Vector3 forward = frameData.m_velocity;
                    if ((citizenData.m_flags & CitizenInstance.Flags.RidingBicycle) == CitizenInstance.Flags.None)
                        forward.y = 0.0f;
                    if ((double)forward.sqrMagnitude > 0.00999999977648258)
                        frameData.m_rotation = Quaternion.LookRotation(forward);
                }
                if (!flag2)
                    return;
                Vector3 worldPos = frameData.m_position;
                float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(worldPos);
                if ((int)buildingID != 0)
                {
                    float num3 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID].SampleWalkingHeight(worldPos, terrainHeight);
                    worldPos.y = worldPos.y + (num3 - worldPos.y) * Mathf.Min(1f, citizenData.m_targetPos.w * 4f);
                    frameData.m_position.y = worldPos.y;
                }
                else
                {
                    if ((double)Mathf.Abs(terrainHeight - worldPos.y) >= 2.0)
                        return;
                    worldPos.y = worldPos.y + (terrainHeight - worldPos.y) * Mathf.Min(1f, citizenData.m_targetPos.w * 4f);
                    frameData.m_position.y = worldPos.y;
                }
            }
        }

        protected override bool CheckSegmentChange(ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position prevPos, PathUnit.Position nextPos, int prevOffset, int nextOffset, Bezier3 bezier)
        {
            NetManager instance = Singleton<NetManager>.instance;
            ushort node = prevOffset >= 128 ? instance.m_segments.m_buffer[(int)prevPos.m_segment].m_endNode : instance.m_segments.m_buffer[(int)prevPos.m_segment].m_startNode;
            ushort num = nextOffset >= 128 ? instance.m_segments.m_buffer[(int)nextPos.m_segment].m_endNode : instance.m_segments.m_buffer[(int)nextPos.m_segment].m_startNode;
            if ((int)node == (int)num && (instance.m_nodes.m_buffer[(int)node].m_flags & NetNode.Flags.TrafficLights) != NetNode.Flags.None)
            {
                Segment3 segment1 = new Segment3(bezier.a, bezier.b);
                Segment3 segment2 = new Segment3(bezier.b, bezier.c);
                Segment3 segment3 = new Segment3(bezier.c, bezier.d);
                Segment3 segment3_1;
                segment3_1.a = instance.m_nodes.m_buffer[(int)node].m_position;
                for (int index = 0; index < 8; ++index)
                {
                    ushort segment4 = instance.m_nodes.m_buffer[(int)node].GetSegment(index);
                    if ((int)segment4 != 0 && (int)segment4 != (int)prevPos.m_segment && (int)segment4 != (int)nextPos.m_segment)
                    {
                        segment3_1.b = (int)instance.m_segments.m_buffer[(int)segment4].m_startNode != (int)node ? segment3_1.a + instance.m_segments.m_buffer[(int)segment4].m_endDirection * 1000f : segment3_1.a + instance.m_segments.m_buffer[(int)segment4].m_startDirection * 1000f;
                        float u;
                        float v;
                        if ((double)segment3_1.DistanceSqr(segment1, out u, out v) < 1.0 || (double)segment3_1.DistanceSqr(segment2, out u, out v) < 1.0 || (double)segment3_1.DistanceSqr(segment3, out u, out v) < 1.0)
                            return this.CheckTrafficLights(node, segment4);
                    }
                }
            }
            return true;
        }

        protected override bool CheckLaneChange(ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position prevPos, PathUnit.Position nextPos, int prevOffset, int nextOffset)
        {
            NetManager instance = Singleton<NetManager>.instance;
            ushort node = prevOffset != 0 ? instance.m_segments.m_buffer[(int)prevPos.m_segment].m_endNode : instance.m_segments.m_buffer[(int)prevPos.m_segment].m_startNode;
            if ((instance.m_nodes.m_buffer[(int)node].m_flags & NetNode.Flags.TrafficLights) != NetNode.Flags.None)
            {
                NetInfo info = instance.m_segments.m_buffer[(int)prevPos.m_segment].Info;
                if (info.m_lanes.Length > (int)prevPos.m_lane && info.m_lanes.Length > (int)nextPos.m_lane)
                {
                    float num1 = info.m_lanes[(int)prevPos.m_lane].m_position;
                    float num2 = info.m_lanes[(int)nextPos.m_lane].m_position;
                    for (int index = 0; index < info.m_lanes.Length; ++index)
                    {
                        if (index != (int)prevPos.m_lane && index != (int)nextPos.m_lane && (info.m_lanes[index].m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != NetInfo.LaneType.None)
                        {
                            float num3 = info.m_lanes[index].m_position;
                            if ((double)num3 > (double)num1 && (double)num3 < (double)num2 || (double)num3 < (double)num1 && (double)num3 > (double)num2)
                                return this.CheckTrafficLights(node, prevPos.m_segment);
                        }
                    }
                }
            }
            return true;
        }

        private bool CheckTrafficLights(ushort node, ushort segment)
        {
            NetManager instance = Singleton<NetManager>.instance;
            uint num1 = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            uint num2 = ((uint)node << 8) / 32768U;
            uint num3 = (uint)((int)num1 - (int)num2 & (int)byte.MaxValue);
            RoadBaseAI.TrafficLightState vehicleLightState;
            RoadBaseAI.TrafficLightState pedestrianLightState;
            bool vehicles;
            bool pedestrians1;
            RoadBaseAI.GetTrafficLightState(node, ref instance.m_segments.m_buffer[(int)segment], num1 - num2, out vehicleLightState, out pedestrianLightState, out vehicles, out pedestrians1);
            switch (pedestrianLightState)
            {
                case RoadBaseAI.TrafficLightState.RedToGreen:
                    if (num3 < 60U)
                        return false;
                    break;

                case RoadBaseAI.TrafficLightState.Red:
                case RoadBaseAI.TrafficLightState.GreenToRed:
                    if (!pedestrians1 && num3 >= 196U)
                    {
                        bool pedestrians2 = true;
                        RoadBaseAI.SetTrafficLightState(node, ref instance.m_segments.m_buffer[(int)segment], num1 - num2, vehicleLightState, pedestrianLightState, vehicles, pedestrians2);
                    }
                    return false;
            }
            return true;
        }

        protected virtual bool EnterVehicle(ushort instanceID, ref CitizenInstance citizenData)
        {
            //Debug.Log(instanceID.ToString() + " entered a vehicle.");
            citizenData.m_flags &= ~CitizenInstance.Flags.EnteringVehicle;
            citizenData.Unspawn(instanceID);
            uint num = citizenData.m_citizen;
            if ((int)num != 0)
            {
                VehicleManager instance = Singleton<VehicleManager>.instance;
                ushort vehicleID = Singleton<CitizenManager>.instance.m_citizens.m_buffer[(long)num].m_vehicle;
                if ((int)vehicleID != 0)
                    vehicleID = instance.m_vehicles.m_buffer[(int)vehicleID].GetFirstVehicle(vehicleID);
                if ((int)vehicleID != 0)
                {
                    VehicleInfo info = instance.m_vehicles.m_buffer[(int)vehicleID].Info;
                    int ticketPrice = info.m_vehicleAI.GetTicketPrice(vehicleID, ref instance.m_vehicles.m_buffer[(int)vehicleID]);
                    if (ticketPrice != 0)
                        Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, ticketPrice, info.m_class);
                }
            }
            return false;
        }

        public override bool TransportArriveAtSource(ushort instanceID, ref CitizenInstance citizenData, Vector3 currentPos, Vector3 nextTarget)
        {
            PathManager instance1 = Singleton<PathManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            PathUnit.Position position1;
            if ((int)citizenData.m_path != 0 && instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position1))
            {
                uint laneId = PathManager.GetLaneID(position1);
                Vector3 position2 = instance2.m_lanes.m_buffer[(long)laneId].CalculatePosition((int)citizenData.m_lastPathOffset < 128 ? 1f : 0.0f);
                Vector3 position3 = instance2.m_lanes.m_buffer[(long)laneId].CalculatePosition((float)citizenData.m_lastPathOffset * 0.003921569f);
                if ((double)Vector3.SqrMagnitude(position2 - currentPos) < 4.0 && (double)Vector3.SqrMagnitude(position3 - nextTarget) < 4.0)
                    return true;
            }
            return false;
        }

        public override bool TransportArriveAtTarget(ushort instanceID, ref CitizenInstance citizenData, Vector3 currentPos, Vector3 nextTarget, ref TransportPassengerData passengerData, bool forceUnload)
        {
            //Debug.Log("Current X: " + currentPos.x + ". Current Z: " + currentPos.z + ". Next X: " + nextTarget.x + ". Next Z: " + nextTarget.z + ". PassengerData: " + passengerData.ToString());

            PathManager instance1 = Singleton<PathManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            CitizenManager instance3 = Singleton<CitizenManager>.instance;
            ushort num1 = 0;
            bool flag = false;
            if ((int)citizenData.m_path != 0)
            {
                PathUnit.Position position;
                if (instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position))
                {
                    uint laneId = PathManager.GetLaneID(position);
                    if ((double)Vector3.SqrMagnitude(instance2.m_lanes.m_buffer[(long)laneId].CalculatePosition((float)position.m_offset * 0.003921569f) - currentPos) >= 4.0)
                        flag = true;
                    num1 = instance2.m_segments.m_buffer[(int)position.m_segment].m_endNode;
                }
                else
                    flag = true;
            }
            if ((int)citizenData.m_path != 0)
            {
                citizenData.m_pathPositionIndex += (byte)2;
                if ((int)citizenData.m_pathPositionIndex >> 1 >= (int)instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].m_positionCount)
                {
                    instance1.ReleaseFirstUnit(ref citizenData.m_path);
                    citizenData.m_pathPositionIndex = (byte)0;
                }
            }
            if ((int)citizenData.m_path != 0)
            {
                PathUnit.Position position;
                if (instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position))
                {
                    citizenData.m_lastPathOffset = position.m_offset;
                    uint laneId = PathManager.GetLaneID(position);
                    if ((double)Vector3.SqrMagnitude(instance2.m_lanes.m_buffer[(long)laneId].CalculatePosition((float)citizenData.m_lastPathOffset * 0.003921569f) - nextTarget) < 4.0)
                    {
                        if (!forceUnload)
                            return false;
                        flag = true;
                    }
                    else if ((int)instance2.m_nodes.m_buffer[(int)num1].m_lane != (int)laneId)
                        flag = true;
                }
                else
                    flag = true;
            }
            citizenData.m_targetPos = (Vector4)currentPos;
            uint num2 = citizenData.m_citizen;
            if ((int)num2 != 0)
            {
                if ((instance3.m_citizens.m_buffer[(long)num2].m_flags & Citizen.Flags.Tourist) != Citizen.Flags.None)
                    ++passengerData.m_touristPassengers.m_tempCount;
                else
                    ++passengerData.m_residentPassengers.m_tempCount;
                VehicleInfo vehicleInfo = this.GetVehicleInfo(instanceID, ref citizenData, false);
                if (vehicleInfo != null && vehicleInfo.m_vehicleType != VehicleInfo.VehicleType.Bicycle)
                    ++passengerData.m_carOwningPassengers.m_tempCount;
                switch (Citizen.GetAgeGroup(instance3.m_citizens.m_buffer[(long)num2].Age))
                {
                    case Citizen.AgeGroup.Child:
                        ++passengerData.m_childPassengers.m_tempCount;
                        break;

                    case Citizen.AgeGroup.Teen:
                        ++passengerData.m_teenPassengers.m_tempCount;
                        break;

                    case Citizen.AgeGroup.Young:
                        ++passengerData.m_youngPassengers.m_tempCount;
                        break;

                    case Citizen.AgeGroup.Adult:
                        ++passengerData.m_adultPassengers.m_tempCount;
                        break;

                    case Citizen.AgeGroup.Senior:
                        ++passengerData.m_seniorPassengers.m_tempCount;
                        break;
                }
            }
            if (flag && (int)citizenData.m_path != 0)
            {
                Singleton<PathManager>.instance.ReleasePath(citizenData.m_path);
                citizenData.m_path = 0U;
            }
            return true;
        }

        public override bool SetCurrentVehicle(ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, uint unitID, Vector3 position)
        {
            uint citizenID = citizenData.m_citizen;
            if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTransport) != CitizenInstance.Flags.None)
            {
                if ((int)vehicleID == 0 && (int)unitID == 0)
                    return true;
                CitizenManager instance = Singleton<CitizenManager>.instance;
                if ((int)citizenID != 0)
                {
                    instance.m_citizens.m_buffer[(long)citizenID].SetVehicle(citizenID, vehicleID, unitID);
                    if ((int)instance.m_citizens.m_buffer[(long)citizenID].m_vehicle == 0)
                        return false;
                    citizenData.m_flags &= ~CitizenInstance.Flags.WaitingTransport;
                    citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                    return true;
                }
                citizenData.m_flags &= ~CitizenInstance.Flags.WaitingTransport;
                citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
                return false;
            }
            if ((int)vehicleID != 0)
                return false;
            CitizenManager instance1 = Singleton<CitizenManager>.instance;
            ushort num = 0;
            if ((int)citizenID != 0)
            {
                num = instance1.m_citizens.m_buffer[(long)citizenID].m_vehicle;
                instance1.m_citizens.m_buffer[(long)citizenID].SetVehicle(citizenID, (ushort)0, 0U);
            }
            if ((int)num != 0)
            {
                Vector3 pos;
                if (this.GetNextTargetPosition(instanceID, ref citizenData, Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int)num].GetLastFramePosition(), out pos))
                {
                    position = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int)num].GetClosestDoorPosition(pos, VehicleInfo.DoorType.Exit);
                }
                else
                {
                    Randomizer r = new Randomizer((int)instanceID ^ (int)num);
                    position = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int)num].GetRandomDoorPosition(ref r, VehicleInfo.DoorType.Exit);
                }
            }
            citizenData.Unspawn(instanceID);
            citizenData.m_targetPos = (Vector4)position;
            citizenData.m_frame0.m_velocity = Vector3.zero;
            citizenData.m_frame0.m_position = position;
            citizenData.m_frame0.m_rotation = Quaternion.identity;
            citizenData.m_frame1 = citizenData.m_frame0;
            citizenData.m_frame2 = citizenData.m_frame0;
            citizenData.m_frame3 = citizenData.m_frame0;
            if ((int)citizenData.m_path != 0 && (citizenData.m_flags & CitizenInstance.Flags.WaitingPath) == CitizenInstance.Flags.None)
            {
                if (Singleton<PathManager>.instance.m_pathUnits.m_buffer[(long)citizenData.m_path].CalculatePathPositionOffset((int)citizenData.m_pathPositionIndex >> 1, position, out citizenData.m_lastPathOffset))
                    this.Spawn(instanceID, ref citizenData);
            }
            else if (this.StartPathFind(instanceID, ref citizenData))
                this.Spawn(instanceID, ref citizenData);
            return true;
        }

        private bool GetNextTargetPosition(ushort instanceID, ref CitizenInstance citizenData, Vector3 refPos, out Vector3 pos)
        {
            PathManager instance1 = Singleton<PathManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            PathUnit.Position position;
            if ((int)citizenData.m_path != 0 && instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position))
            {
                uint laneId = PathManager.GetLaneID(position);
                if ((int)laneId != 0)
                {
                    NetInfo info = instance2.m_segments.m_buffer[(int)position.m_segment].Info;
                    if (info.m_lanes != null && info.m_lanes.Length > (int)position.m_lane && info.m_lanes[(int)position.m_lane].m_laneType == NetInfo.LaneType.Pedestrian)
                    {
                        float laneOffset;
                        instance2.m_lanes.m_buffer[(long)laneId].GetClosestPosition(refPos, out pos, out laneOffset);
                        return true;
                    }
                }
                ushort num1 = instance2.m_segments.m_buffer[(int)position.m_segment].m_startNode;
                Vector3 point = instance2.m_nodes.m_buffer[(int)num1].m_position;
                uint num2 = instance2.m_nodes.m_buffer[(int)num1].m_lane;
                if ((int)num2 != 0)
                {
                    uint num3 = (uint)instance2.m_lanes.m_buffer[(long)num2].m_segment;
                    uint laneID;
                    int laneIndex;
                    float laneOffset;
                    if (instance2.m_segments.m_buffer[(long)num3].GetClosestLanePosition(point, NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, out pos, out laneID, out laneIndex, out laneOffset))
                        return true;
                }
                pos = point;
                return true;
            }
            pos = Vector3.zero;
            return false;
        }

        private Vector4 GetTransportWaitPosition(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, float minSqrDistance)
        {
            PathManager instance1 = Singleton<PathManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            PathUnit.Position position1;
            if (!instance1.m_pathUnits.m_buffer[(long)citizenData.m_path].GetPosition((int)citizenData.m_pathPositionIndex >> 1, out position1))
            {
                this.InvalidPath(instanceID, ref citizenData);
                return citizenData.m_targetPos;
            }
            ushort num1 = instance2.m_segments.m_buffer[(int)position1.m_segment].m_startNode;
            if ((citizenData.m_flags & CitizenInstance.Flags.BoredOfWaiting) != CitizenInstance.Flags.None)
                instance2.m_nodes.m_buffer[(int)num1].m_maxWaitTime = byte.MaxValue;
            else if ((int)citizenData.m_waitCounter > (int)instance2.m_nodes.m_buffer[(int)num1].m_maxWaitTime)
                instance2.m_nodes.m_buffer[(int)num1].m_maxWaitTime = citizenData.m_waitCounter;
            uint laneID = instance2.m_nodes.m_buffer[(int)num1].m_lane;
            if ((int)laneID == 0)
                return citizenData.m_targetPos;
            uint num2 = (uint)instance2.m_lanes.m_buffer[(long)laneID].m_segment;
            NetInfo.Lane laneInfo;
            if (!instance2.m_segments.m_buffer[(long)num2].GetClosestLane(laneID, NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, out laneID, out laneInfo))
                return citizenData.m_targetPos;
            ushort num3 = instance2.m_segments.m_buffer[(long)num2].m_startNode;
            ushort num4 = instance2.m_segments.m_buffer[(long)num2].m_endNode;
            if (((instance2.m_nodes.m_buffer[(int)num3].m_flags | instance2.m_nodes.m_buffer[(int)num4].m_flags) & NetNode.Flags.Disabled) != NetNode.Flags.None)
                citizenData.m_waitCounter = byte.MaxValue;
            Randomizer randomizer = new Randomizer((uint)instanceID | laneID << 16);
            float num5 = instance2.m_nodes.m_buffer[(int)num1].Info.m_netAI.MaxTransportWaitDistance();
            int num6 = (int)instance2.m_nodes.m_buffer[(int)num1].m_laneOffset << 8;
            int @int = Mathf.RoundToInt(num5 * 65280f / Mathf.Max(1f, instance2.m_lanes.m_buffer[(long)laneID].m_length));
            int min = Mathf.Clamp(num6 - @int, 0, 65280);
            int max = Mathf.Clamp(num6 + @int, 0, 65280);
            int num7 = randomizer.Int32(min, max);
            Vector3 position2;
            Vector3 direction;
            instance2.m_lanes.m_buffer[(long)laneID].CalculatePositionAndDirection((float)num7 * 1.531863E-05f, out position2, out direction);
            float num8 = (float)((double)Mathf.Max(0.0f, laneInfo.m_width - 1f) * (double)randomizer.Int32(-500, 500) * (1.0 / 1000.0));
            position2 += Vector3.Cross(Vector3.up, direction).normalized * num8;
            return new Vector4(position2.x, position2.y, position2.z, 0.0f);
        }

        protected virtual Vector4 GetVehicleEnterPosition(ushort instanceID, ref CitizenInstance citizenData, float minSqrDistance)
        {
            //Debug.Log("Citizen " + instanceID + " entered a vehicle with a minimum square distance of " + minSqrDistance + ".");
            CitizenManager instance1 = Singleton<CitizenManager>.instance;
            VehicleManager instance2 = Singleton<VehicleManager>.instance;
            uint num1 = citizenData.m_citizen;
            if ((int)num1 != 0)
            {
                ushort num2 = instance1.m_citizens.m_buffer[(long)num1].m_vehicle;
                if ((int)num2 != 0)
                {
                    Vector4 vector4 = (Vector4)instance2.m_vehicles.m_buffer[(int)num2].GetClosestDoorPosition((Vector3)citizenData.m_targetPos, VehicleInfo.DoorType.Enter);
                    vector4.w = citizenData.m_targetPos.w;
                    return vector4;
                }
            }
            return citizenData.m_targetPos;
        }

        protected virtual void GetBuildingTargetPosition(ushort instanceID, ref CitizenInstance citizenData, float minSqrDistance)
        {
            //Debug.Log("Citizen " + instanceID + " is headed toward a building that is " + minSqrDistance + " units away.");
            if ((int)citizenData.m_targetBuilding != 0)
            {
                BuildingManager instance = Singleton<BuildingManager>.instance;
                if ((int)instance.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].m_fireIntensity != 0)
                {
                    citizenData.m_flags |= CitizenInstance.Flags.Panicking;
                    citizenData.m_targetDir = Vector2.zero;
                }
                else
                {
                    BuildingInfo info = instance.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].Info;
                    Randomizer randomizer = new Randomizer((int)instanceID << 8 | (int)citizenData.m_targetSeed);
                    Vector3 position;
                    Vector3 target;
                    Vector2 direction;
                    CitizenInstance.Flags specialFlags;
                    info.m_buildingAI.CalculateUnspawnPosition(citizenData.m_targetBuilding, ref instance.m_buildings.m_buffer[(int)citizenData.m_targetBuilding], ref randomizer, this.m_info, instanceID, out position, out target, out direction, out specialFlags);
                    citizenData.m_flags = citizenData.m_flags & ~CitizenInstance.Flags.TargetFlags | specialFlags;
                    citizenData.m_targetPos = new Vector4(position.x, position.y, position.z, 1f);
                    citizenData.m_targetDir = direction;
                }
            }
            else
            {
                citizenData.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                citizenData.m_targetDir = Vector2.zero;
            }
        }

        protected virtual bool ArriveAtTarget(ushort instanceID, ref CitizenInstance citizenData)
        {
            //Debug.Log("Citizen " + instanceID + " arrived at their target.");
            if ((citizenData.m_flags & CitizenInstance.Flags.HangAround) != CitizenInstance.Flags.None)
            {
                uint num = citizenData.m_citizen;
                if ((int)num != 0)
                {
                    CitizenManager instance = Singleton<CitizenManager>.instance;
                    if (instance.m_citizens.m_buffer[(long)num].CurrentLocation == Citizen.Location.Moving)
                        this.ArriveAtDestination(instanceID, ref citizenData, true);
                    if ((int)instance.m_citizens.m_buffer[(long)num].GetBuildingByLocation() == (int)citizenData.m_targetBuilding)
                        return false;
                }
                citizenData.m_flags &= ~CitizenInstance.Flags.TargetFlags;
                citizenData.Unspawn(instanceID);
            }
            else
                this.ArriveAtDestination(instanceID, ref citizenData, true);
            return true;
        }

        protected virtual void ArriveAtDestination(ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            //Debug.Log("Citizen " + instanceID + " arrived at their destination.");
            uint citizenID = citizenData.m_citizen;
            if ((int)citizenID != 0)
            {
                CitizenManager instance1 = Singleton<CitizenManager>.instance;
                instance1.m_citizens.m_buffer[(long)citizenID].SetVehicle(citizenID, (ushort)0, 0U);
                if (success)
                    instance1.m_citizens.m_buffer[(long)citizenID].SetLocationByBuilding(citizenID, citizenData.m_targetBuilding);
                if ((int)citizenData.m_targetBuilding != 0 && instance1.m_citizens.m_buffer[(long)citizenID].CurrentLocation == Citizen.Location.Visit)
                {
                    BuildingManager instance2 = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance2.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].Info;
                    int amountDelta = -100;
                    info.m_buildingAI.ModifyMaterialBuffer(citizenData.m_targetBuilding, ref instance2.m_buildings.m_buffer[(int)citizenData.m_targetBuilding], TransferManager.TransferReason.Shopping, ref amountDelta);
                    ushort eventID = instance2.m_buildings.m_buffer[(int)citizenData.m_targetBuilding].m_eventIndex;
                    if ((int)eventID != 0)
                    {
                        EventManager instance3 = Singleton<EventManager>.instance;
                        instance3.m_events.m_buffer[(int)eventID].Info.m_eventAI.VisitorEnter(eventID, ref instance3.m_events.m_buffer[(int)eventID], citizenID);
                    }
                }
            }
            if ((citizenData.m_flags & CitizenInstance.Flags.HangAround) != CitizenInstance.Flags.None && success)
                return;
            this.SetSource(instanceID, ref citizenData, (ushort)0);
            this.SetTarget(instanceID, ref citizenData, (ushort)0);
            citizenData.Unspawn(instanceID);
        }

        protected virtual VehicleInfo GetVehicleInfo(ushort instanceID, ref CitizenInstance citizenData, bool forceProbability)
        {
            return (VehicleInfo)null;
        }
    }
}