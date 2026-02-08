using UnityEditor;
using UnityEngine;

public class FaceBlendShapeWindow : EditorWindow
{
    private StudentSpawner spawner;

    private SkinnedMeshRenderer skinnedRenderer;
    private Mesh mesh;

    private bool female = true;
    private int faceIndex;

    public static void OpenWindow(StudentSpawner spawner)
    {
        FaceBlendShapeWindow window =
            CreateInstance<FaceBlendShapeWindow>();

        window.titleContent = new GUIContent("Face BlendShapes");
        window.spawner = spawner;

        window.ShowUtility();
    }

    private void OnGUI()
    {
        if (spawner == null)
        {
            EditorGUILayout.HelpBox(
                "StudentSpawner reference missing.",
                MessageType.Error);
            return;
        }

        GUILayout.Label("Source", EditorStyles.boldLabel);

        skinnedRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
            "SkinnedMeshRenderer",
            skinnedRenderer,
            typeof(SkinnedMeshRenderer),
            true);

        mesh = (Mesh)EditorGUILayout.ObjectField(
            "Mesh (optional)",
            mesh,
            typeof(Mesh),
            false);

        GUILayout.Space(10);

        GUILayout.Label("Target Face", EditorStyles.boldLabel);

        female = EditorGUILayout.Toggle("Female", female);
        faceIndex = EditorGUILayout.IntField("Face Index", faceIndex);

        GUILayout.Space(10);

        GUI.enabled = skinnedRenderer != null || mesh != null;

        if (GUILayout.Button("Populate BlendShapes"))
        {
            Populate();
        }

        GUI.enabled = true;
    }

    private void Populate()
    {
        Mesh sourceMesh =
            skinnedRenderer != null
            ? skinnedRenderer.sharedMesh
            : mesh;

        if (sourceMesh == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No mesh assigned.",
                "OK");
            return;
        }

        var faceList = female ? spawner.FemaleFaces : spawner.MaleFaces;

        if (faceIndex < 0 || faceIndex >= faceList.Count)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Face index must be between 0 and {faceList.Count - 1}",
                "OK");
            return;
        }

        FaceData faceData = faceList[faceIndex];
        faceData.BlendShapes.Clear();

        for (int i = 0; i < sourceMesh.blendShapeCount; i++)
        {
            faceData.BlendShapes.Add(new BlendShapeValue
            {
                name = sourceMesh.GetBlendShapeName(i),
                value = skinnedRenderer != null
                    ? skinnedRenderer.GetBlendShapeWeight(i)
                    : 0f
            });
        }

        EditorUtility.SetDirty(spawner);

        Debug.Log(
            $"Populated {sourceMesh.blendShapeCount} blendshapes into " +
            $"{(female ? "Female" : "Male")} face index {faceIndex}");
    }
}
