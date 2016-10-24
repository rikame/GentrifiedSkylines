using ColossalFramework;
using System;
using UnityEngine;

namespace GentrifiedSkylines.Detours
{
    public class Trip
    {
        private bool activated;
        private CitizenInstance m_citizen;
        private byte m_current;
        private CitizenInstance.Flags m_flags;
        private byte m_legCount;
        private Leg[] m_legs;
        private ushort m_source;
        private ushort m_target;
        private short tentativeCount = 0;
        private bool tentativeNewLeg = false;
        private bool tentativeEndLeg = false;
        private bool tentativeEndTrip = false;
        private bool legActivated = false;
        private bool waitingActivated = false;
        private bool invalid = false;
        private Leg.Flags tentativeLeg = Leg.Flags.None;
        private Leg.Flags currentVehicleType;
        private Leg.Flags previousVehicleType = Leg.Flags.None;

        public Trip(CitizenInstance citizen, ushort source, ushort target)               //Default Constructor
        {
            m_legCount = 0;
            activated = false;
            m_legs = new Leg[0];
            m_source = source;
            m_target = target;
            m_citizen = citizen;
            m_flags = citizen.m_flags;
        }

        public void AddLeg(Leg.Flags mode, Vector3 vec)          //Attempt to add a Leg to the trip
        {
            //Activate if not yet activated
            if (activated)
            {
                CloseCurrentLeg();
            }
            else
            {
                activated = true;
            }
            //Cycle Leg array to a new array of size n+1
            byte oldLength = Convert.ToByte(Mathf.Clamp(m_legs.Length, 0, byte.MaxValue - 1));
            byte newLength = Convert.ToByte(oldLength + 1);
            Leg[] temp = new Leg[oldLength + 1];
            for (byte i = 1; i <= oldLength; i++)
            {
                temp[i - 1] = m_legs[i - 1];
            }
            temp[newLength] = new Leg(vec);
            m_legs = temp;
            m_legCount += 1;
            //Set current, uses this order to avoid null/range exceptions
            m_current = (byte)(m_legCount - 1);
        }

        public void CloseCurrentLeg()
        {
            //Close
        }

        public CitizenInstance GetCitizen()
        {
            return m_citizen;
        }

        public float GetContribution()              //Calculate and return the overall contribution of the trip
        {
            return 7;
        }

        public byte GetLegCount()                   //Get the number of legs
        {
            return m_legCount;
        }

        public Leg[] GetLegs()                      //Retrive Leg Array
        {
            if (activated)
            {
                return m_legs;
            }
            else
            {
                return new Leg[0];
            }
        }

        public ushort GetSource()                   //Return the arbitrary ID of the source of the trip
        {
            return m_source;
        }

        public Building GetSourceBuilding()
        {
            return Singleton<BuildingManager>.instance.m_buildings.m_buffer[m_source];
        }

        public Vector3 GetSourcePosition()
        {
            return GetSourceBuilding().m_position;
        }

        public ushort GetTarget()                   //Return the arbitrary ID of the target of the trip
        {
            return m_target;
        }

        public Building GetTargetBuilding()
        {
            return Singleton<BuildingManager>.instance.m_buildings.m_buffer[m_target];
        }

        public Vector3 GetTargetPosition()
        {
            return GetSourceBuilding().m_position;
        }

        public void SetSource(ushort s)             //Set the arbitrary ID of the source of the trip
        {
            m_source = s;
        }

        public void SetTarget(ushort t)             //Set the arbitrary ID of the target of the trip
        {
            m_target = t;
        }

        public void FinalizeLeg(Vector3 vec)
        {
            m_legs[m_current].finalize(vec);
        }

        public void TryAddLeg(CitizenInstance.Flags t_flags)        //Checks to see if a new leg should be added. If not then
        {
            if (CheckMoving())
            {
                if (CheckNewVehicle())
                {
                    AddLeg(currentVehicleType, GetMyPosition());
                    previousVehicleType = currentVehicleType;
                }
                CheckFlags(t_flags);
                MediateMode(t_flags);
            }
        }

