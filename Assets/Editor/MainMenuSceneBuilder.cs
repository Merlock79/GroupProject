#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Редакторний інструмент: Tools > Loop Runner > Build MainMenu Scene.
/// Розмістити в Assets/Editor/MainMenuSceneBuilder.cs.
/// Створює сцену MainMenu, Canvas, фон, заголовок, три кнопки,
/// прив'язує події та ставить сцену першою в Build Settings.
/// </summary>
public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string TestLevelPath = "Assets/Scenes/TestLevel.unity";
    private const string BgTexturePath = "Assets/UI/MainMenu/NeonGrid.png";

    private static readonly Color Cyan = new Color(0.1f, 0.95f, 1f);
    private static readonly Color Magenta = new Color(1f, 0.2f, 0.8f);
    private static readonly Color DarkBg = new Color(0.03f, 0.02f, 0.1f);

    [MenuItem("Tools/Loop Runner/Build MainMenu Scene")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(Path.GetDirectoryName(BgTexturePath));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Камера
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = DarkBg;
        cam.orthographic = true;

        // Canvas + Scaler
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        // Фон
        var bg = CreateImage("Background", canvasGo.transform, DarkBg);
        Stretch(bg.rectTransform);
        var grid = CreateImage("NeonGrid", canvasGo.transform, Color.white);
        Stretch(grid.rectTransform);
        var gridSprite = CreateGridSprite();
        if (gridSprite != null)
        {
            grid.sprite = gridSprite;
            grid.type = Image.Type.Tiled;
            grid.pixelsPerUnitMultiplier = 1f;
        }
        grid.raycastTarget = false;
        bg.raycastTarget = false;

        // Заголовок
        var title = CreateText("Title", canvasGo.transform, "LOOP RUNNER", 160, Cyan);
        title.fontStyle = FontStyles.Bold;
        var tRect = title.rectTransform;
        tRect.anchorMin = tRect.anchorMax = new Vector2(0.5f, 0.5f);
        tRect.sizeDelta = new Vector2(1500, 240);
        tRect.anchoredPosition = new Vector2(0, 300);
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(Cyan, Cyan, Magenta, Magenta);
        title.color = Color.white;

        // Кнопки
        var sprite = FindButtonSprite();
        var play = CreateButton("PlayButton", canvasGo.transform, "ГРАТИ", sprite, new Vector2(520, 130), new Vector2(0, 20), 64, Cyan);
        var settings = CreateButton("SettingsButton", canvasGo.transform, "НАЛАШТУВАННЯ", sprite, new Vector2(400, 90), new Vector2(0, -130), 40, Magenta);
        var exit = CreateButton("ExitButton", canvasGo.transform, "ВИХІД", sprite, new Vector2(400, 90), new Vector2(0, -250), 40, Magenta);

        // Скрипт MainMenu
        var menuGo = new GameObject("MainMenu", typeof(MainMenu));
        var menu = menuGo.GetComponent<MainMenu>();
        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(exit.onClick, menu.Exit);
        // "Налаштування" — навмисно без обробника.

        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        Debug.Log("[MainMenuSceneBuilder] Сцену MainMenu створено: " + ScenePath);
    }

    // ---------- допоміжні методи ----------

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static Button CreateButton(string name, Transform parent, string label, Sprite sprite,
        Vector2 size, Vector2 pos, float fontSize, Color accent)
    {
        var img = CreateImage(name, parent, sprite != null ? Color.white : new Color(accent.r, accent.g, accent.b, 0.15f));
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        if (sprite == null)
        {
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(3, -3);
        }

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = colors;

        var txt = CreateText("Label", img.transform, label, fontSize, Color.white);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform);
        return btn;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Шукає спрайт кнопки з пакета Simple Button Set 02.</summary>
    private static Sprite FindButtonSprite()
    {
        var folders = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && AssetDatabase.IsValidFolder(p)
                        && p.IndexOf("Simple Button Set", System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        if (folders.Length == 0)
        {
            Debug.LogWarning("[MainMenuSceneBuilder] Simple Button Set 02 не знайдено в проєкті. " +
                             "Імпортуйте пакет через Package Manager > My Assets і перезапустіть інструмент, " +
                             "поки що використано запасний стиль кнопок.");
            return null;
        }
        var guid = AssetDatabase.FindAssets("button t:Sprite", folders).FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
    }

    /// <summary>Процедурна неонова сітка для фону (Neon Geometry / Digital World).</summary>
    private static Sprite CreateGridSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool line = x < 2 || y < 2;
            tex.SetPixel(x, y, line ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.18f) : new Color(0, 0, 0, 0));
        }
        tex.Apply();
        File.WriteAllBytes(BgTexturePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(BgTexturePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(BgTexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.spriteBorder = Vector4.zero;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BgTexturePath);
    }

    /// <summary>MainMenu — індекс 0 у Build Settings; TestLevel (якщо існує) — далі.</summary>
    private static void UpdateBuildSettings()
    {
        var list = EditorBuildSettings.scenes
            .Where(s => s.path != ScenePath)
            .ToList();
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        if (File.Exists(TestLevelPath) && list.All(s => s.path != TestLevelPath))
            list.Add(new EditorBuildSettingsScene(TestLevelPath, true));
        else if (!File.Exists(TestLevelPath))
            Debug.LogWarning("[MainMenuSceneBuilder] Сцени TestLevel ще немає: створіть її та додайте до Build Settings, " +
                             "або змініть назву в полі Test Level Scene Name компонента MainMenu.");
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
