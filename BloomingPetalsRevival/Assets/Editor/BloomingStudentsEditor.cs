using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ChangeIDPopup : EditorWindow
{
    private int currentIndex;
    private int newIndex;
    private string currentStudentName;
    private List<string> allNames;
    private Action<int> onConfirm;

    public static void Show(
        int currentIndex,
        string currentStudentName,
        List<string> allNames,
        Action<int> onConfirm)
    {
        var win = CreateInstance<ChangeIDPopup>();
        win.currentIndex = currentIndex;
        win.newIndex = currentIndex;
        win.currentStudentName = currentStudentName;
        win.allNames = allNames;
        win.onConfirm = onConfirm;

        win.titleContent = new GUIContent("Change Student ID");
        win.position = new Rect(
            Screen.width / 2f,
            Screen.height / 2f,
            400,
            150);

        win.ShowUtility();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            $"{currentStudentName}'s new ID will be:",
            EditorStyles.wordWrappedLabel);

        newIndex = EditorGUILayout.IntField(newIndex + 1) - 1;

        int clamped = Mathf.Clamp(
            newIndex,
            0,
            allNames.Count - 1);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            $"They will replace {allNames[clamped]}, whose new ID will be {currentIndex + 1}",
            EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Confirm"))
        {
            onConfirm?.Invoke(clamped);
            Close();
        }
    }
}

[System.Serializable]
public class BloomingStudentsWrapper
{
    public List<SpawnStudentData> students;
}

public class BloomingStudentsEditor : EditorWindow
{
    private const string JsonPath =
        "Assets/StreamingAssets/Hidden/BloomingStudents.json";

    private List<SpawnStudentData> students =
        new List<SpawnStudentData>();

    private List<bool> foldouts =
        new List<bool>();

    private List<bool> runtimeFoldouts =
        new List<bool>();

    private List<Rect> headerRects =
        new List<Rect>();

    private Vector2 scroll;
    private bool fileExists;
    private string search = "";
    private int dragIndex = -1;

