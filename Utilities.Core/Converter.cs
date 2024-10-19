using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Utilities.core;

public class Converter
{
    public static string ConvertBitArrayToHex(BitArray bitArray)
    {
        if (bitArray == null)
            return string.Empty;

        int myLength = bitArray.Length / 8;
        byte[] myByte = new byte[myLength - 1 + 1];

        bitArray.CopyTo(myByte, 0);

        return BitConverter.ToString(myByte).Replace("-", "");
    }

    public static BitArray ConvertHexToBitArray(string hexData)
    {
        if (hexData == null)
            return new BitArray(0);

        var value = ushort.Parse(hexData, NumberStyles.HexNumber);
        var bytes = BitConverter.GetBytes(value);
        var bitArray = new BitArray(bytes);

        return bitArray;
    }

    public static ushort[] ConvertByteArrayToWord(byte[] data)
    {
        if (data.Length < 2) return new ushort[6];
        int length = data.Length / 2 + Convert.ToInt16(data.Length % 2 > 0);
        ushort[] word = new ushort[length];
      
        for (int x = 0; x < length; x += 1)
        {
            word[x] = (ushort)(data[x * 2] * 256 + data[x * 2 + 1]);
        }

        return word;
    }

    public static ushort[] ConvertFloatToWordUShortCouple(float[] positions)
    {
        int num = positions.Length * 2;
        ushort[] word = new ushort[num];

        for (int i = 0; i < positions.Length; i++)
        {
            ushort[] positionWord = Converter.ConvertFloatToUShortCouple(positions[i]);
            word[i * 2] = positionWord[0];
            word[i * 2 + 1] = positionWord[1];
        }

        return word;
    }

    public static float ConvertShortCoupleToFloat(short upper, short lower)
    {
        string upperBinary = Convert.ToString(upper, 2);
        string lowerBinary = Convert.ToString(lower, 2);
        string composed = string.Format("{0}{1}", lowerBinary.ToString().PadLeft(16, '0'), upperBinary.ToString().PadLeft(16, '0'));

        int i = Convert.ToInt32(composed, 2);
        byte[] b = BitConverter.GetBytes(i);
        float original = BitConverter.ToSingle(b, 0);
        return original;
    }

    public static float ConvertUIntCoupleToFloat(ushort upper, ushort lower)
    {
        string upperBinary = Convert.ToString(upper, 2);
        string lowerBinary = Convert.ToString(lower, 2);
        string composed = string.Format("{0}{1}", lowerBinary.ToString().PadLeft(16, '0'), upperBinary.ToString().PadLeft(16, '0'));

        int i = Convert.ToInt32(composed, 2);
        byte[] b = BitConverter.GetBytes(i);
        float original = BitConverter.ToSingle(b, 0);
        return original;
    }

    public static short[] ConvertFloatToShortCouple(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        int i = BitConverter.ToInt32(bytes, 0);
        short upper = (short)(i >> 16);
        short lower = (short)(i & 0xFFFF);
        return new short[] { lower, upper };
    }

    public static ushort[] ConvertFloatToUShortCouple(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        int i = BitConverter.ToInt32(bytes, 0);
        ushort upper = (ushort)(i >> 16);
        ushort lower = (ushort)(i & 0xFFFF);
        return new ushort[] { lower, upper };
    }

    public static ushort[] ConvertFloatToUIntCouple(float value, int round = 3)
    {
        double roundedValue = Math.Round(value, round);
        short[] couple = ConvertFloatToShortCouple((float)roundedValue);
        return new ushort[] { (ushort)(Convert.ToInt16(couple[0]) + 0), (ushort)(Convert.ToInt16(couple[1]) + 0) };
    }

