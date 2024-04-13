#if  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER // Auto generated by AddMacroForInstantGameFiles.exe

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Unity.AutoStreaming
{
    internal class ASTextureUI : TabBase<ASTexturesTreeView, ASTextureTreeDataItem>
    {
        static readonly string k_AutoStreamingAbLutDir = AutoStreamingSettings.autoStreamingDirectory + "/ASABLut";

        bool m_TexForceRebuildAssetBundle;
        private GUIStyle m_ToggleStyle;

        public ASTextureUI()
        {
            m_ToggleStyle = GUI.skin.toggle;
            m_ToggleStyle.margin = new RectOffset(3, 3, 3, 2);
        }

        protected override MultiColumnHeaderState CreateColumnHeaderState(float treeViewWidth)
        {
            return ASTexturesTreeView.CreateDefaultMultiColumnHeaderState(treeViewWidth);
        }

        protected override void InitTreeView(MultiColumnHeader multiColumnHeader)
        {
            var treeModel = new TreeModelT<ASTextureTreeDataItem>(ASMainWindow.Instance.TextureData);
            m_TreeView = new ASTexturesTreeView(m_TreeViewState, multiColumnHeader, treeModel);
        }

        protected override void OnToolbarGUI(Rect rect)
        {
            GUILayout.BeginArea(rect);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);

                if (GUILayout.Button("Sync Textures", s_MiniButton, GUILayout.Width(100)))
                {
                    SyncTextures(false);
                }

                GUILayout.Space(20);
                m_TexForceRebuildAssetBundle = GUILayout.Toggle(m_TexForceRebuildAssetBundle, "Force Rebuild", m_ToggleStyle);
                if (GUILayout.Button("Generate AssetBundles", s_MiniButton, GUILayout.Width(150)))
                {
                    GenerateTextureAssetBundles(m_TexForceRebuildAssetBundle);
                    SyncTextures(true);
                }

                if (GUILayout.Button("Generate Placeholders", s_MiniButton, GUILayout.Width(150)))
                {
                    GeneratePlaceholders();
                }

#pragma warning disable CS0618 // Type or member is obsolete
                bool useLegacySpritePacker =
                    (EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOn
                        || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnly);
#pragma warning restore CS0618 // Type or member is obsolete

                if (useLegacySpritePacker)
                {
                    if (GUILayout.Button("ConvertLegacySpritePacker", s_MiniButton, GUILayout.Width(200)))
                    {
                        ConvertLegacySpritePackerToSpriteAtlas();
                    }
                }


#if UNITY_ADDRESSABLES
                if (PlayerSettings.autoStreaming)
                {
                    if (GUILayout.Button("Use SpriteAtlas Placeholder in Addressable", s_MiniButton, GUILayout.Width(280)))
                    {
                        AddressableSpriteAtlasUtils.ReplaceAdressableSpriteAtlas(true);
                    }
                }
                else
                {
                    if (GUILayout.Button("Use Original SpriteAtlas in Addressable", s_MiniButton, GUILayout.Width(280)))
                    {
                        AddressableSpriteAtlasUtils.ReplaceAdressableSpriteAtlas(false);
                    }
                }
#endif
                GUILayout.Space(5);
                m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString, GUILayout.Width(200));

                GUILayout.FlexibleSpace();

                string statusReport = "";
                var allTexs = AutoStreamingSettings.textures;
                var placeholderItems = allTexs.Where(x => x.usePlaceholder);
                statusReport = string.Format("Placeholder: {0}/{1},RT: {2}, AB: {3}, Warning: {4}",
                    placeholderItems.Count(),
                    allTexs.Length,
                    EditorUtility.FormatBytes(placeholderItems.Select(x => (long)x.runtimeMemory).Sum()),
                    EditorUtility.FormatBytes(placeholderItems.Select(x => (long)x.assetBundleSize).Sum()),
                    placeholderItems.Select(x => (x.warningFlag != 0 ? 1 : 0)).Sum());
                GUILayout.Label(statusReport);
            }

            GUILayout.EndArea();
        }

        void SyncTextures(bool updateAbAndPlaceholderOnly)
        {
            ASUtilities.GenerateAddressablePathsText();
            AutoStreamingSettings.SyncTextures(updateAbAndPlaceholderOnly);
            ASMainWindow.Instance.TextureData = null;
            m_TreeViewInitialized = false;
        }

        static Texture2D LoadTexture2DFromPath(string path)
        {
            var tex2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex2D != null)
                return tex2D;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (typeof(Texture2D).IsInstanceOfType(asset))
                    return asset as Texture2D;
            }
            return null;
        }

        static bool AssetExists(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return false;

            var obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
            if (obj == null)
                return false;

            return true;
        }

        static void WriteOutAtlasTag2AbNameFile()
        {
            List<string> saABLines = new List<string>();
            saABLines.Add("spriteatlas");
            saABLines.Add("0");

            var texItems = AutoStreamingSettings.textures;

            foreach (var texItem in texItems)
            {
                if (texItem.usePlaceholder && texItem.assetPath.EndsWith(".spriteatlas"))
                {
                    SpriteAtlas sa = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(texItem.assetPath);
                    if (sa == null)
                    {
                        UnityEngine.Debug.LogError("AutoStreaming: Missing Spriteatlas at path: " + texItem.assetPath);
                        continue;
                    }

                    saABLines.Add(sa.tag);
                    saABLines.Add(AssetDatabase.AssetPathToGUID(texItem.assetPath));
                }
            }

            if (saABLines.Count > 2)
            {
                saABLines[1] = (saABLines.Count - 2).ToString();
                if (!Directory.Exists(k_AutoStreamingAbLutDir))
                    Directory.CreateDirectory(k_AutoStreamingAbLutDir);

                File.WriteAllText(Path.Combine(k_AutoStreamingAbLutDir, "spriteatlas"), string.Join("\n", saABLines.ToArray()));
            }
        }

        internal static void GenerateTextureAssetBundles(bool forceRebuild)
        {
            var allTexs = AutoStreamingSettings.textures;
            Dictionary<string, AutoStreamingSettingsTexture> texMap = new Dictionary<string, AutoStreamingSettingsTexture>();
            foreach (var item in allTexs)
            {
                if (!AssetExists(item.assetPath)) 
                {
                    UnityEngine.Debug.LogError("AutoStreaming: Missing Texture2D/Spriteatlas at path: " + item.assetPath + ". You may need to SyncTextures before continue.");
                    continue;
                }
                texMap.Add(AssetDatabase.AssetPathToGUID(item.assetPath), item);
            }

            //////////////////////////////////////////////////////////////////
            // 1. delete
            List<string> existingABPaths = ASUtilities.GetExistingAssetBundles(ASBuildConstants.k_TextureABPath);
            foreach (var abPath in existingABPaths)
            {
                string abGuid = Path.GetFileNameWithoutExtension(abPath);
                if (texMap.ContainsKey(abGuid) && texMap[abGuid].usePlaceholder)
                    continue;

                File.Delete(abPath);

                string manifestPath = abPath + ".manifest";
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }
            }

            //////////////////////////////////////////////////////////////////
            // 2. generate
            // back up
            bool originalAutoStreaming = PlayerSettings.autoStreaming;

            // Disable AutoStreaming when building AssetBundles for the original textures.
            PlayerSettings.autoStreaming = false;

            List<AssetBundleBuild> abs = new List<AssetBundleBuild>();

            for (int i = 0; i < allTexs.Length; i++)
            {
                var item = allTexs[i];
                if (item.usePlaceholder)
                {
                    // Generate an AssetBundle for the original asset which can be downloaded at runtime.
                    AssetBundleBuild ab = new AssetBundleBuild();
                    ab.assetBundleName = AssetDatabase.AssetPathToGUID(item.assetPath) + ".abas";

                    string texAssetPath = item.assetPath;
                    if (AssetDatabase.GetMainAssetTypeAtPath(item.assetPath) != typeof(Texture2D)
                        && AssetDatabase.GetMainAssetTypeAtPath(item.assetPath) != typeof(SpriteAtlas)
                        && AssetDatabase.GetMainAssetTypeAtPath(item.assetPath) != typeof(Cubemap))
                    {
                        Texture2D texSubObj = LoadTexture2DFromPath(item.assetPath);
                        if (texSubObj == null)
                            continue;

                        if (texSubObj.width <= 0 || texSubObj.height <= 0) 
                        {
                            UnityEngine.Debug.LogWarning("Texture "+ item.assetPath + " with size ("+ texSubObj.width + "," + texSubObj.height + ") has no texture data, will unchecking usePlaceholder.");
                            item.usePlaceholder = false;
                            continue;
                        }

                        Texture2D newTexSubObj = new Texture2D(texSubObj.width, texSubObj.height, texSubObj.format, texSubObj.mipmapCount,false);
                        newTexSubObj.LoadRawTextureData(texSubObj.GetRawTextureData());
                        newTexSubObj.Apply();
                        Directory.CreateDirectory("Assets/AutoStreamingData/TmpTextureAssets");

                        texAssetPath = string.Format("Assets/AutoStreamingData/TmpTextureAssets/{0}.asset", AssetDatabase.AssetPathToGUID(item.assetPath));
                        AssetDatabase.CreateAsset(newTexSubObj, texAssetPath);
                    }

                    ab.assetNames = new string[] { texAssetPath };
                    abs.Add(ab);
                }
            }


            if (abs.Count > 0)
            {
                string absDir = ASUtilities.GetPlatformSpecificResourcePath(ASBuildConstants.k_TextureABPath);
                Directory.CreateDirectory(absDir);

                BuildAssetBundleOptions buildABOption =
                    forceRebuild ? BuildAssetBundleOptions.ForceRebuildAssetBundle : BuildAssetBundleOptions.None;
                buildABOption = (buildABOption | BuildAssetBundleOptions.DisableWriteTypeTree);
                BuildPipeline.BuildAssetBundles(absDir, abs.ToArray(), buildABOption, EditorUserBuildSettings.activeBuildTarget);
            }

            // restore
            PlayerSettings.autoStreaming = originalAutoStreaming;

            // 3. generate file Library/AutoStreamingCache/ASABLut/spriteatlas 
            WriteOutAtlasTag2AbNameFile();

            AssetDatabase.Refresh();
        }

        void GeneratePlaceholders()
        {
            var allTexs = AutoStreamingSettings.textures;
            Dictionary<string, AutoStreamingSettingsTexture> texMap = new Dictionary<string, AutoStreamingSettingsTexture>();
            foreach (var item in allTexs)
            {
                if (!AssetExists(item.assetPath))
                {
                    UnityEngine.Debug.LogError("AutoStreaming: Missing Texture2D/Spriteatlas at path: " + item.assetPath + ". You may need to SyncTextures before continue.");
                    continue;
                }
                texMap.Add(item.assetPath, item);
            }

            // 1. Delete. Do we want to keep the placeholders? May it will be configured to be loaded on demand later.
#if false
            var existingPlaceholders = AutoStreamingSettings.GetExistingPlaceholders(true, true);
            foreach (var placeholderPath in existingPlaceholders)
            {
                string assetPath = AutoStreamingSettings.PlaceholderPathToAssetPath(placeholderPath);
                if (texMap.ContainsKey(assetPath))
                {
                    var texItem = texMap[assetPath];

                    if (texItem.usePlaceholder)
                        continue;
                }

                AssetDatabase.DeleteAsset(placeholderPath);
                UnityEngine.Debug.Log(String.Format("DeleteAsset({0})", placeholderPath));
            }
#endif
            // 2. Generate
            List<string> spriteAtlasArr = new List<string>();
            foreach (var kv in texMap)
            {
                var texItem = kv.Value;
                if (texItem.usePlaceholder && texItem.assetPath.EndsWith(".spriteatlas"))
                    spriteAtlasArr.Add(texItem.assetPath);
            }

            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                AutoStreamingSettings.GenerateTexturePlaceholders();
                GeneratePlaceholdersForSpriteAtlas(spriteAtlasArr);
                stopwatch.Stop();
                UnityEngine.Debug.Log("GeneratePlaceholders cost: " + stopwatch.ElapsedMilliseconds * 0.001f + "s.");
            }

            SyncTextures(true);
        }

        void ConvertLegacySpritePackerToSpriteAtlas()
        {
            AutoStreamingSettings.SyncTextures(false);

            var allTexs = AutoStreamingSettings.textures;
            HashSet<string> asTexs = new HashSet<string>();
            foreach (var item in allTexs)
            {
                if (!AssetExists(item.assetPath))
                {
                    UnityEngine.Debug.LogError("AutoStreaming: Missing Texture2D/Spriteatlas at path: " + item.assetPath + ". You may need to SyncTextures before continue.");
                    continue;
                }
                asTexs.Add(AssetDatabase.AssetPathToGUID(item.assetPath));
            }

            string[] allSprites = AssetDatabase.FindAssets("t:sprite", null);
            Dictionary<string, List<UnityEngine.Object>> atlasMap = new Dictionary<string, List<UnityEngine.Object>>();

            int cnt = 0;
            foreach (var sprite in allSprites)
            {
                EditorUtility.DisplayProgressBar("AutoStreaming", "Converting Legacy SpritePacker: " + cnt + "/" + allSprites.Length, ((float)(cnt++)) / allSprites.Length);

                string tex2DAssetPath = AssetDatabase.GUIDToAssetPath(sprite);

                TextureImporter tex2DImporter = TextureImporter.GetAtPath(tex2DAssetPath) as TextureImporter;
#pragma warning disable CS0618 // Type or member is obsolete
                if (tex2DImporter != null && !string.IsNullOrEmpty(tex2DImporter.spritePackingTag))
                {
                    if (!atlasMap.ContainsKey(tex2DImporter.spritePackingTag))
                    {
                        atlasMap[tex2DImporter.spritePackingTag] = new List<UnityEngine.Object>();
                    }
                   
                    if(asTexs.Contains(sprite))
                        atlasMap[tex2DImporter.spritePackingTag].Add(AssetDatabase.LoadAssetAtPath<Texture2D>(tex2DAssetPath));

                    tex2DImporter.spritePackingTag = "";
                    AssetDatabase.WriteImportSettingsIfDirty(tex2DAssetPath);
                }
#pragma warning restore CS0618 // Type or member is obsolete

            }

            EditorUtility.ClearProgressBar();

            string atlasRoot = "Assets/AutoStreamingData/AutoConvert";
            if (atlasMap.Count > 0)
            {
                Directory.CreateDirectory(atlasRoot);
            }
            foreach (var kv in atlasMap)
            {
                if (kv.Value.Count == 0)
                    continue;

                SpriteAtlas spriteAtlas = new SpriteAtlas();
                spriteAtlas.SetIsVariant(false);
                spriteAtlas.SetIncludeInBuild(true);

                var packSetting = spriteAtlas.GetPackingSettings();
                packSetting.enableRotation = false;
                packSetting.enableTightPacking = false;
                spriteAtlas.SetPackingSettings(packSetting);
                spriteAtlas.Add(kv.Value.ToArray());

                var platformSettings = spriteAtlas.GetPlatformSettings("DefaultTexturePlatform");
                platformSettings.format = TextureImporterFormat.Automatic;
                platformSettings.compressionQuality = (int)TextureCompressionQuality.Normal;
                spriteAtlas.SetPlatformSettings(platformSettings);

                string atlasPath = atlasRoot + "/" + kv.Key + ".spriteatlas";
                string atlasDir = Path.GetDirectoryName(atlasPath);
                if (!Directory.Exists(atlasDir))
                    Directory.CreateDirectory(atlasDir);
                AssetDatabase.CreateAsset(spriteAtlas, atlasRoot + "/" + kv.Key + ".spriteatlas");
            }

            AssetDatabase.Refresh();
            EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnlyAtlas;

            SyncTextures(false);
        }

        void GeneratePlaceholdersForSpriteAtlas(List<string> tex2DAssets)
        {
            float i = 0;
            foreach (var tex2DAsset in tex2DAssets)
            {
                string originalAssetFullPath = Path.GetFullPath(tex2DAsset);
                EditorUtility.DisplayProgressBar("GeneratePlaceholders", "Generate SpriteAtlas: " + i + "/" + tex2DAssets.Count, i++ / (float)(tex2DAssets.Count));
                string placeholderPath = AutoStreamingSettings.AssetPathToPlaceholderPath(tex2DAsset);

                Directory.CreateDirectory(Path.GetDirectoryName(placeholderPath));
                AssetDatabase.CopyAsset(tex2DAsset, placeholderPath);

                SpriteAtlas originalAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(tex2DAsset);
                SpriteAtlas placeholderAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(placeholderPath);

                placeholderAtlas.SetIncludeInBuild(originalAtlas.GetIncludeInBuild());
                placeholderAtlas.SetIsVariant(true);
                placeholderAtlas.SetMasterAtlas(originalAtlas.isVariant ? originalAtlas.GetMasterAtlas() : originalAtlas);
                placeholderAtlas.SetIsPlaceholder(true);
                placeholderAtlas.SetVariantScale(0.125f);

                //prevent sample from nearby sprites
                var setting = placeholderAtlas.GetTextureSettings();
                setting.filterMode = FilterMode.Point;
                placeholderAtlas.SetTextureSettings(setting);
                AssetDatabase.ImportAsset(placeholderPath, ImportAssetOptions.ForceUpdate);
            }
            EditorUtility.ClearProgressBar();
        }
    }
}

#endif  //  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER, Auto generated by AddMacroForInstantGameFiles.exe
