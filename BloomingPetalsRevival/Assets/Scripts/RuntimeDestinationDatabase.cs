using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

[Serializable]
public class DestinationData
{
    public string id;
    public string name;
    public Vector3 position;
    public Vector3 rotation;
}

[Serializable]
public class DestinationDatabase
{
    public List<DestinationData> destinations = new List<DestinationData>();
}

public static class RuntimeDestinationDatabase
{
    private static List<DestinationData> cached;
    private static DateTime lastWrite;

    private static string Path =>
        $"{Application.streamingAssetsPath}/Hidden/BloomingDestinations.json";

    public static List<DestinationData> GetAll()
    {
        if (!File.Exists(Path))
            return new List<DestinationData>();

        var writeTime = File.GetLastWriteTimeUtc(Path);

        if (cached == null || writeTime != lastWrite)
        {
            string json = File.ReadAllText(Path);
            var wrapper = JsonUtility.FromJson<BloomingDestinationWrapper>(json);
            cached = wrapper?.destinations ?? new List<DestinationData>();
            lastWrite = writeTime;
        }

        return cached;
    }

    public static DestinationData GetByID(string id)
    {
        return GetAll().FirstOrDefault(d => d.id == id);
    }
}

[Serializable]
public class BloomingDestinationWrapper
{
    public List<DestinationData> destinations;
}
