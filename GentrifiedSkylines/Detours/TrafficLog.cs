using System;
using UnityEngine;

namespace GentrifiedSkylines.Detours
{
    public static class TrafficLog
    {
        private static ushort[] buildingIDs;
        private static ushort buildingIndex;
        private static BuildingTrafficLog[] masterLog;
        private static bool? activated;
        private static byte[,] grid2;
        private static bool used = false;

        public static void Activate()
        {
            activated = true;
            buildingIndex = 0;
            masterLog = new BuildingTrafficLog[ushort.MaxValue];
            buildingIDs = new ushort[ushort.MaxValue];
        }

        public static byte[,] CollectOldRatings()
        {
            return grid2;
        }

        public static byte[,] CollectRatings(bool source, bool target)
        {
            if (activated.HasValue)
            {
                grid2 = new byte[byte.MaxValue + 1, byte.MaxValue + 1];
                for (int i = 0; i <= byte.MaxValue; i++)
                {
                    for (int j = 0; j <= byte.MaxValue; j++)
                    {
                        grid2[i, j] = 0;
                    }
                }
                if (used == true)
                {
                    for (ushort i = 0; i <= buildingIndex; i++)
                    {
                        BuildingTrafficLog localLog = masterLog[i];
                        Debug.Log("I: " + i + ".");
                        ushort localID = buildingIDs[i];
                        Building building = BuildingManager.instance.m_buildings.m_buffer[buildingIDs[Convert.ToUInt16(i)]];
                        float x = building.m_position.x;
                        float z = building.m_position.z;
                        byte x2 = Convert.ToByte(Mathf.Clamp(((x + (128f * 38.4f)) / 38.4f), 0, byte.MaxValue));
                        byte z2 = Convert.ToByte(Mathf.Clamp(((z + (128f * 38.4f)) / 38.4f), 0, byte.MaxValue));
                        //NOTE: This currently sums the value of all buildings in an area. Needs normalization.
                        //NOTE: source and target bools are passed down through this method into the buildingLog.
                        Debug.Log("Step1");
                        grid2[x2, z2] += masterLog[i].GetRating(source, target);
                        Debug.Log("Step2");
                        Debug.Log(masterLog[i].GetRating(source, target));
                    }
                }
                return grid2;
            }
            else
            {
                Activate();     //Try again
                return CollectRatings(source, target);
            }
        }

        public static void Reset()
        {
            //Reset tracking
        }

        public static void NewBuildingLog(Building tempBuilidng, ushort tempID)
        {
            used = true;
            if (!activated.HasValue)
            {
                Activate();     //Try again
            }
            //NOTE: This checks to see if the building has been added already
            if (IDToRef(tempID) == 0)
            {
                AddBuildingRef(tempID);
                masterLog[buildingIndex] = new BuildingTrafficLog();
                masterLog[buildingIndex].NewBuildingLog(tempBuilidng, tempID);
            }
        }

        public static void AddBuildingRef(ushort tempID)
        {
            buildingIDs[buildingIndex] = tempID;
            buildingIndex++;
        }

        public static ushort IDToRef(ushort tempID)     //Takes real ID and finds index position
        {
            if (activated.HasValue)
            {
                for (int i = 0; i <= buildingIndex; i++)
                {
                    if (buildingIDs[i] == tempID)
                    {
                        return Convert.ToUInt16(i);
                    }
                }
                return 0;
            }
            else
            {
                Activate();
                return IDToRef(tempID);
            }
        }
    }
}