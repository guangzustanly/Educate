﻿#if  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER // Auto generated by AddMacroForInstantGameFiles.exe

using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace Unity.AutoStreaming
{
    internal class ModelTools
    {
        static void ReimportWithDefaultImportMaterial()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", null);

            int i = 0;
            foreach (var guid in modelGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
				
                if(importer == null)
                    continue;
				
                if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
                {
                    EditorUtility.DisplayProgressBar("Reimport with DefaultImportMaterial",
                        string.Format("Reimporting {0} {1}/{2}", assetPath, i, modelGuids.Length),
                        (i * 1.0f) / modelGuids.Length);
                    AssetDatabase.ImportAsset(assetPath);
                    Debug.Log(assetPath);
                }

                ++i;
            }

            EditorUtility.ClearProgressBar();
        }

        internal static void ReimportWithMaterial(Material mat)
        {
            if (mat != EditorGraphicsSettings.GetModelImportDefaultMaterial())
                EditorGraphicsSettings.SetModelImportDefaultMaterial(mat);

            ReimportWithDefaultImportMaterial();
        }



    }
}

#endif  //  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER, Auto generated by AddMacroForInstantGameFiles.exe