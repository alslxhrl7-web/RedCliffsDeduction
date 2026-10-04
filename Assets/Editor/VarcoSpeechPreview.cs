using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class VarcoSpeechPreview
    {
        [MenuItem("적벽추리/입모양 확대 미리보기")]
        public static void Render()
        {
            var motions = Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsSortMode.None);
            var preview = EditorSceneManager.NewPreviewScene();
            var sheet = new Texture2D(300 * 9, 600, TextureFormat.RGB24, false);
            var target = new RenderTexture(300, 300, 24);
            try
            {
                var camera = new GameObject("SpeechPreviewCamera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, preview); camera.scene = preview;
                camera.orthographic = true; camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.2f, .22f, .25f);
                camera.targetTexture = target; camera.aspect = 1;
                var light = new GameObject("SpeechPreviewLight").AddComponent<Light>();
                SceneManager.MoveGameObjectToScene(light.gameObject, preview);
                light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 31;
                light.transform.rotation = Quaternion.Euler(30, -25, 0);
                foreach (var motion in motions)
                {
                    var clone = Object.Instantiate(motion.gameObject);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                    var sample = clone.GetComponent<VarcoIdleMotion>(); sample.InitializeForPreview();
                    var filter = clone.GetComponentInChildren<MeshFilter>();
                    float min = float.PositiveInfinity, max = float.NegativeInfinity;
                    foreach (var p in filter.sharedMesh.vertices)
                    {
                        float y = clone.transform.InverseTransformPoint(filter.transform.TransformPoint(p)).y;
                        min = Mathf.Min(min, y); max = Mathf.Max(max, y);
                    }
                    float mouthY = min + (max - min) * VarcoIdleMotion.MouthHeightFraction(sample.Profile);
                    var center = clone.transform.TransformPoint(new Vector3(0, mouthY, 0));
                    camera.orthographicSize = (max - min) * clone.transform.lossyScale.y * .09f;
                    camera.transform.position = center + Vector3.back * 7; camera.transform.LookAt(center);
                    for (int row = 0; row < 2; row++)
                    {
                        sample.StartSpeech(row == 0 ? "" : "아", -.07f); sample.Sample(0);
                        camera.Render(); camera.Render(); camera.Render(); RenderTexture.active = target;
                        sheet.ReadPixels(new Rect(0, 0, 300, 300), sample.Profile * 300, row == 0 ? 300 : 0);
                    }
                    Object.DestroyImmediate(clone);
                }
                sheet.Apply(); Directory.CreateDirectory("Logs/VarcoPreviews");
                File.WriteAllBytes("Logs/VarcoPreviews/speech-closeups.png", sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(sheet);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
