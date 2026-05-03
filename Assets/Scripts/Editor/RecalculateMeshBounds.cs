using UnityEngine;
using UnityEditor;

public static class RecalculateMeshBounds
{
    [MenuItem("Tools/Recalculate Bounds On Selected")]
    static void RecalcSelected()
    {
        int meshCount = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                mf.sharedMesh.RecalculateBounds();
                EditorUtility.SetDirty(mf.sharedMesh);
                meshCount++;
            }
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                EditorUtility.SetDirty(smr);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Recalculated bounds on {meshCount} meshes.");
    }

    [MenuItem("Tools/Disable Culling On Selected (Always Render)")]
    static void DisableCulling()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.allowOcclusionWhenDynamic = false;
                count++;
            }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                b.Expand(100f);
                mf.sharedMesh.bounds = b;
                EditorUtility.SetDirty(mf.sharedMesh);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Expanded bounds on {count} renderers (will always render).");
    }
}
