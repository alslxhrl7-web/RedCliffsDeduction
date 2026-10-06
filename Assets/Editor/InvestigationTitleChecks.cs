using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RedCliffMystery.Investigation;

public static class InvestigationTitleChecks
{
    [MenuItem("적벽추리/타이틀/배경 가져오기 설정")]
    public static void ImportBackground()
    {
        const string path = "Assets/Resources/Title/RedCliffsTitleBackground.png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();

        // Logo and menu brush are drawn 1:1 by RawImage, so keep their authored size and clean alpha edges.
        foreach (var overlay in new[] { "Assets/Resources/Title/RedCliffsTitleLogo.png", "Assets/Resources/Title/RedCliffsTitleBrush.png" }) {
            AssetDatabase.ImportAsset(overlay);
            var art = (TextureImporter)AssetImporter.GetAtPath(overlay);
            if (art == null) continue;
            art.textureType = TextureImporterType.Default;
            art.sRGBTexture = true;
            art.alphaIsTransparency = true;
            art.npotScale = TextureImporterNPOTScale.None;
            art.mipmapEnabled = false;
            art.isReadable = false;
            art.wrapMode = TextureWrapMode.Clamp;
            art.maxTextureSize = 2048;
            art.textureCompression = TextureImporterCompression.CompressedHQ;
            art.SaveAndReimport();
        }
    }

    [MenuItem("적벽추리/타이틀/화면과 버튼 확인")]
    public static void Capture()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play Mode first");
        var title = UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
        var session = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        Require(title != null && session != null, "Missing title or session");
        Require(session.BlocksMovement && !session.CanProcessInvestigationInput, "Movement not locked");
        Require(!session.view.gameObject.activeSelf, "Investigation HUD visible on title");
        Require(Resources.Load<Texture2D>("Title/RedCliffsTitleBackground") != null, "Cover art missing");
        var labels = title.GetComponentsInChildren<UnityEngine.UI.Text>();
        Require(labels.Any(t => t.text == InvestigationTitleScreen.GameTitle)
            || (title.TitleLogo != null && title.TitleLogo.texture != null), "Title missing");
        Require(Resources.Load<Font>("Title/RedCliffsSerif-SemiBold") != null, "Title font missing");
        Require(labels.Any(t => t.text == InvestigationTitleScreen.Subtitle), "Subtitle missing");
        Canvas.ForceUpdateCanvases();
        foreach (var button in new[] { title.StartButton, title.QuitButton }) {
            Require(button.interactable, "Button disabled");
            var rect = (RectTransform)button.transform;
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Require(center.x > 0 && center.x < Screen.width && center.y > 0 && center.y < Screen.height, "Button outside screen");
            var pointer = new PointerEventData(EventSystem.current) { position = center };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button, "Button covered");
        }
        Directory.CreateDirectory("Logs/TitleScreen");
        ScreenCapture.CaptureScreenshot("Logs/TitleScreen/title-screen.png");
        File.WriteAllText("Logs/TitleScreen/check.txt", "PASS title, subtitle, cover art, movement lock, hidden HUD and both button hit targets\nResolution: " + Screen.width + "x" + Screen.height);
    }

    [MenuItem("적벽추리/타이틀/시작 버튼 확인")]
    public static void StartGame()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
        Require(screen != null, "Missing title");
        screen.StartButton.onClick.Invoke();
        var session = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        Require(session.view.gameObject.activeInHierarchy, "HUD not restored");
        Require(session.view.title.text == "조조", "Opening not shown");
        Require(session.view.choices.GetComponentInChildren<UnityEngine.UI.InputField>() != null, "Name input missing");
        Require(session.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "저장한 조사 불러오기"), "Load option missing");
        File.AppendAllText("Logs/TitleScreen/check.txt", "\nPASS start button opens Cao Cao name entry and save load option");
    }

    [MenuItem("적벽추리/타이틀/종료 버튼 확인")]
    public static void QuitGame()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
        Require(screen != null, "Missing title");
        screen.QuitButton.onClick.Invoke();
    }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