    [MenuItem("Tools/Blooming Students Editor")]
    public static void ShowWindow()
    {
        GetWindow<BloomingStudentsEditor>(
            "Blooming Students Editor");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void SyncLists()
    {
        while (foldouts.Count < students.Count)
            foldouts.Add(true);

        while (runtimeFoldouts.Count < students.Count)
            runtimeFoldouts.Add(false);

        while (foldouts.Count > students.Count)
            foldouts.RemoveAt(foldouts.Count - 1);

        while (runtimeFoldouts.Count > students.Count)
            runtimeFoldouts.RemoveAt(runtimeFoldouts.Count - 1);
    }

    private void OnGUI()
    {
        SyncLists();
        headerRects.Clear();

        if (!fileExists)
            EditorGUILayout.HelpBox(
                "BloomingStudents.json not found.",
                MessageType.Info);

        DrawTopBar();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < students.Count; i++)
            DrawStudentCard(i);

        HandleDragAndDrop();

        EditorGUILayout.EndScrollView();
        DrawFooter();
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal();
        search = EditorGUILayout.TextField("Search", search);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Expand All"))
            for (int i = 0; i < foldouts.Count; i++)
                foldouts[i] = true;

        if (GUILayout.Button("Collapse All"))
            for (int i = 0; i < foldouts.Count; i++)
                foldouts[i] = false;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStudentCard(int i)
    {
        var s = students[i];

        if (!string.IsNullOrEmpty(search))
        {
            string n =
                $"{s.FirstName} {s.LastName}".ToLower();

            if (!n.Contains(search.ToLower()))
                return;
        }

        GUI.backgroundColor =
            s.StudentGender == Gender.Female
                ? new Color(1f, .8f, .9f)
                : s.StudentGender == Gender.Male
                    ? new Color(.6f, .8f, 1f)
                    : Color.gray;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        foldouts[i] = EditorGUILayout.Foldout(
            foldouts[i],
            $"{i + 1}. {s.FirstName} {s.LastName}",
            true);

        if (GUILayout.Button("Change ID", GUILayout.MaxWidth(80)))
            ChangeStudentID(i);

        if (GUILayout.Button("Clone", GUILayout.MaxWidth(50)))
        {
            students.Insert(i + 1, s.Clone());
            foldouts.Insert(i + 1, true);
            runtimeFoldouts.Insert(i + 1, false);
        }

        if (GUILayout.Button("X", GUILayout.MaxWidth(25)))
        {
            if (EditorUtility.DisplayDialog(
                "Delete Student",
                $"Delete {s.FirstName} {s.LastName}?",
                "Delete",
                "Cancel"))
            {
                students.RemoveAt(i);
                foldouts.RemoveAt(i);
                runtimeFoldouts.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                return;
            }
        }

        EditorGUILayout.EndHorizontal();

        Rect headerRect = GUILayoutUtility.GetLastRect();
        headerRects.Add(headerRect);
        EditorGUIUtility.AddCursorRect(
            headerRect,
            MouseCursor.Pan);

        if (Event.current.type == EventType.MouseDown &&
            headerRect.Contains(Event.current.mousePosition))
        {
            dragIndex = i;
            Event.current.Use();
        }

        GUI.backgroundColor = Color.white;

        if (foldouts[i])
        {
            DrawStudent(s);
            DrawAccessories(s);
            DrawDestinations("Destinations", s, ref s.StudentDestinations);
            DrawDestinations("Patrol Destinations", s, ref s.PatrolDestinations);
            DrawRuntime(s, i);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawStudent(SpawnStudentData s)
    {
        EditorGUILayout.LabelField(
            $"Student ID: {s.StudentID}",
            EditorStyles.boldLabel);

        s.FirstName = EditorGUILayout.TextField("First Name", s.FirstName);
        s.LastName = EditorGUILayout.TextField("Last Name", s.LastName);
        s.StudentGender = (Gender)EditorGUILayout.EnumPopup("Gender", s.StudentGender);
        s.StudentRole = (Role)EditorGUILayout.EnumPopup("Role", s.StudentRole);
        s.StudentPersona = (Persona)EditorGUILayout.EnumPopup("Persona", s.StudentPersona);
        s.StudentClub = (Club)EditorGUILayout.EnumPopup("Club", s.StudentClub);
        s.Body = EditorGUILayout.IntField("Body", s.Body);
        s.Hairstyle = EditorGUILayout.IntField("Hairstyle", s.Hairstyle);
        s.Face = EditorGUILayout.IntField("Face", s.Face);
        s.StudentIdleAnimation = EditorGUILayout.TextField("Idle Animation", s.StudentIdleAnimation);
        s.StudentWalkAnimation = EditorGUILayout.TextField("Walk Animation", s.StudentWalkAnimation);
        s.StudentSprintAnimation = EditorGUILayout.TextField("Sprint Animation", s.StudentSprintAnimation);
        s.SubtitleColor = EditorGUILayout.ColorField("Subtitle Color", s.SubtitleColor);
        s.StudentInterests = (Interests)EditorGUILayout.EnumFlagsField("Interests", s.StudentInterests);
        s.AcceptedGifts = (GiftsToAccept)EditorGUILayout.EnumFlagsField("Accepted Gifts", s.AcceptedGifts);
        s.shouldPatrol = EditorGUILayout.Toggle("Should Patrol", s.shouldPatrol);
        s.trust = EditorGUILayout.IntSlider("Trust", s.trust, -50, 50);
    }

    private void DrawAccessories(SpawnStudentData s)
    {
        EditorGUILayout.LabelField(
            "Accessories",
            EditorStyles.boldLabel);

        for (int i = 0; i < s.StudentAccessories.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                s.StudentAccessories[i]?.ToString() ?? "Accessory",
                GUILayout.MaxWidth(200));

            if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
            {
                s.StudentAccessories.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Accessory"))
            s.StudentAccessories.Add(new Accessory());
    }

    private void DrawDestinations(
    string label,
    SpawnStudentData currentStudent,
    ref DestinationSpot[] array)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        if (array == null)
            array = new DestinationSpot[0];

        var list = array.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            list[i].destinationAction =
                (DestinationAction)EditorGUILayout.EnumPopup(
                    "Action",
                    list[i].destinationAction);

            list[i].animToPlayOnSpot =
                EditorGUILayout.TextField(
                    "Animation",
                    list[i].animToPlayOnSpot);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                string.IsNullOrEmpty(list[i].destinationID)
                    ? "No destination selected"
                    : DestinationDatabaseCache
                        .Get()
                        .FirstOrDefault(d => d.id == list[i].destinationID)
                        ?.name ?? "Missing Destination");

            if (GUILayout.Button("Select", GUILayout.MaxWidth(60)))
            {
                var used = new HashSet<string>();

                foreach (var other in students)
                {
                    if (other == currentStudent) continue;
                    foreach (var d in other.StudentDestinations)
                        if (!string.IsNullOrEmpty(d.destinationID))
                            used.Add(d.destinationID);
                }

                SelectDestinationPopup.Show(
                    list[i].destinationID,
                    used,
                    selected =>
                    {
                        list[i].destinationID = selected.id;
                    });
            }

            EditorGUILayout.EndHorizontal();

            list[i].spotTime =
                (Phase)EditorGUILayout.EnumPopup(
                    "Phase",
                    list[i].spotTime);

            list[i].occupied =
                EditorGUILayout.Toggle(
                    "Occupied",
                    list[i].occupied);

            if (GUILayout.Button("Remove"))
            {
                list.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Destination"))
            list.Add(new DestinationSpot());

        array = list.ToArray();
    }



    private void DrawRuntime(SpawnStudentData s, int i)
    {
        runtimeFoldouts[i] =
            EditorGUILayout.Foldout(
                runtimeFoldouts[i],
                "Runtime (Debug)",
                true);

        if (!runtimeFoldouts[i])
            return;

        EditorGUILayout.BeginVertical("box");

        s.didMeet = EditorGUILayout.Toggle("Did Meet", s.didMeet);
        s.didCompliment = EditorGUILayout.Toggle("Did Compliment", s.didCompliment);
        s.isDead = EditorGUILayout.Toggle("Is Dead", s.isDead);
        s.holdingGrudge = EditorGUILayout.Toggle("Holding Grudge", s.holdingGrudge);
        s.canApologise = EditorGUILayout.Toggle("Can Apologise", s.canApologise);
        s.apologiseFor = EditorGUILayout.TextField("Apologise For", s.apologiseFor);

        EditorGUILayout.EndVertical();
    }

    private void HandleDragAndDrop()
    {
        if (dragIndex < 0 || headerRects.Count == 0)
            return;

        Event e = Event.current;

        if (e.type == EventType.MouseUp)
        {
            for (int i = 0; i < headerRects.Count; i++)
            {
                if (e.mousePosition.y < headerRects[i].center.y)
                {
                    SwapStudents(dragIndex, i);
                    break;
                }
            }

            dragIndex = -1;
            e.Use();
        }
    }

    private void SwapStudents(int a, int b)
    {
        if (a == b)
            return;

        SpawnStudentData temp = students[a];
        students[a] = students[b];
        students[b] = temp;

        bool f = foldouts[a];
        foldouts[a] = foldouts[b];
        foldouts[b] = f;

        bool r = runtimeFoldouts[a];
        runtimeFoldouts[a] = runtimeFoldouts[b];
        runtimeFoldouts[b] = r;
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Student"))
        {
            students.Add(new SpawnStudentData());
            foldouts.Add(true);
            runtimeFoldouts.Add(false);
        }

        if (GUILayout.Button("Save"))
            SaveData();

        EditorGUILayout.EndHorizontal();
    }

    private void LoadData()
    {
        students.Clear();
        foldouts.Clear();
        runtimeFoldouts.Clear();

        fileExists = File.Exists(JsonPath);
        if (!fileExists)
            return;

        var wrapper =
            JsonUtility.FromJson<BloomingStudentsWrapper>(
                File.ReadAllText(JsonPath));

        if (wrapper != null && wrapper.students != null)
            students = wrapper.students;
    }

    private void SaveData()
    {
        for (int i = 0; i < students.Count; i++)
            students[i].StudentID = i + 1;

        File.WriteAllText(
            JsonPath,
            JsonUtility.ToJson(
                new BloomingStudentsWrapper
                {
                    students = students
                },
                true));

        if (Directory.Exists($"{Application.streamingAssetsPath}/Hidden/Backup"))
        {
            string backuppath = $"{Application.streamingAssetsPath}/Hidden/Backup/BloomingStudentsBackup_{System.DateTime.Now.Year}{System.DateTime.Now.Month}{System.DateTime.Now.Day}_{System.DateTime.Now.Hour}{System.DateTime.Now.Minute}{System.DateTime.Now.Second}.json";
            File.WriteAllText(
            backuppath,
            JsonUtility.ToJson(
                new BloomingStudentsWrapper
                {
                    students = students
                },
                true));
        }

        AssetDatabase.Refresh();
    }

    private void ChangeStudentID(int index)
    {
        ChangeIDPopup.Show(
            index,
            $"{students[index].FirstName} {students[index].LastName}",
            students.Select(
                s => $"{s.FirstName} {s.LastName}")
                .ToList(),
            target => SwapStudents(index, target));
    }
}

public static class SpawnStudentDataExtensions
{
    public static SpawnStudentData Clone(this SpawnStudentData o)
    {
        return JsonUtility.FromJson<SpawnStudentData>(
            JsonUtility.ToJson(o));
    }
}
