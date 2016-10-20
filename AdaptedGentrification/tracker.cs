using UnityEngine;
using System;
namespace AdaptiveGentrification.Detours
{
    public static class tra
    {
        public static Int64[] cker;
        public static String[] ckerLandValue;
        public static String[] ckerOperator;
        public static bool? flag;
        public static bool? flagLandValue;
        static tra()
        {
            ckerActivate();
            serialize(new Int64[255]);
            serializeLandValue(new String[255]);
        }
        public static void ckerActivate()
        {
            if (!flag.HasValue)
            {
                flag = true;
                serialize(new Int64[255]);
                serializeLandValue(new String[255]);
                serializeOperator(new String[255]);
            }
        }
        public static Int64 ckerGet(byte district)
        {
            district = Convert.ToByte(Mathf.Clamp(district, 0, 255));
            if (flag.HasValue)
            {
                return cker[district];
            }
            else
            {
                ckerActivate();
                return ckerGet(district);
            }

        }
        public static string ckerGetLandValue(byte district)
        {
            district = Convert.ToByte(Mathf.Clamp(district, 0, 255));
            if (flag.HasValue)
            {
                return ckerLandValue[district];
            }
            else
            {
                ckerActivate();
                return ckerGetLandValue(district);
            }

        }
        public static string ckerGetOperator(byte district)
        {
            district = Convert.ToByte(Mathf.Clamp(district, 0, 255));
            if (flag.HasValue)
            {
                return ckerOperator[district];
            }
            else
            {
                ckerActivate();
                return ckerGetOperator(district);
            }

        }
        public static void serialize(Int64[] t)
        {
            for (byte i = 0; (i < 255); i++)
            {
                t[i] = 0;
                cker = t;
            }
        }
        public static void serializeLandValue(String[] s)
        {
            for (byte i = 0; (i < 255); i++)
            {
                s[i] = "0";
                ckerLandValue = s;
            }
        }
        public static void serializeOperator(String[] s)
        {
            for (byte i = 0; (i < 255); i++)
            {
                s[i] = "0";
                ckerOperator = s;
            }
        }
        public static void ckerLoad(byte l, Int64 v)
        {
            if (flag.HasValue)
            {
                l = Convert.ToByte(Mathf.Clamp(l, 0, 255));
                cker[l] += v;
            }
            else
            {
                ckerActivate();
                ckerLoad(l, v);
            }
        }
        public static void ckerSet(byte l, Int64 v)
        {
            if (flag.HasValue)
            {
                l = Convert.ToByte(Mathf.Clamp(l, 0, 255));
                cker[l] = v;
            }
            else
            {
                ckerActivate();
                ckerSet(l, v);
            }
        }
        public static void ckerSetLandValue(byte l, string str)
        {
            if (flag.HasValue)
            {
                l = Convert.ToByte(Mathf.Clamp(l, 0, 255));
                try
                {
                    double temp = Convert.ToDouble(str);
                    ckerLandValue[l] = str;
                }
                catch (System.FormatException e)
                {
                    ckerSetLandValue(l, str.Substring(1, str.Length));
                }
                catch (IndexOutOfRangeException e2)
                {
                    ckerLandValue[l] = "0";
                }
            }
            else
            {
                ckerActivate();
                ckerSetLandValue(l, str);
            }
        }
        public static void ckerSetOperator(byte l, string str)
        {
            if (flag.HasValue)
            {
                l = Convert.ToByte(Mathf.Clamp(l, 0, 255));
                try
                {
                    ckerOperator[l] = str;
                }
                catch (System.FormatException e)
                {
                    ckerSetOperator(l, str.Substring(1, str.Length));
                }
                catch (IndexOutOfRangeException e2)
                {
                    ckerOperator[l] = "X";
                }
            }
            else
            {
                ckerActivate();
                ckerSetOperator(l, str);
            }
        }
    }
}