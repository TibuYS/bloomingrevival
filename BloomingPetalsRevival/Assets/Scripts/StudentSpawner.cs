using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum Gender { Female, Male }
public enum Club { None, Cooking, Drama, Art }
public enum Role { Generic, ClubLeader, Rival, Senpai, Teacher }
public enum Persona { Carefree, Introvert, Arrogant, Neutral, Coward, PhoneAddict }
public enum Phase { BeforeClass, ClassPreparation, Classtime, Lunchtime, CleaningTime, EndOfDay, None }
public enum DestinationAction { Stand, Think, Patrol, Socialize }
[Serializable]
public class DestinationSpot
{
    public string destinationID;
    public string animToPlayOnSpot;
    public DestinationAction destinationAction;
    public Phase spotTime;
    public bool occupied;
}




[System.Flags]
public enum Interests
{
    Friends = 0,
    Solitude = 1 << 0,
    Cooking = 1 << 1,
    Drama = 1 << 2,
    Reading = 1 << 3,
    Money = 1 << 4
}

[System.Flags]
public enum GiftsToAccept
{
    None = 0,
    Money = 1 << 0,
    Snack = 1 << 1,
    Flower = 1 << 2
}

[System.Serializable]
public class Hairstyle
{
    public GameObject HairPrefab;
    public Vector3 HairPosition;
    public Vector3 HairRotation;
}

[System.Serializable]
public class Accessory
{
    public string AccessoryPath;
    public GameObject AccessoryPrefab;
    public Vector3 Position;
    public Vector3 Rotation;
}

[System.Serializable]
public class BloomingStudentsWrapper
{
    public List<SpawnStudentData> students;
}


#region FACE DATA

[System.Serializable]
public class BlendShapeValue
{
    public string name;
    [Range(0f, 100f)]
    public float value;
}

[System.Serializable]
public class FaceData
{
    public Material FaceMaterial;
    public List<BlendShapeValue> BlendShapes = new List<BlendShapeValue>();
}

#endregion

[System.Serializable]
public class SpawnStudentData
{
    [Header("Name")]
    public string FirstName;
    public string LastName;

    [Header("Gender")]
    public Gender StudentGender;

    [Header("Appearance")]
    public int Body;
    public int Hairstyle;
    public int Face;

    [Header("Club")]
    public Club StudentClub;

    [Header("Role")]
    public Role StudentRole;

    [Header("Personality")]
    public Persona StudentPersona;

    [Header("Gifts to accept")]
    public GiftsToAccept AcceptedGifts;

    [Header("Animations")]
    public string StudentIdleAnimation;
    public string StudentWalkAnimation;
    public string StudentSprintAnimation;

    [Header("Subtitle Color")]
    public Color SubtitleColor = Color.white;

    [Header("Student's Interests")]
    public Interests StudentInterests;

    [Header("Student's Destinations")]
    public DestinationSpot[] StudentDestinations;

    [Header("Student Accessories")]
    public List<Accessory> StudentAccessories = new List<Accessory>();

    public bool shouldPatrol;
    public DestinationSpot[] PatrolDestinations;

    [Header("Runtime Values")]
    public bool didCompliment;
    public bool didMeet;
    public bool isDead;
    public bool holdingGrudge;
    public bool canApologise;
    public string apologiseFor;
    public int StudentID;

    [Range(-50, 50)]
    public int trust;
}

public class StudentSpawner : MonoBehaviour
{
    [Header("Base Prefabs")] public GameObject FemaleStudentPrefab;
    public GameObject MaleStudentPrefab;
    public GameObject TeacherPrefab;[Header("Hairstyles")]
    public List<Hairstyle> FemaleHairstyles = new List<Hairstyle>();
    public List<Hairstyle> MaleHairstyles = new List<Hairstyle>();
    [Header("Body Materials")]
    public List<Material> FemaleBodyMaterials = new List<Material>();
    [Header("Face Presets")]
    public List<FaceData> FemaleFaces = new List<FaceData>();
    public List<FaceData> MaleFaces = new List<FaceData>();
    [Header("Spawned (runtime)")]
    public List<StudentScript> SpawnedStudents = new List<StudentScript>();
    public static StudentSpawner instance;

