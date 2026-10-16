using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MeshColliderToBoxColliderTool
{
    private const string MenuPath = "Tools/Colliders/MeshCollider to BoxCollider";
    private const string UndoName = "MeshCollider to BoxCollider";

    [MenuItem(MenuPath)]
    private static void ConvertSelected()
    {
        var colliders = new HashSet<MeshCollider>();
        foreach (GameObject root in Selection.gameObjects)
        {
            if (!root.scene.IsValid())
                continue;

            foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
                colliders.Add(collider);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int converted = 0;
        int skipped = 0;

        try
        {
            foreach (MeshCollider source in colliders)
            {
                if (source.sharedMesh == null)
                {
                    skipped++;
                    continue;
                }

                // Mesh-local bounds stay aligned with the collider's existing transform.
                Bounds bounds = source.sharedMesh.bounds;
                BoxCollider box = Undo.AddComponent<BoxCollider>(source.gameObject);
                Undo.RecordObject(box, UndoName);
                box.center = bounds.center;
                box.size = bounds.size;
                box.sharedMaterial = source.sharedMaterial;
                box.isTrigger = source.isTrigger;
                box.contactOffset = source.contactOffset;
                box.includeLayers = source.includeLayers;
                box.excludeLayers = source.excludeLayers;
                box.layerOverridePriority = source.layerOverridePriority;
                box.providesContacts = source.providesContacts;
                box.enabled = source.enabled;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                EditorSceneManager.MarkSceneDirty(box.gameObject.scene);
                Undo.DestroyObjectImmediate(source);
                converted++;
            }
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        Debug.Log($"[Collider Tool] BoxCollider로 변경: {converted}개 / Mesh가 없어 건너뜀: {skipped}개. Ctrl+Z로 되돌릴 수 있습니다.");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanConvertSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        foreach (GameObject root in Selection.gameObjects)
        {
            if (root.scene.IsValid())
                return true;
        }

        return false;
    }
}
