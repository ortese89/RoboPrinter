using DeviceId;
using DeviceId.Encoders;
using DeviceId.Formatters;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Utilities.core;

public static class Extensions
{
    public static T? Clone<T>(T source)
    {
        if (source == null)
            return default;

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string jsonString = JsonSerializer.Serialize(source, options);
        return JsonSerializer.Deserialize<T>(jsonString, options);
    }

    public static byte[] CopyString(this object item, string value)
    {
        byte[] obj = (byte[])item;
        return Encoding.ASCII.GetBytes(value.PadRight(obj.Length, '\0'));
    }

	public static string GetDeviceId()
	{
		return new DeviceIdBuilder()
			   .OnWindows(windows => windows
				.AddProcessorId()
				.AddMotherboardSerialNumber())
			   .UseFormatter(new HashDeviceIdFormatter(() => SHA256.Create(), new Base64UrlByteArrayEncoder()))
			   .ToString();
	}

    public static string GetMd5Hash(MD5 md5Hash, string input)
    {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new();

        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        return sBuilder.ToString();
    }

    public static string GetTestRepository()
    {
        if (Directory.Exists(@"..//..//..//..//TestRepository//"))        
            return @"..//..//..//..//TestRepository//";

        if (Directory.Exists(@"..//..//..//..//..//TestRepository//"))
            return @"..//..//..//..//..//TestRepository//";

        return string.Empty;
    }

    public static bool IsNullOrZero(this byte value)
    {
        return value.Equals(0);
    }

    public static bool IsNullOrZero(this byte? value)
    {
        return value.Equals(0) || value.Equals(null);
    }

    public static bool IsNumeric(this string s)
    {
        return double.TryParse(s, out _);
    }

    public static TimeSpan StripMilliseconds(this TimeSpan time)
    {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }

    public static byte[] Reset(this byte[] item)
    {
        byte[] result = new byte[item.Length];

        for (int i = 0; i < item.Length - 1; i++)
        {
            result[i] = 0x0;
        }

        return result;
    }

    public static string ToInvariantString(this double value)
    {
        return value.ToString().Replace(",", ".");
    }

    public static string ToInvariantString(this float value)
    {
        return value.ToString().Replace(",", ".");
    }

    public static string ToInvariantString(this int value)
    {
        return value.ToString().Replace(",", ".");
    }

    public static Stream ToStream(this Image image, ImageFormat format)
    {
        MemoryStream stream = new();
        image.Save(stream, format);
        stream.Position = 0;
        return stream;
    }

    public static string ToStringNullSafe(this object value)
    {
        return (value ?? string.Empty).ToString();
    }

    public static string ToXML<T>(this T obj)
    {
        using var sw = new StringWriter();
        XmlSerializer serializer = new(typeof(T));
        serializer.Serialize(sw, obj);
        return sw.ToString();
    }

    public static string ToXMLDictionary(this IDictionary dictionary)
    {
        using StringWriter sw = new();
        List<Entry> entries = new(dictionary.Count);

        foreach (object key in dictionary.Keys)
        {
            var keyValue = dictionary[key];

            if (keyValue != null)
                entries.Add(new Entry(key, keyValue));
        }

        XmlSerializer serializer = new(typeof(List<Entry>));
        serializer.Serialize(sw, entries);
        return sw.ToString();
    }

    public static void XmlToDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, string xml)
        where TKey : notnull
    {
        using StringReader sr = new(xml);
        XmlSerializer serializer = new(typeof(List<Entry>));

        try
        {
            List<Entry> entryList = (List<Entry>)(serializer.Deserialize(sr) ?? new List<Entry>());

            foreach (Entry entry in entryList)
            {
                if (entry.Key is TKey key && entry.Value is TValue value)
                    dictionary[key] = value;
            }
        }
        catch (Exception)
        {
        }
    }
}

public class Entry
{
    public object? Key;
    public object? Value;

    public Entry() { 

    }

    public Entry(object key, object value)
    {
        Key = key;
        Value = value;
    }
}