    private List<SpawnStudentData> loadedStudents = new List<SpawnStudentData>();

    private string JsonPath =>
        Path.Combine(Application.streamingAssetsPath, "Hidden/BloomingStudents.json");

    private void Awake()
    {
        instance = this;
        LoadStudentsFromJson();
    }

    private void Start()
    {
        foreach (var student in loadedStudents)
            SpawnStudent(student);
    }

    private void LoadStudentsFromJson()
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogError($"Student JSON not found at: {JsonPath}");
            return;
        }

        string json = File.ReadAllText(JsonPath);
        var wrapper = JsonUtility.FromJson<BloomingStudentsWrapper>(json);

        if (wrapper == null || wrapper.students == null)
        {
            Debug.LogError("Failed to parse student JSON.");
            return;
        }

        loadedStudents = wrapper.students;

        for (int i = 0; i < loadedStudents.Count; i++)
            loadedStudents[i].StudentID = i + 1;
    }

    private void SpawnStudent(SpawnStudentData data)
    {
        GameObject prefab;
        string hairPath;
        string facePath;
        string bodyPath;

        Hairstyle hair;
        FaceData faceData;
        Material bodyMat = null;

        if (data.StudentGender == Gender.Female)
        {
            prefab = data.StudentRole == Role.Teacher ? TeacherPrefab : FemaleStudentPrefab;

            hairPath = data.StudentRole == Role.Teacher
                ? "PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/Neck/Head"
                : "PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/Neck/Head/HairAttacher";

            facePath = data.StudentRole == Role.Teacher ? "TeacherHead" : "Meshes/Face";
            bodyPath = data.StudentRole == Role.Teacher ? "TeacherBody" : "Meshes/Body";

            hair = FemaleHairstyles[data.Hairstyle];
            faceData = FemaleFaces[data.Face];

            if (data.StudentRole != Role.Teacher)
                bodyMat = FemaleBodyMaterials[data.Body];
        }
        else
        {
            prefab = MaleStudentPrefab;
            hairPath = "PelvisRoot/Hips/Spine/Spine1/Spine2/Spine3/Neck/Head/HairAttacher";
            facePath = "Face";
            bodyPath = "Body";

            hair = MaleHairstyles[data.Hairstyle];
            faceData = MaleFaces[data.Face];
        }

        GameObject studentGO = Instantiate(prefab);
        studentGO.SetActive(true);

        StudentScript script = studentGO.GetComponent<StudentScript>();
        SpawnedStudents.Add(script);
        script.StudentData = data;

        studentGO.name = $"Student_{data.StudentID} ({data.FirstName} {data.LastName})";

        // hair
        GameObject hairGO = Instantiate(hair.HairPrefab);
        hairGO.transform.SetParent(studentGO.transform.Find(hairPath));
        hairGO.transform.localPosition = hair.HairPosition;
        hairGO.transform.localEulerAngles = hair.HairRotation;

        // face
        SkinnedMeshRenderer faceRenderer =
            studentGO.transform.Find(facePath).GetComponent<SkinnedMeshRenderer>();

        ApplyFaceData(faceRenderer, faceData);

        // body
        if (bodyMat != null)
        {
            SkinnedMeshRenderer body =
                studentGO.transform.Find(bodyPath).GetComponent<SkinnedMeshRenderer>();

            Material mat = Instantiate(bodyMat);
            body.material = mat;
            body.sharedMaterial = mat;
        }
    }

    public void ApplyFaceData(SkinnedMeshRenderer renderer, FaceData data)
    {
        if (renderer == null || data == null)
            return;

        if (data.FaceMaterial != null)
            renderer.material = Instantiate(data.FaceMaterial);

        Mesh mesh = renderer.sharedMesh;

        for (int i = 0; i < mesh.blendShapeCount; i++)
            renderer.SetBlendShapeWeight(i, 0f);

        foreach (var bs in data.BlendShapes)
        {
            int index = mesh.GetBlendShapeIndex(bs.name);
            if (index != -1)
                renderer.SetBlendShapeWeight(index, bs.value);
        }
    }
}

