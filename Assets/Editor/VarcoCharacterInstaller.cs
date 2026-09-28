using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    // Imports visual assets only; the existing NPC roots retain their dialogue identities.
    public static class VarcoCharacterInstaller
    {
        const string Root = "Assets/Art/Characters/Varco";
        static readonly string[] Characters = { "CaoCao", "Seolpyeong", "Duseok", "Caimao", "Zhangyun" };

        public static void ApplyAvailableBatch()
        {
            EditorSceneManager.OpenScene(FirstInvestigationBuilder.ScenePath);
            ApplyAvailable();
            RenderAssetPreview();
        }

        [MenuItem("적벽추리/바르코 인물 모델 적용")]
        public static void ApplyAvailable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before importing characters.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != FirstInvestigationBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save the Investigation scene first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string character in Characters)
            {
                string folder = Root + "/" + character;
                string modelPath = folder + "/" + character + ".fbx";
                if (!File.Exists(modelPath)) continue;
                var npc = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == character);
                var prefab = ImportVisual(character, folder, modelPath);
                var oldVisual = npc.Find("VarcoVisual");
                if (oldVisual != null) Undo.DestroyObjectImmediate(oldVisual.gameObject);
                foreach (var renderer in npc.GetComponentsInChildren<Renderer>(true))
                {
                    Undo.RecordObject(renderer, "Replace prototype character");
                    renderer.enabled = character == "Duseok"
                        && (renderer.name == "SpearShaft" || renderer.name == "SpearHead");
                }
                foreach (var collider in npc.GetComponentsInChildren<Collider>(true))
                {
                    Undo.RecordObject(collider, "Replace prototype collision");
                    collider.enabled = false;
                }
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npc);
                Undo.RegisterCreatedObjectUndo(visual, "Import Varco character");
                visual.name = "VarcoVisual";
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localPosition = character == "CaoCao"
                    ? npc.InverseTransformPoint(new Vector3(41.4f, 0, .95f)) : Vector3.zero;
                var capsule = npc.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = Undo.AddComponent<CapsuleCollider>(npc.gameObject);
                Undo.RecordObject(capsule, "Set character interaction capsule");
                capsule.enabled = true;
                capsule.isTrigger = false;
                capsule.height = character == "CaoCao" ? 2.1f : 1.9f;
                capsule.radius = .43f;
                capsule.direction = 1;
                capsule.center = visual.transform.localPosition + Vector3.up * capsule.height * .5f;
                Debug.Log("VARCO_APPLIED " + character);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Investigation scene.");
        }

        static GameObject ImportVisual(string character, string folder, string modelPath)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            string textureFolder = folder + "/Textures";
            if (!AssetDatabase.IsValidFolder(textureFolder)) AssetDatabase.CreateFolder(folder, "Textures");
            importer.ExtractTextures(textureFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            importer.SaveAndReimport();
            var packed = PackSurfaceTexture(textureFolder, character);
            string normalPath = textureFolder + "/normal.png";
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Cannot import " + modelPath);
            var wrapper = new GameObject(character + "Visual");
            try
            {
                var instance = UnityEngine.Object.Instantiate(model, wrapper.transform);
                instance.name = "Model";
                instance.transform.localPosition = Vector3.zero;
                // Retain the FBX axis conversion (VARCO exports Z-up).
                instance.transform.localRotation = Quaternion.Euler(0, 180, 0) * instance.transform.localRotation;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("No mesh in " + modelPath);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float height = character == "CaoCao" ? 2.15f : character == "Seolpyeong" ? 1.83f : 1.95f;
                if (bounds.size.y < .001f) throw new InvalidOperationException("Invalid mesh bounds: " + character);
                float scale = height / bounds.size.y;
                instance.transform.localScale *= scale;
                instance.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
                int materialIndex = 0;
                var converted = new System.Collections.Generic.Dictionary<Material, Material>();
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null) throw new InvalidOperationException("Missing source material: " + character);
                        if (!converted.TryGetValue(source, out var material))
                        {
                            string path = folder + "/" + character + "_" + materialIndex++ + ".mat";
                            material = AssetDatabase.LoadAssetAtPath<Material>(path);
                            if (material == null)
                            {
                                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                                AssetDatabase.CreateAsset(material, path);
                            }
                            var albedo = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : source.mainTexture;
                            if (albedo == null) throw new InvalidOperationException("Missing albedo texture: " + character);
                            material.SetTexture("_BaseMap", albedo);
                            material.SetColor("_BaseColor", Color.white);
                            material.SetFloat("_Metallic", 0);
                            material.SetFloat("_Smoothness", .25f);
                            if (packed != null)
                            {
                                material.SetTexture("_MetallicGlossMap", packed);
                                material.SetTexture("_OcclusionMap", packed);
                                material.SetFloat("_Metallic", 1);
                                material.SetFloat("_Smoothness", SurfaceSmoothness(character));
                                material.SetFloat("_OcclusionStrength", .4f);
                                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                                material.EnableKeyword("_OCCLUSIONMAP");
                            }
                            if (normal != null)
                            {
                                material.SetTexture("_BumpMap", normal);
                                material.SetFloat("_BumpScale", SurfaceNormalStrength(character));
                                material.EnableKeyword("_NORMALMAP");
                            }
                            EditorUtility.SetDirty(material);
                            converted.Add(source, material);
                            Debug.Log("VARCO_MATERIAL " + character + " albedo=" + AssetDatabase.GetAssetPath(albedo));
                        }
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                }
                return PrefabUtility.SaveAsPrefabAsset(wrapper, folder + "/" + character + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }

        static float SurfaceSmoothness(string character) => character == "Seolpyeong" ? .16f
            : character == "Duseok" ? .28f : .38f;

        static float SurfaceNormalStrength(string character) => character == "Seolpyeong" ? .18f : .28f;

        [MenuItem("적벽추리/바르코 인물 표면 다듬기")]
        public static void PolishSurfaceMaterials()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before polishing materials.");
            foreach (string character in Characters)
            {
                string folder = Root + "/" + character;
                var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/" + character + "_0.mat");
                if (material == null) continue;
                var packed = PackSurfaceTexture(folder + "/Textures", character);
                Undo.RecordObject(material, "Polish Varco surface");
                material.SetFloat("_Smoothness", SurfaceSmoothness(character));
                material.SetFloat("_BumpScale", SurfaceNormalStrength(character));
                material.SetFloat("_OcclusionStrength", .4f);
                if (packed != null)
                {
                    material.SetTexture("_MetallicGlossMap", packed);
                    material.SetTexture("_OcclusionMap", packed);
                }
                EditorUtility.SetDirty(material);
                Debug.Log("VARCO_POLISHED " + character);
            }
            AssetDatabase.SaveAssets();
        }

        static Texture2D PackSurfaceTexture(string folder, string character)
        {
            string sourcePath = folder + "/orm.png";
            if (!File.Exists(sourcePath)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(sourcePath))) throw new IOException("Invalid ORM texture.");
                var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var orm = pixels[i];
                    // VARCO/glTF: R=occlusion, G=roughness, B=metallic.
                    // URP: metallic in R, occlusion in G, smoothness in A.
                    // AI-generated metal masks can spill onto skin and cloth. Reduce that response;
                    // the clerk's entire outfit is non-metallic.
                    float metalStrength = character == "Seolpyeong" ? 0 : character == "Duseok" ? .35f : .65f;
                    pixels[i] = new Color32((byte)(orm.b * metalStrength), orm.r, 0, (byte)(255 - orm.g));
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                string packedPath = folder + "/MetallicOcclusionSmoothness.png";
                File.WriteAllBytes(packedPath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(packedPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(packedPath);
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        // Authoring preview only. Does not run the game, dialogue, or investigation state.
        public static void RenderAssetPreview()
        {
            var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .6f, .6f);
            int count = 0;
            foreach (string character in Characters)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + character + "/" + character + ".prefab");
                if (prefab == null) continue;
                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.transform.position = new Vector3(count * 1.6f, 0, 0);
                count++;
            }
            var key = new GameObject("PreviewKey").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 2;
            key.transform.rotation = Quaternion.Euler(35, -25, 0);
            var fill = new GameObject("PreviewFill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .65f;
            fill.transform.rotation = Quaternion.Euler(20, 150, 0);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.22f, .24f, .27f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
            int width = Mathf.Max(600, count * 420);
            var target = new RenderTexture(width, 800, 24);
            var pixels = new Texture2D(width, 800, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.aspect = width / 800f;
                var center = new Vector3((count - 1) * .8f, 1.1f, 0);
                Directory.CreateDirectory("Logs/VarcoPreviews");
                foreach (int side in new[] { -1, 1 })
                {
                    camera.transform.position = center + new Vector3(0, 0, side * 6);
                    camera.transform.LookAt(center);
                    // Warm the SRP material/texture bindings after synchronous asset imports.
                    camera.Render();
                    camera.Render();
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, width, 800), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes("Logs/VarcoPreviews/" + (side == -1 ? "front" : "back") + ".png", pixels.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