        private bool CheckMoving()
        {
            if (GetMyCitizen().CurrentLocation == Citizen.Location.Moving)
                return true;
            return false;
        }
        public bool IsInvalid()
        {
            return invalid;
        }
        private bool CheckNewVehicle()
        {
            bool found = false;

            if (GetMyVehicleSafety())
            {
                VehicleInfo.VehicleType t_mode = GetVehicleType();
                if (t_mode == VehicleInfo.VehicleType.Bicycle)
                {
                    currentVehicleType = Leg.Flags.Bicycle;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Car)
                {
                    currentVehicleType = Leg.Flags.Car;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Metro)
                {
                    currentVehicleType = Leg.Flags.Metro;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Plane)
                {
                    currentVehicleType = Leg.Flags.Plane;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Ship)
                {
                    currentVehicleType = Leg.Flags.Ship;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Train)
                {
                    currentVehicleType = Leg.Flags.Train;
                    found = true;
                }
                if (t_mode == VehicleInfo.VehicleType.Tram)
                {
                    currentVehicleType = Leg.Flags.Tram;
                    found = true;
                }
                ItemClass.SubService t_subservice = GetMyVehicle().Info.m_class.m_subService;
                if (t_subservice == ItemClass.SubService.PublicTransportBus)
                {
                    currentVehicleType = Leg.Flags.Bus;
                    found = true;
                }
                if (t_subservice == ItemClass.SubService.PublicTransportTaxi)
                {
                    currentVehicleType = Leg.Flags.Taxi;
                    found = true;
                }
                ItemClass.Service t_service = GetMyVehicle().Info.m_class.m_service;
                if (t_service != ItemClass.Service.None)
                {
                    this.Invalidate();                          //Trip is taking place in a special case and is excluded from consideration with regard to accessibility.
                }
                if (!found)                                     //Citizen is moving but without any detectable vehicle. Walking is assumed.
                    currentVehicleType = Leg.Flags.Walk;
                if (currentVehicleType != previousVehicleType)
                    return true;
                return false;
            }
            currentVehicleType = Leg.Flags.Walk;                //Repeated code block. If citizen is moving with an invalid vehicle then walking is assumed.
            if (currentVehicleType != previousVehicleType)
                return true;
            return false;
        }
        private void Invalidate()
        {
            invalid = true;
        }
        private void CheckFlags(CitizenInstance.Flags t_flags)
        {
            tentativeCount = 0;
            tentativeNewLeg = false;
            tentativeEndLeg = false;
            tentativeEndTrip = false;
            legActivated = false;
            waitingActivated = false;
            tentativeLeg = Leg.Flags.None;

            if (m_flags != t_flags)                                                                                             //If Flags have been updated
            {                                                                                                                   //then evaluate if a new trip must be added
                if ((m_flags & CitizenInstance.Flags.Underground) != (t_flags & CitizenInstance.Flags.Underground))
                {
                    if ((m_flags & CitizenInstance.Flags.Underground) != CitizenInstance.Flags.None)
                    //Start Underground
                    {
                    }
                    else
                    //Cease Underground
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.BorrowCar) != (t_flags & CitizenInstance.Flags.BorrowCar))
                {
                    if ((m_flags & CitizenInstance.Flags.BorrowCar) != CitizenInstance.Flags.None)
                    //Start BorrowCar
                    {
                    }
                    else
                    //Cease BrorrowCar
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.EnteringVehicle) != (t_flags & CitizenInstance.Flags.EnteringVehicle))
                {
                    if ((m_flags & CitizenInstance.Flags.EnteringVehicle) != CitizenInstance.Flags.None)
                    //Start EnteringVehicle
                    {
                    }
                    else
                    //Cease EnteringVehicle
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.RidingBicycle) != (t_flags & CitizenInstance.Flags.RidingBicycle))
                {
                    if ((m_flags & CitizenInstance.Flags.RidingBicycle) != CitizenInstance.Flags.None)
                    //Start RidingBicycle
                    {
                    }
                    else
                    //Cease RidingBicycle
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.OnBikeLane) != (t_flags & CitizenInstance.Flags.OnBikeLane))
                {
                    if ((m_flags & CitizenInstance.Flags.OnBikeLane) != CitizenInstance.Flags.None)
                    //Start OnBikeLane
                    {
                    }
                    else
                    //Cease OnBikeLane
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.OnPath) != (t_flags & CitizenInstance.Flags.OnPath))
                {
                    if ((m_flags & CitizenInstance.Flags.OnPath) != CitizenInstance.Flags.None)
                    //Start OnPath
                    {
                    }
                    else
                    //End OnPath
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.SittingDown) != (t_flags & CitizenInstance.Flags.SittingDown))
                {
                    if ((m_flags & CitizenInstance.Flags.SittingDown) != CitizenInstance.Flags.SittingDown)
                    //Start SittingDown
                    {
                    }
                    else
                    //Cease SittingDown
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.TryingSpawnVehicle) != (t_flags & CitizenInstance.Flags.TryingSpawnVehicle))
                {
                }
                if ((m_flags & CitizenInstance.Flags.WaitingPath) != (t_flags & CitizenInstance.Flags.WaitingPath))
                {
                }
                if ((m_flags & CitizenInstance.Flags.WaitingTransport) != (t_flags & CitizenInstance.Flags.WaitingTransport))
                {
                }
                if ((m_flags & CitizenInstance.Flags.WaitingTaxi) != (t_flags & CitizenInstance.Flags.WaitingTaxi))
                {
                    if ((m_flags & CitizenInstance.Flags.WaitingTaxi) != CitizenInstance.Flags.None)
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.BoredOfWaiting) != (t_flags & CitizenInstance.Flags.BoredOfWaiting))
                {
                    if ((m_flags & CitizenInstance.Flags.BoredOfWaiting) != CitizenInstance.Flags.None)
                    //Start BoredOfWaiting
                    {
                    }
                    else
                    //Cease BoredOfWaiting
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.Panicking) != (t_flags & CitizenInstance.Flags.Panicking))
                {
                }
                if ((m_flags & CitizenInstance.Flags.AtTarget) != (t_flags & CitizenInstance.Flags.AtTarget))
                {
                    if ((m_flags & CitizenInstance.Flags.AtTarget) != CitizenInstance.Flags.None)
                    //Start AtTarget
                    {
                    }
                    else
                    //Cease AtTarget
                    {
                    }
                }
                if ((m_flags & CitizenInstance.Flags.InsideBuilding) != (t_flags & CitizenInstance.Flags.InsideBuilding))
                {
                }
                if ((m_flags & CitizenInstance.Flags.Deleted) != (t_flags & CitizenInstance.Flags.Deleted))
                {
                }
                if ((m_flags & CitizenInstance.Flags.CannotUseTransport) != (t_flags & CitizenInstance.Flags.CannotUseTransport))
                {
                }
                if ((m_flags & CitizenInstance.Flags.CannotUseTaxi) != (t_flags & CitizenInstance.Flags.CannotUseTaxi))
                {
                }
                /*
                * ----------------------------------------------------------------------------------------------------------------------------------------------------------
                * UNUSED:                           TargetFlags ; RequireSlowStart ; Transition ; Created ; CustomName ; CustomColor ; HangAround ;
                * ----------------------------------------------------------------------------------------------------------------------------------------------------------
                */


        



            }
        }

        private Vector3 GetCitizenPosition()
        {
            return m_citizen.GetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex).m_position;
        }

        private Vector3 GetCitizenVelocity()
        {
            return m_citizen.GetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex).m_velocity;
        }

        private Vector3 GetMyPosition()
        {
            if (GetMyVehicleSafety())
                return GetVehiclePosition();
            return GetCitizenPosition();
        }

        private Citizen GetMyCitizen()
        {
            return Singleton<CitizenManager>.instance.m_citizens.m_buffer[m_citizen.m_citizen];
        }

        private Vehicle GetMyVehicle() //MUST RUN WITH GETMYVEHICLESAFETY TO AVOID POSSIBLE CRASH
        {
            return Singleton<VehicleManager>.instance.m_vehicles.m_buffer[Singleton<CitizenManager>.instance.m_citizens.m_buffer[m_citizen.m_citizen].m_vehicle];
        }

        private bool GetMyVehicleSafety()
        {
            uint temp = Singleton<CitizenManager>.instance.m_citizens.m_buffer[m_citizen.m_citizen].m_vehicle;
            if (temp == 0)
            {
                return false;
            }
            return true;
        }

        private Vector3 GetVehiclePosition()
        {
            return GetMyVehicle().GetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex).m_position;
        }

        private float GetVehicleTravelDistance()
        {
            return GetMyVehicle().GetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex).m_travelDistance;
        }

        private VehicleInfo.VehicleType GetVehicleType()
        {
            return GetMyVehicle().Info.m_vehicleType;
        }

        private Vector3 GetVehicleVelocity()
        {
            return GetMyVehicle().GetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex).m_velocity;
        }

        private void RefreshCitizen()
        {
            m_citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[m_citizen.m_citizen];
        }

        private bool MediateMode(CitizenInstance.Flags t_flags)
        {
            //Check if current leg has been initialized
            if (activated && m_legs[m_current].getMode() != Leg.Flags.None)
            {
                //Check for modes needing updating
                m_legs[m_current].addTime(1);
                RefreshCitizen();
                switch (m_legs[m_current].getMode())
                {
                    case Leg.Flags.Bicycle:
                        if ((m_flags & CitizenInstance.Flags.OnBikeLane) != CitizenInstance.Flags.None)
                        {
                            m_legs[m_current].addTimeOnBikeLane(1);
                        }
                        break;

                    case Leg.Flags.Bus:
                        break;

                    case Leg.Flags.Car:
                        break;

                    case Leg.Flags.Metro:
                        break;

                    case Leg.Flags.Plane:
                        break;

                    case Leg.Flags.Ship:
                        break;

                    case Leg.Flags.Taxi:
                        break;

                    case Leg.Flags.Train:
                        break;

                    case Leg.Flags.Tram:
                        break;

                    case Leg.Flags.Walk:
                        break;
                }
                return true;
            }
            return false;
        }

        private void CheckHangups()
        {

        }
    }
}