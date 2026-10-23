// Розмістити у: Assets/Editor/LevelPrefabSetBuilder.cs
// Запуск: меню  Tools > Level > Build Prefab Set
// Скрипт можна запускати повторно: матеріали, меш і префаби оновлюються на місці.

using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelPrefabSetBuilder
{
    const string PrefabDir = "Assets/Prefabs/Level";
    const string MaterialDir = "Assets/Materials/Level";
    const string MeshDir = "Assets/Meshes/Level";

    const string TagEnvironment = "Environment";
    const string TagHazard = "Hazard";

    // ---------- Палітра (Neon Geometry) ----------
    // Безпечні платформи: темні з холодними акцентами
    static readonly Color PlatformBase = new Color(0.04f, 0.06f, 0.11f);
    static readonly Color PlatformGlow = new Color(0.00f, 0.55f, 0.90f) * 0.6f;   // холодний блакитний, слабкий
    // Небезпека: контрастні рожеві/червоні
    static readonly Color HazardBase = new Color(0.30f, 0.00f, 0.10f);
    static readonly Color HazardGlow = new Color(1.00f, 0.08f, 0.45f) * 2.5f;   // неоново-рожевий
    static readonly Color SpikeBase = new Color(0.35f, 0.00f, 0.02f);
    static readonly Color SpikeGlow = new Color(1.00f, 0.05f, 0.08f) * 2.5f;   // червоний
    // Декор: приглушений
    static readonly Color DecorBase = new Color(0.09f, 0.09f, 0.14f);
    static readonly Color DecorGlow = new Color(0.20f, 0.20f, 0.35f) * 0.35f;  // тьмяний сіро-фіолетовий

    [MenuItem("Tools/Level/Build Prefab Set")]
    public static void Build()
    {
        EnsureFolder(PrefabDir);
        EnsureFolder(MaterialDir);
        EnsureFolder(MeshDir);
        EnsureTag(TagEnvironment);
        EnsureTag(TagHazard);

        Material mPlatform = MakeMaterial("M_Platform", PlatformBase, PlatformGlow, 0.6f, 0.4f);
        Material mHazard = MakeMaterial("M_HazardBlock", HazardBase, HazardGlow, 0.3f, 0.2f);
        Material mSpike = MakeMaterial("M_Spike", SpikeBase, SpikeGlow, 0.3f, 0.2f);
        Material mDecor = MakeMaterial("M_DecorativeBlock", DecorBase, DecorGlow, 0.1f, 0.1f);

        Mesh pyramid = BuildPyramidMesh();

        // --- Platform: безпечна, суцільний колайдер ---
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Platform";
            go.tag = TagEnvironment;
            go.transform.localScale = new Vector3(4, 1, 4);              // цілі числа
            go.GetComponent<MeshRenderer>().sharedMaterial = mPlatform;
            // BoxCollider з примітива = 1x1x1 локально = точно по моделі
            Save(go);
        }

        // --- HazardBlock: колайдер МЕНШИЙ за модель, trigger ---
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HazardBlock";
            go.tag = TagHazard;
            go.transform.localScale = new Vector3(1, 1, 1);
            go.GetComponent<MeshRenderer>().sharedMaterial = mHazard;
            var col = go.GetComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 0.8f, 0.8f);                    // 80% від моделі
            col.isTrigger = true;
            Save(go);
        }

        // --- Spike: піраміда, колайдер вужчий і нижчий за видиму модель ---
        {
            var go = new GameObject("Spike");
            go.tag = TagHazard;
            go.AddComponent<MeshFilter>().sharedMesh = pyramid;
            go.AddComponent<MeshRenderer>().sharedMaterial = mSpike;
            go.transform.localScale = new Vector3(1, 1, 1);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);                     // пивот меша - центр основи
            col.size = new Vector3(0.5f, 0.7f, 0.5f);                  // менше за піраміду 1x1x1
            col.isTrigger = true;
            Save(go);
        }

        // --- DecorativeBlock: приглушений, без колайдера ---
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DecorativeBlock";
            go.tag = TagEnvironment;
            go.transform.localScale = new Vector3(2, 2, 2);
            go.GetComponent<MeshRenderer>().sharedMaterial = mDecor;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());      // декор не блокує гравця
            Save(go);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Level prefab set створено в " + PrefabDir);
    }

    // ---------- Допоміжні методи ----------

    static void Save(GameObject go)
    {
        string path = $"{PrefabDir}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static Material MakeMaterial(string name, Color baseColor, Color emission, float smoothness, float metallic)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");                 // Built-in RP
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = baseColor;                                            // _BaseColor / _Color
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Піраміда 1x1 в основі, висота 1, пивот у центрі основи. Плоскі грані (окремі вершини).
    static Mesh BuildPyramidMesh()
    {
        string path = $"{MeshDir}/SpikePyramid.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = "SpikePyramid" };
        else mesh.Clear();

        Vector3 a = new Vector3(-0.5f, 0, -0.5f);
        Vector3 b = new Vector3(0.5f, 0, -0.5f);
        Vector3 c = new Vector3(0.5f, 0, 0.5f);
        Vector3 d = new Vector3(-0.5f, 0, 0.5f);
        Vector3 t = new Vector3(0f, 1, 0f);

        var v = new Vector3[]
        {
            a, t, b,   // передня
            b, t, c,   // права
            c, t, d,   // задня
            d, t, a,   // ліва
            a, b, c, d // низ
        };
        var tris = new int[]
        {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            12, 14, 13,
            12, 15, 14
        };
        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (isNew) AssetDatabase.CreateAsset(mesh, path);
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void EnsureTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}