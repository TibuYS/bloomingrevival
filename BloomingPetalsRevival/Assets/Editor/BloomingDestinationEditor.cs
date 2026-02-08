using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#region DATA_MODELS

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

#endregion

public class SelectDestinationPopup : EditorWindow
{
    private string currentID;
    private List<DestinationData> all;
    private HashSet<string> usedByOthers;
    private Action<DestinationData> onSelect;
    private Vector2 scroll;

    public static void Show(
        string currentID,
        HashSet<string> usedByOthers,
        Action<DestinationData> onSelect)
    {
        var win = CreateInstance<SelectDestinationPopup>();
        win.currentID = currentID;
        win.usedByOthers = usedByOthers;
        win.onSelect = onSelect;
        win.all = DestinationDatabaseCache.Get();

        win.titleContent = new GUIContent("Select Destination");
        win.position = new Rect(
            Screen.width / 2f,
            Screen.height / 2f,
            350,
            400);

        win.ShowUtility();
    }

    private string search = "";

    private void OnGUI()
    {
        GUILayout.Label("Select Destination", EditorStyles.boldLabel);

        search = EditorGUILayout.TextField("Search", search);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var d in all)
        {
            if (!string.IsNullOrEmpty(search) &&
                !d.name.ToLower().Contains(search.ToLower()))
                continue;

            bool used = usedByOthers.Contains(d.id);
            bool isCurrent = d.id == currentID;

            Color prev = GUI.backgroundColor;

            if (used && !isCurrent)
                GUI.backgroundColor = new Color(1f, .6f, .6f);

            if (GUILayout.Button(d.name))
            {
                onSelect?.Invoke(d);
                Close();
            }

            GUI.backgroundColor = prev;
        }

        EditorGUILayout.EndScrollView();
    }
}


#region JSON_IO

[Serializable]
public class BloomingDestinationWrapper
{
    public List<DestinationData> destinations;
}

public static class DestinationDatabaseCache
{
    private static List<DestinationData> cached;
    private static DateTime lastWrite;

    private static string Path =>
        $"{Application.streamingAssetsPath}/Hidden/BloomingDestinations.json";

    public static List<DestinationData> Get()
    {
        if (!File.Exists(Path))
            return new List<DestinationData>();

        var t = File.GetLastWriteTimeUtc(Path);
        if (cached == null || t != lastWrite)
        {
            cached = JsonUtility
                .FromJson<BloomingDestinationWrapper>(
                    File.ReadAllText(Path))
                ?.destinations ?? new List<DestinationData>();

            lastWrite = t;
        }

        return cached;
    }
}

public static class DestinationIO
{
    public static string FilePath =>
        Path.Combine(Application.streamingAssetsPath,
            "Hidden/BloomingDestinations.json");

    static DateTime lastWriteTime;

    public static DestinationDatabase Load(bool force = false)
    {
        if (!File.Exists(FilePath))
            return new DestinationDatabase();

        DateTime writeTime = File.GetLastWriteTimeUtc(FilePath);

        if (!force && writeTime == lastWriteTime)
            return null;

        lastWriteTime = writeTime;

        string json = File.ReadAllText(FilePath);
        var db = JsonUtility.FromJson<DestinationDatabase>(json) ?? new DestinationDatabase();

        foreach (var d in db.destinations)
        {
            if (string.IsNullOrEmpty(d.id))
                d.id = Guid.NewGuid().ToString();
        }

        return db;
    }

    public static void Save(DestinationDatabase db)
    {
        string dir = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(FilePath, json);

        lastWriteTime = File.GetLastWriteTimeUtc(FilePath);
        AssetDatabase.Refresh();
    }
}

#endregion

#region DESTINATION_EDITOR_WINDOW

public class BloomingDestinationEditor : EditorWindow
{
    DestinationDatabase db;
    string search = "";
    string renameBuffer = "";
    int selectedIndex = -1;
    Vector2 scroll;

    [MenuItem("Tools/Blooming Destination Editor")]
    static void Open()
    {
        GetWindow<BloomingDestinationEditor>("Destinations");
    }

    void OnEnable()
    {
        Reload(force: true);
        EditorApplication.update += LiveReload;
    }

    void OnDisable()
    {
        EditorApplication.update -= LiveReload;
    }

    void LiveReload()
    {
        var updated = DestinationIO.Load();
        if (updated != null)
        {
            db = updated;
            Repaint();
        }
    }

    void Reload(bool force = false)
    {
        db = DestinationIO.Load(force) ?? new DestinationDatabase();
    }

    void OnGUI()
    {
        if (db == null)
            Reload(true);

        GUILayout.Label("Blooming Destination Editor", EditorStyles.boldLabel);
        search = EditorGUILayout.TextField("Search", search);

        GUILayout.Space(5);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(150));

        for (int i = 0; i < db.destinations.Count; i++)
        {
            var d = db.destinations[i];

            if (!string.IsNullOrEmpty(search) &&
                !d.name.ToLower().Contains(search.ToLower()))
                continue;

            if (GUILayout.Button(d.name, EditorStyles.miniButton))
            {
                selectedIndex = i;
                renameBuffer = d.name;
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);

        if (selectedIndex >= 0 && selectedIndex < db.destinations.Count)
        {
            DrawSelected(db.destinations[selectedIndex]);
        }
    }

    void DrawSelected(DestinationData d)
    {
        GUILayout.Label("Selected Destination", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("ID", d.id);

        renameBuffer = EditorGUILayout.TextField("Name", renameBuffer);
        if (renameBuffer != d.name)
            d.name = renameBuffer;

        d.position = EditorGUILayout.Vector3Field("Position", d.position);
        d.rotation = EditorGUILayout.Vector3Field("Rotation", d.rotation);

        GUILayout.Space(5);

        if (GUILayout.Button("Change Position to Statue Position"))
        {
            var statue = GameObject.Find("DestinationStatue");
            if (statue != null)
            {
                d.position = statue.transform.position;
                d.rotation = statue.transform.eulerAngles;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Missing Statue",
                    "DestinationStatue not found in scene.",
                    "OK");
            }
        }

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save"))
            DestinationIO.Save(db);

        if (GUILayout.Button("Delete"))
        {
            if (EditorUtility.DisplayDialog(
                "Delete Destination",
                $"Delete '{d.name}'?",
                "Yes", "No"))
            {
                db.destinations.RemoveAt(selectedIndex);
                selectedIndex = -1;
                DestinationIO.Save(db);
            }
        }

        GUILayout.EndHorizontal();
    }
}

#endregion
