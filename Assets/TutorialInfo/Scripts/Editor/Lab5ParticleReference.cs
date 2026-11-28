// Editor-only screenshot setup, kept outside the student game repository.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Lab5ParticleReference : EditorWindow
{
    private const string Folder = "Assets/__Lab5ParticleReference";
    private const string PlayerPath = "Assets/_Project/Prefabs/Characters/PF_FirstPersonPlayer.prefab";
    private static Object[] targets;
    private static int selected;
    private static string originalScene;
    private static GameObject reference;
    private static EditorWindow nativeInspector;
    private static readonly string[] Labels =
    {
        "Pistol: MuzzleFlash", "Pistol: Smoke", "Flash material", "Smoke material",
        "Texture import", "Rifle: MuzzleFlash", "Rifle: Smoke", "Shotgun: MuzzleFlash", "Shotgun: Smoke"
    };

    [MenuItem("Tools/Lab5 Particle Reference/Open")]
    public static void Open() { GetWindow<Lab5ParticleReference>("Lab5 Reference").Show(); }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Temporary unsaved copy. Never Apply or save this scene. Native Inspector shows the actual particle values.", MessageType.Info);
        if (GUILayout.Button("Build reference copy")) Build();
        if (targets == null) return;
        for (int i = 0; i < Labels.Length; i++)
        {
            int index = i;
            if (GUILayout.Button((i + 1) + ". " + Labels[i])) Select(index);
        }
        EditorGUILayout.LabelField("Next selection: Ctrl+Shift+J");
        if (GUILayout.Button("Return to original scene"))
        {
            if (nativeInspector != null) nativeInspector.Close();
            Selection.activeObject = null;
            if (!string.IsNullOrEmpty(originalScene)) EditorSceneManager.OpenScene(originalScene);
            targets = null;
        }
    }

    [MenuItem("Tools/Lab5 Particle Reference/Next %#j")]
    private static void Next() { if (targets != null) Select((selected + 1) % targets.Length); }

    private static void Select(int index)
    {
        selected = index;
        Selection.activeObject = targets[index];
        EditorGUIUtility.PingObject(targets[index]);
    }

    private static void Build()
    {
        if (reference != null) throw new System.InvalidOperationException("Reference already exists.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save your own changes first.");
        if (AssetDatabase.IsValidFolder(Folder)) throw new System.InvalidOperationException("Reference folder already exists.");
        originalScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.CreateFolder("Assets", "__Lab5ParticleReference");
        Texture2D flashTexture = MakeTexture("T_MuzzleFlash", false);
        Texture2D smokeTexture = MakeTexture("T_MuzzleSmoke", true);
        Material flashMaterial = MakeMaterial("M_MuzzleFlash_Additive", flashTexture, true);
        Material smokeMaterial = MakeMaterial("M_MuzzleSmoke_Alpha", smokeTexture, false);
        reference = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
        PrefabUtility.UnpackPrefabInstance(reference, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        reference.name = "PF_FirstPersonPlayer_REFERENCE_UNSAVED";
        Transform pistol = Find("PistolMuzzle");
        Transform rifle = Find("RifleMuzzle");
        Transform shotgun = Find("ShotgunMuzzle");
        foreach (Transform old in reference.GetComponentsInChildren<Transform>(true).Where(t => t.name == "MuzzleFlash").ToArray())
            DestroyImmediate(old.gameObject);
        var flashObject = new GameObject("MuzzleFlash");
        flashObject.transform.SetParent(pistol, false);
        ParticleSystem flash = flashObject.AddComponent<ParticleSystem>();
        var smokeObject = new GameObject("Smoke");
        smokeObject.transform.SetParent(flash.transform, false);
        ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
        ConfigureFlash(flash, flashMaterial);
        ConfigureSmoke(smoke, smokeMaterial);
        ParticleSystem rifleFlash = Instantiate(flashObject, rifle, false).GetComponent<ParticleSystem>();
        rifleFlash.name = "MuzzleFlash";
        ParticleSystem rifleSmoke = rifleFlash.transform.Find("Smoke").GetComponent<ParticleSystem>();
        SetDifferences(rifleFlash, rifleSmoke, false);
        ParticleSystem shotgunFlash = Instantiate(flashObject, shotgun, false).GetComponent<ParticleSystem>();
        shotgunFlash.name = "MuzzleFlash";
        ParticleSystem shotgunSmoke = shotgunFlash.transform.Find("Smoke").GetComponent<ParticleSystem>();
        SetDifferences(shotgunFlash, shotgunSmoke, true);
        foreach (ParticleSystem system in reference.GetComponentsInChildren<ParticleSystem>(true)) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        targets = new Object[] { flash.gameObject, smoke.gameObject, flashMaterial, smokeMaterial, flashTexture,
            rifleFlash.gameObject, rifleSmoke.gameObject, shotgunFlash.gameObject, shotgunSmoke.gameObject };
        Validate(flash, smoke, rifleFlash, rifleSmoke, shotgunFlash, shotgunSmoke);
        Select(0);
        nativeInspector = CreateInstance(System.Type.GetType("UnityEditor.InspectorWindow, UnityEditor")) as EditorWindow;
        nativeInspector.titleContent = new GUIContent("Inspector");
        nativeInspector.position = new Rect(1050f, 60f, 800f, 1240f);
        nativeInspector.ShowUtility();
    }

    private static Transform Find(string name)
    {
        Transform value = reference.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        if (value.parent.name != "AnimatedRoot") throw new System.InvalidOperationException(name + " is not under AnimatedRoot.");
        return value;
    }

    private static void ConfigureFlash(ParticleSystem ps, Material material)
    {
        var m = ps.main;
        m.duration = 0.1f; m.loop = false; m.prewarm = false; m.playOnAwake = false;
        m.startDelay = 0f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.03f, 0.045f);
        m.startSpeed = 0f; m.startSize3D = false; m.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.13f);
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.startColor = (Color)new Color32(255, 225, 160, 255); m.gravityModifier = 0f;
        m.simulationSpace = ParticleSystemSimulationSpace.Local; m.scalingMode = ParticleSystemScalingMode.Local;
        m.simulationSpeed = 1f; m.stopAction = ParticleSystemStopAction.None; m.maxParticles = 8;
        var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0f; emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1, (short)1, 1, 0.01f) });
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        color.color = MakeGradient(new Color(1f, 0.8f, 0.5f), new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.25f), new GradientAlphaKey(0f, 1f) });
        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.15f)));
        ConfigureRenderer(ps, material);
    }

    private static void ConfigureSmoke(ParticleSystem ps, Material material)
    {
        var m = ps.main;
        m.duration = 0.5f; m.loop = false; m.prewarm = false; m.playOnAwake = false; m.startDelay = 0f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.35f); m.startSpeed = 0f;
        m.startSize3D = false; m.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.045f);
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); m.startColor = (Color)new Color32(165, 165, 155, 65);
        m.gravityModifier = 0f; m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.scalingMode = ParticleSystemScalingMode.Local; m.simulationSpeed = 1f; m.stopAction = ParticleSystemStopAction.None; m.maxParticles = 32;
        var emission = ps.emission; emission.enabled = false; emission.rateOverTime = 0f; emission.rateOverDistance = 0f; emission.SetBursts(new ParticleSystem.Burst[0]);
        var shape = ps.shape; shape.enabled = false;
        var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f); velocity.y = new ParticleSystem.MinMaxCurve(0.06f, 0.12f); velocity.z = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
        var color = ps.colorOverLifetime; color.enabled = true;
        color.color = MakeGradient(Color.white, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1.2f), new Keyframe(1f, 2.2f)));
        var rotation = ps.rotationOverLifetime; rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-20f * Mathf.Deg2Rad, 20f * Mathf.Deg2Rad);
        ConfigureRenderer(ps, material);
    }

    private static ParticleSystem.MinMaxGradient MakeGradient(Color end, GradientAlphaKey[] alpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(end, 1f) }, alpha);
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void ConfigureRenderer(ParticleSystem ps, Material material)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.sharedMaterial = material;
    }

    private static void SetDifferences(ParticleSystem flash, ParticleSystem smoke, bool shotgun)
    {
        var f = flash.main;
        f.startLifetime = shotgun ? new ParticleSystem.MinMaxCurve(0.045f, 0.065f) : new ParticleSystem.MinMaxCurve(0.025f, 0.035f);
        f.startSize = shotgun ? new ParticleSystem.MinMaxCurve(0.18f, 0.24f) : new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
        f.startColor = (Color)(shotgun ? new Color32(255, 215, 140, 255) : new Color32(255, 235, 185, 255));
        var s = smoke.main;
        s.startLifetime = shotgun ? new ParticleSystem.MinMaxCurve(0.35f, 0.5f) : new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
        s.startSize = shotgun ? new ParticleSystem.MinMaxCurve(0.05f, 0.08f) : new ParticleSystem.MinMaxCurve(0.035f, 0.055f);
        s.startColor = (Color)new Color32(165, 165, 155, shotgun ? (byte)85 : (byte)75);
    }

    private static Texture2D MakeTexture(string name, bool smoke)
    {
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float px = (x - 127.5f) / 127.5f, py = (y - 127.5f) / 127.5f;
                float radius = Mathf.Sqrt(px * px + py * py);
                float angle = Mathf.Atan2(py, px);
                float edge = smoke ? 0.82f + 0.12f * Mathf.PerlinNoise(x * 0.025f, y * 0.025f) : 0.42f + 0.4f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 3f)), 8f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius / edge), smoke ? 1.4f : 1.1f);
                if (smoke) alpha *= 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.045f, y * 0.045f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        texture.Apply();
        string path = Folder + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        string external = Path.GetFullPath(Path.Combine(Application.dataPath, "../../5/Images/Particles_Rework/Textures"));
        Directory.CreateDirectory(external); File.WriteAllBytes(Path.Combine(external, name + ".png"), texture.EncodeToPNG());
        DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default; importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 256; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material MakeMaterial(string name, Texture2D texture, bool additive)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.name = name; material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        material.SetFloat("_ZWrite", 0f); material.SetColor("_BaseColor", Color.white); material.SetTexture("_BaseMap", texture);
        material.SetOverrideTag("RenderType", "Transparent"); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, Folder + "/" + name + ".mat");
        return material;
    }

    private static void Validate(params ParticleSystem[] systems)
    {
        for (int i = 0; i < systems.Length; i += 2)
        {
            ParticleSystem flash = systems[i], smoke = systems[i + 1];
            if (flash.main.loop || flash.main.playOnAwake || flash.main.startSpeed.constant != 0f || flash.main.maxParticles != 8 || flash.emission.burstCount != 1 ||
                smoke.main.loop || smoke.main.playOnAwake || smoke.emission.enabled || smoke.main.simulationSpace != ParticleSystemSimulationSpace.World || smoke.main.maxParticles != 32 ||
                smoke.transform.parent != flash.transform || flash.transform.localPosition != Vector3.zero || smoke.transform.localScale != Vector3.one)
                throw new System.InvalidOperationException("Reference validation failed for pair " + i);
        }
        string external = Path.GetFullPath(Path.Combine(Application.dataPath, "../../5/Verification_Particles_2026-11-02"));
        Directory.CreateDirectory(external);
        File.WriteAllText(Path.Combine(external, "reference-validation.txt"), "PASS: three native ParticleSystem pairs, burst/loop/speed/world-space/hierarchy contracts checked. No source prefab saved.\n");
        Debug.Log("Lab5 screenshot reference: validated three particle pairs. This scene is intentionally unsaved.");
    }
}
