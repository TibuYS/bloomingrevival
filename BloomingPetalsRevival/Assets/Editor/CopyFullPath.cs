using UnityEngine;
using UnityEditor;

public class CopyFullPath : MonoBehaviour
{
    [MenuItem("GameObject/Copy Full Path", false, 0)]
    private static void CopySelectedFullPath()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("error", "select a GameObject first", "OK");
            return;
        }

        string fullPath = GetFullPath(Selection.activeGameObject);
        EditorGUIUtility.systemCopyBuffer = fullPath;
        Debug.Log($"Copied path: {fullPath}");
    }

    private static string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
