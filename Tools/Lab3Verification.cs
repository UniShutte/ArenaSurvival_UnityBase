// Copy into Assets/Editor in an isolated validation clone, never into the teaching baseline.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Lab3Verification
{
    public static void ImportTmpEssentials()
    {
        string package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage",
            SearchOption.AllDirectories).Single();
        AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
        AssetDatabase.importPackageFailed += (_, error) =>
        {
            Debug.LogError(error);
            EditorApplication.Exit(1);
        };
        AssetDatabase.ImportPackage(package, false);
    }

    public static void PrepareAndRender()
    {
        const string scenePath = "Assets/_Project/Scenes/Arena_01.unity";
        const string prefabPath = "Assets/_Project/Prefabs/Environment/PF_ArenaEntrance.prefab";
        EditorSceneManager.OpenScene(scenePath);
        GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Environment/PF_Wall.prefab");
        GameObject root = new GameObject("PF_ArenaEntrance");
        root.layer = LayerMask.NameToLayer("Environment");
        AddWall(wall, root.transform, "LeftPillar", new Vector3(-3f, 1.5f, 0f), new Vector3(1f, 3f, 1f));
        AddWall(wall, root.transform, "RightPillar", new Vector3(3f, 1.5f, 0f), new Vector3(1f, 3f, 1f));
        AddWall(wall, root.transform, "Lintel", new Vector3(0f, 3.5f, 0f), new Vector3(7f, 1f, 1f));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        GameObject old = GameObject.Find("PF_ArenaEntrance");
        if (old != null)
            Object.DestroyImmediate(old);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(GameObject.Find("Environment").transform);
        instance.transform.position = new Vector3(-12f, 0.5f, -12f);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        foreach (Transform child in instance.transform)
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                throw new System.InvalidOperationException("Nested prefab connection missing: " + child.name);

        Directory.CreateDirectory("Logs/VisualReview");
        ShaderUtil.allowAsyncCompilation = false;
        File.WriteAllLines("Logs/VisualReview/materials.txt",
            Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .SelectMany(r => r.sharedMaterials.Select(m => r.name + " | " + r.transform.position + " | " +
                    (m == null ? "MISSING" : AssetDatabase.GetAssetPath(m) + " | " + m.shader.name))));
        File.WriteAllLines("Logs/VisualReview/terrain-prototypes.txt",
            Terrain.activeTerrain.terrainData.treePrototypes
                .SelectMany(p => p.prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials.Select(m => AssetDatabase.GetAssetPath(p.prefab) + " | " +
                        (m == null ? "MISSING" : AssetDatabase.GetAssetPath(m) + " | " + m.shader.name)))));
        File.WriteAllLines("Logs/VisualReview/terrain-details.txt",
            Terrain.activeTerrain.terrainData.detailPrototypes.Select(p =>
                "mesh=" + p.usePrototypeMesh + " instancing=" + p.useInstancing +
                " mode=" + p.renderMode + " texture=" + AssetDatabase.GetAssetPath(p.prototypeTexture) +
                " prefab=" + AssetDatabase.GetAssetPath(p.prototype) +
                (p.prototype == null ? "" : " materials=" + string.Join(";", p.prototype.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials.Select(m => m == null ? "MISSING" : AssetDatabase.GetAssetPath(m) + " | " + m.shader.name))))));
        GameObject cameraObject = new GameObject("VerificationCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 800f;
        Transform spawn = GameObject.Find("PlayerSpawnPoint").transform;
        Capture(camera, "spawn", spawn.position + Vector3.up * 0.7f,
            spawn.position + Vector3.up * 0.7f + Quaternion.Euler(0f, spawn.eulerAngles.y, 0f) * Vector3.forward * 20f);
        Capture(camera, "arena", new Vector3(12f, 18f, -24f), new Vector3(-2f, 0f, -2f));
        Capture(camera, "entrance", new Vector3(-12f, 3f, -21f), new Vector3(-12f, 2.5f, -12f));
        Capture(camera, "route", new Vector3(-24f, 8f, -40f), new Vector3(-12f, 2f, -16f));
        Capture(camera, "spawn-warm", spawn.position + Vector3.up * 0.7f,
            spawn.position + Vector3.up * 0.7f + Quaternion.Euler(0f, spawn.eulerAngles.y, 0f) * Vector3.forward * 20f);
        Object.DestroyImmediate(cameraObject);
        int missing = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if (missing != 0)
            throw new System.InvalidOperationException("Missing scripts: " + missing);
        Debug.Log("LAB3_VERIFIED: three nested wall instances; no missing scene scripts; screenshots saved.");
    }

    private static void AddWall(GameObject prefab, Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        child.name = name;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = scale;
    }

    private static void Capture(Camera camera, string name, Vector3 position, Vector3 lookAt)
    {
        camera.transform.position = position;
        camera.transform.LookAt(lookAt);
        var texture = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            texture.Create();
            RenderPipeline.SubmitRenderRequest(camera,
                new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/VisualReview/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            texture.Release();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(image);
        }
    }
}