    public static byte[] ConvertResultDataToKeyValue(string controlResultData, string code)
    {
        string strResult = string.Empty;
        bool codeWritten = false;
        List<string> packedDetails = controlResultData.Split('|').ToList();

        foreach (string pd in packedDetails)
        {
            List<string> details = pd.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            if (details.Count > 8)
            {
                bool result = Convert.ToInt16(details[4]) == 1;
                bool degraded = Convert.ToInt16(details[4]) == 2;
                string name = details.Count > 8 ? details[8].ToStringNullSafe() : string.Empty;
                string outcome = degraded ? "RD=3" : (result ? "RD=1" : "RD=2");
                string codeResult = string.Empty;

                if (!string.IsNullOrEmpty(code) && !codeWritten)
                {
                    codeWritten = true;
                    codeResult = string.Format(":DT={0}", code);
                }

                strResult = string.Format("{0}ID={1}{2}:{3},", strResult, name, codeResult, outcome);
            }
        }

        strResult = string.Format("{0};", strResult.TrimEnd(','));

        return Encoding.ASCII.GetBytes(strResult);
    }

    public static float GetScaledValue(float value, float maxValue, int minPctZRange = 0, int maxPctZRange = 100, bool reverse = false)
    {
        if (maxValue == 0)
            return 0f;

        float result;
        float unit = maxValue / 100;
        float pctValue = value / unit;

        if (pctValue < minPctZRange)
            return 0f;

        if (pctValue > maxPctZRange)
            return 100f;

        float zoomedValue = pctValue - minPctZRange;
        float zoomedMaxValue = maxPctZRange - minPctZRange;
        float scaledPctValue = zoomedValue / zoomedMaxValue * 100;

        if (reverse)
            result = 100 - scaledPctValue;
        else
            result = scaledPctValue;

        return (float)Math.Round((decimal)result, 3, MidpointRounding.AwayFromZero);
    }

    public static string NumberToAlphabet(int number)
    {
        LinkedList<int> array = new();

        while (number > 26)
        {
            var value = number % 26;

            if (value == 0)
            {
                number = number / 26 - 1;
                array.AddFirst(26);
            }
            else
            {
                number /= 26;
                array.AddFirst(value);
            }
        }

        if (number > 0)
            array.AddFirst(number);

        return new string(array.Select(s => (char)(((int)("A").ToCharArray()[0] + s - 1))).ToArray());
    }

    public static string RectanglesToControlResultData(List<Rectangle> boxes)
    {
        string result = string.Empty;

        foreach (Rectangle item in boxes)
        {
            result = string.Format("{0}{1};{2};{3};{4};0;1|", result, item.X, item.Y, item.Width, item.Height);
        }

        return result.TrimEnd('|');
    }

    public static Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
    {
        double angleInRadians = angleInDegrees * (Math.PI / 180);
        double cosTheta = Math.Cos(angleInRadians);
        double sinTheta = Math.Sin(angleInRadians);

        Point pt = new()
        {
            X = (int)(cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
            Y = (int)(sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
        };

        return pt;
    }

    public static List<string> SplitString(string value, string mask)
    {
        List<string> results = new() { };
        string[] masks = mask.Split(';');
        int position = 0;

        foreach (string item in masks)
        {
            int myField = Convert.ToInt16(item);
            results.Add(value.Substring(position, myField));
            position += myField;
        }

        return results;
    }

    public static byte[] StringToByteArray(string value, byte[] byteArray)
    {
        byte[] result = new byte[byteArray.Length];
        byte[] valueArray = Encoding.ASCII.GetBytes(value);
        int byteCount = byteArray.Length > valueArray.Length ? valueArray.Length : byteArray.Length;

        Buffer.BlockCopy(valueArray, 0, result, 0, byteCount);

        return result;
    }

    public static int[] StringToWordArray(string value)
    {
        List<int> results = new() { };
        List<string> listExa = new() { };
        List<string> exaWord = new() { };

        int length = value.Length;

        if(length % 2 != 0)
            value = string.Format("{0}\0", value);

        char[] arrayChar = value.ToCharArray(); 

        for (int i = 0; i < arrayChar.Length; i++)
        {
            listExa.Add(Convert.ToByte(arrayChar[i]).ToString("x2"));
        }

        for (int i = 0; i < arrayChar.Length / 2; i++)
        {
            exaWord.Add(string.Format("{0}{1}", listExa[i * 2], listExa[(i * 2) + 1]));
        }

        for (int i = 0; i < exaWord.Count; i++)
        {
            long n = long.Parse(exaWord[i], NumberStyles.HexNumber);
            results.Add(Convert.ToInt32(n));
        }

        return results.ToArray<int>();
    }
}
