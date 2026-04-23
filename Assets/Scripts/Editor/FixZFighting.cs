using UnityEngine;
using UnityEditor;

public class FixZFighting : EditorWindow
{
    [MenuItem("Tools/Fix Z-Fighting (stretch walls 0.01)")]
    static void Fix()
    {
        GameObject[] selected = Selection.gameObjects;
        int fixed_count = 0;

        foreach (GameObject obj in selected)
        {
            Transform[] children = obj.GetComponentsInChildren<Transform>();

            foreach (Transform t in children)
            {
                // Трохи збільшуємо scale щоб стіни перекривались зі стелею/підлогою
                t.localScale += new Vector3(0.001f, 0.001f, 0.001f);
                fixed_count++;
            }
        }

        Debug.Log($"✅ Виправлено {fixed_count} об'єктів");
    }
}