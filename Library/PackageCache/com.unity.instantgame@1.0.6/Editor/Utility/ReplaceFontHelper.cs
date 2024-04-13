#if  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER // Auto generated by AddMacroForInstantGameFiles.exe

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
#if UNITY_UGUI
using UnityEngine.UI;
#endif

public class ReplaceFonts : Editor
{
    //Replace all font usage inside scenes and prefabs
    internal static void ReplaceTextFont(Font fontToUse)
    {
        if (fontToUse == null)
            return;

        //replace text font in scene
        foreach (var scene in EditorBuildSettings.scenes)
        {
            var s = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);

            var sceneRoots = s.GetRootGameObjects();
            foreach (var root in sceneRoots)
            {

#if UNITY_UGUI
                var textComps = root.GetComponentsInChildren<Text>(true);
                if (textComps != null && textComps.Length != 0)
                {
                    foreach (var text in textComps)
                    {
                        if (text.font == null || text.font != fontToUse)
                        {
                            text.font = fontToUse;
                            EditorUtility.SetDirty(text);
                        }
                    }
                }
#endif
                //text mesh comps
                var textMeshComps = root.GetComponentsInChildren<TextMesh>(true);
                if (textMeshComps == null || textMeshComps.Length == 0)
                    continue;
                foreach (var text in textMeshComps)
                {
                    if (text.font == null || text.font != fontToUse)
                    {
                        text.font = fontToUse;
                        MeshRenderer mr = text.gameObject.GetComponent<MeshRenderer>();
                        if (mr == null)
                        {
                            Debug.LogError("Error: can not find mesh renderer in text mesh gameobject");
                        }
                        mr.material = fontToUse.material;
                        EditorUtility.SetDirty(text);
                        EditorUtility.SetDirty(mr);
                    }
                }
            }

            EditorSceneManager.SaveScene(s);
        }

        string[] guids = AssetDatabase.FindAssets("t:prefab", null);


        foreach (string guid in guids)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid),typeof(GameObject)) as GameObject;

#if UNITY_UGUI
            //replace text font in prefab
            var textComps = prefab.GetComponentsInChildren<Text>(true);
            if (textComps != null && textComps.Length != 0)
            {
                foreach (var text in textComps)
                {
                    if (text.font == null || text.font != fontToUse)
                    {
                        text.font = fontToUse;
                        EditorUtility.SetDirty(text);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    }
                }
            }
#endif
            //text mesh comps
            var textMeshComps = prefab.GetComponentsInChildren<TextMesh>(true);
            if (textMeshComps == null || textMeshComps.Length == 0)
                continue;
            foreach (var text in textMeshComps)
            {
                if (text.font == null || text.font != fontToUse)
                {
                    text.font = fontToUse;
                    MeshRenderer mr = text.gameObject.GetComponent<MeshRenderer>();
                    if (mr == null)
                    {
                        Debug.LogError("Error: can not find mesh renderer in text mesh gameobject");
                    }
                    mr.material = fontToUse.material;
                    EditorUtility.SetDirty(text);
                    EditorUtility.SetDirty(mr);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mr);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif  //  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER, Auto generated by AddMacroForInstantGameFiles.exe
