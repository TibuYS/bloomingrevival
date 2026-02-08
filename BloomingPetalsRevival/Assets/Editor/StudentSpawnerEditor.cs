using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StudentSpawner))]
public class StudentSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("bp student spawner tools", EditorStyles.boldLabel);

        if (GUILayout.Button("load face blendshapes from.."))
        {
            FaceBlendShapeWindow.OpenWindow((StudentSpawner)target);
        }
    }
}
