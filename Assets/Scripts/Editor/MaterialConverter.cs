using UnityEngine;
using UnityEditor;

public class MaterialConverter : EditorWindow
{
    [MenuItem("Tools/Convert Pink Materials to URP")]
    static void ConvertMaterials()
    {
        // Знаходимо всі матеріали в проекті
        string[] guids = AssetDatabase.FindAssets("t:Material"); // шукає ВСІ матеріали в проекті
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // Конвертуємо всі не-URP шейдери
            if (!mat.shader.name.Contains("Universal Render Pipeline") &&
                !mat.shader.name.Contains("Unlit"))
            {
                // Зберігаємо текстуру
                Texture albedo = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                // Міняємо на URP шейдер
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");

                // Відновлюємо текстуру
                if (albedo != null)
                    mat.SetTexture("_BaseMap", albedo);

                mat.SetColor("_BaseColor", color);
count++;
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"Converted: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Конвертовано {count} матеріалів!");
    }
}