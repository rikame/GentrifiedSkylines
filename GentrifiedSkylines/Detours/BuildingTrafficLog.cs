using System;
using UnityEngine;

namespace GentrifiedSkylines.Detours
{
    public class BuildingTrafficLog
    {
        public Trip[] sourceLog = new Trip[byte.MaxValue + 1];
        public Trip[] targetLog = new Trip[byte.MaxValue + 1];
        private byte indexSource;
        private byte indexTarget;
        private bool? activatedS;
        private bool? activatedT;
        private Building m_building;
        private ushort m_thisID;
        private bool used = false; //Becomes true when a
        private bool filledS = false; //Becomes true when a source is added
        private bool filledT = false; //Becomes true when a target is added

        public void NewBuildingLog(Building building, ushort ID)
        {
            m_building = building;
            m_thisID = ID;
            used = true;
        }

        public Building GetBuilding()
        {
            return m_building;
        }

        public ushort GetID()
        {
            return m_thisID;
        }

        public byte GetRating(bool source, bool target)
        {
            Debug.Log("Well, it's a start");
            if (used == true)
            {
                Debug.Log("Used is true!");
                byte ratingSource = 0;
                byte ratingTarget = 0;
                /*
                int count = 0;
                int cost = 0;
                int distance = 0;
                int distanceAverage = 0;
                int legs = 0;
                */
                float accumulated = 0;
                byte count = 0;
                Debug.Log("hey" + filledS);
                if (source & filledS)
                {
                    Debug.Log("A IndexSource: " + indexSource + ". Accumulated: " + accumulated);
                    for (int i = 0; i <= indexSource; i++)
                    {
                        Debug.Log("B IndexSource: " + indexSource + ". Accumulated: " + accumulated);
                        accumulated += sourceLog[Convert.ToByte(i)].GetContribution();
                        accumulated *= accumulated;
                        count++;
                    }
                    Debug.Log("C IndexSource: " + indexSource + ". Accumulated: " + accumulated);
                    ratingSource = Convert.ToByte(Mathf.Clamp(Convert.ToSingle(Math.Sqrt(Convert.ToDouble(accumulated / count))), 0, byte.MaxValue));
                }
                if (target & filledT)
                {
                    for (int i = 0; i <= indexTarget; i++)
                    {
                        accumulated += targetLog[Convert.ToByte(i)].GetContribution();
                        accumulated *= accumulated;
                        count++;
                    }
                    ratingTarget = Convert.ToByte(Mathf.Clamp(Convert.ToSingle(Math.Sqrt(Convert.ToDouble(accumulated / count))), 0, byte.MaxValue));
                }
                if (source & target)
                {
                    return Convert.ToByte((ratingSource + ratingTarget) / 2);
                }
                else if (source & !target)
                {
                    return ratingSource;
                }
                else if (target & !source)
                {
                    return ratingTarget;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public void addSourceEntry(Trip t)
        {
            filledS = true;
            if (!activatedS.HasValue)
            {
                SelfActivateS(t);
            }
            else
            {
                sourceLog[indexSource] = t;
                if (indexSource <= byte.MaxValue)
                {
                    indexSource += 1;
                }
                else
                {
                    indexSource = 0;
                }
            }
        }

        public void addTargetEntry(Trip t)
        {
            filledT = true;
            if (!activatedT.HasValue)
            {
                SelfActivateT(t);
            }
            else
            {
                targetLog[indexSource] = t;
                if (indexTarget <= byte.MaxValue)
                {
                    indexTarget += 1;
                }
                else
                {
                    indexTarget = 0;
                }
            }
        }

        private void SelfActivateS(Trip t)
        {
            activatedS = true;
            indexSource = 0;
            addSourceEntry(t);
        }

        private void SelfActivateT(Trip t)
        {
            activatedT = true;
            indexTarget = 0;
            addTargetEntry(t);
        }

        public Trip GetSourceTrip(byte b)
        {
            return sourceLog[Mathf.Clamp(b, 0, 255)];
        }

        public Trip GetTargetTrip(byte b)
        {
            return targetLog[Mathf.Clamp(b, 0, 255)];
        }

        public Trip[] GetSourceTrips()
        {
            Trip[] temp = new Trip[byte.MaxValue + 1];
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                temp[i] = sourceLog[i];
            }
            return temp;
        }

        public Trip[] GetTargetTrips()
        {
            Trip[] temp = new Trip[byte.MaxValue + 1];
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                temp[i] = targetLog[i];
            }
            return temp;
        }

        public Trip[] GetAllTrips()
        {
            Trip[] temp = new Trip[byte.MaxValue * 2 + 2];
            int i;
            for (i = 0; i <= byte.MaxValue; i++)
            {
                temp[i] = sourceLog[i];
            }
            for (i++; i <= (byte.MaxValue * 2) + 1; i++)
            {
                temp[i] = targetLog[i];
            }
            return temp;
        }

        public static byte[,] GenerateAccessibilityGrid(bool source, bool target)
        {
            byte[,] grid = new byte[byte.MaxValue + 1, byte.MaxValue + 1];
            if (!(source | target))
            {
                for (int i = 0; i <= byte.MaxValue; i++)
                {
                    for (int j = 0; j <= byte.MaxValue; j++)
                    {
                        grid[i, j] = 0;
                    }
                }
            }
            if (source & !target)
            {
                for (int i = 0; i <= byte.MaxValue; i++)
                {
                    for (int j = 0; j <= byte.MaxValue; j++)
                    {
                        //Analyze Trips
                    }
                }
            }
            if (target & !source)
            {
                for (int i = 0; i <= byte.MaxValue; i++)
                {
                    for (int j = 0; j <= byte.MaxValue; j++)
                    {
                        //Analyze Trips
                    }
                }
            }
            if (target & source)
            {
                byte[,] tempSource = GenerateAccessibilityGrid(true, false);
                byte[,] tempTarget = GenerateAccessibilityGrid(false, true);
                byte[,] tempCombined = tempSource;
                for (int i = 0; i <= byte.MaxValue; i++)
                {
                    for (int j = 0; j <= byte.MaxValue; j++)
                    {
                        tempCombined[Convert.ToByte(Mathf.Clamp(i, 0, byte.MaxValue)), Convert.ToByte(Mathf.Clamp(j, 0, byte.MaxValue))] = Convert.ToByte(Mathf.Clamp((tempCombined[Convert.ToByte(Mathf.Clamp(i, 0, byte.MaxValue)), Convert.ToByte(Mathf.Clamp(j, 0, byte.MaxValue))] + tempTarget[Convert.ToByte(Mathf.Clamp(i, 0, byte.MaxValue)), Convert.ToByte(Mathf.Clamp(j, 0, byte.MaxValue))]), 0, byte.MaxValue));
                    }
                }
                return tempCombined;
            }
            return grid;
        }
    }
}