using UnityEngine;
using UnityEditor;

public class AutoUVScale : EditorWindow
{
    [MenuItem("Tools/Fix Wall UV Tiling")]
    static void FixUVs()
    {
        GameObject[] selected = Selection.gameObjects;
        int fixed_count = 0;

        foreach (GameObject obj in selected)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;

                Vector3 scale = rend.transform.lossyScale;

                Material mat = new Material(rend.sharedMaterial);

                // Розміри трьох осей
                float x = scale.x;
                float y = scale.y;
                float z = scale.z;

                // Якщо одна з горизонтальних осей дуже маленька (торець)
                // — беремо більшу горизонтальну як ширину
                float width;
                if (x < 0.5f)
                    width = z;
                else if (z < 0.5f)
                    width = x;
                else
                    width = Mathf.Max(x, z);

                // Висота завжди Y
                float tileX = width / 2f;
                float tileY = y / 2f;

                // Мінімум 1 повторення
                tileX = Mathf.Max(tileX, 1f);
                tileY = Mathf.Max(tileY, 1f);

                mat.mainTextureScale = new Vector2(tileX, tileY);
                rend.sharedMaterial = mat;
                fixed_count++;
            }
        }

        Debug.Log($"✅ Виправлено UV на {fixed_count} об'єктах");
    }

    [MenuItem("Tools/Hide Wall Edges (shrink Z)")]
    static void ShrinkWalls()
    {
        GameObject[] selected = Selection.gameObjects;
        int count = 0;

        foreach (GameObject obj in selected)
        {
            Transform[] children = obj.GetComponentsInChildren<Transform>();
            foreach (Transform t in children)
            {
                Vector3 s = t.localScale;
                // Зменшуємо найтоншу вісь до 0.05 щоб торці майже не було видно
                if (s.x < 0.5f) s.x = 0.05f;
                if (s.z < 0.5f) s.z = 0.05f;
                t.localScale = s;
                count++;
            }
        }

        Debug.Log($"Згорнуто {count} об'єктів");
    }
}