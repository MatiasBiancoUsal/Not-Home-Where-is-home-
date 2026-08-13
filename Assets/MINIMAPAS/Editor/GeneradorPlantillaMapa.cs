using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeneradorPlantillaMapa : EditorWindow
{
    private const int LongestSidePixels = 2048;
    private const float BoundsPadding = 1f;

    [MenuItem("Not Home/Minimapas/Generar plantilla de la zona abierta")]
    private static void OpenWindow()
    {
        GetWindow<GeneradorPlantillaMapa>("Plantilla de mapa");
    }

    private void OnGUI()
    {
        Scene scene = SceneManager.GetActiveScene();

        EditorGUILayout.LabelField("Generador de plantilla de minimapa", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Abrir la escena de la zona que se quiere capturar. La herramienta usa los colliders " +
            "estaticos para obtener limites exactos, oculta temporalmente al Player y objetos dinamicos, " +
            "genera un PNG y lo conecta al MinimapUI de la escena.",
            MessageType.Info);

        EditorGUILayout.LabelField("Escena abierta", scene.IsValid() ? scene.name : "Ninguna");
        EditorGUILayout.LabelField("Resolucion maxima", LongestSidePixels + " px");
        EditorGUILayout.Space();

        GUI.enabled = scene.IsValid() && scene.isLoaded;
        if (GUILayout.Button("Generar y conectar plantilla", GUILayout.Height(36f)))
        {
            GenerateForOpenScene();
        }
        GUI.enabled = true;
    }

    private static void GenerateForOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Minimapa", "No hay una escena abierta.", "Aceptar");
            return;
        }

        MinimapUI minimap = Object.FindFirstObjectByType<MinimapUI>(FindObjectsInactive.Include);
        if (minimap == null)
        {
            EditorUtility.DisplayDialog("Minimapa", "La escena no contiene un MinimapUI.", "Aceptar");
            return;
        }

        if (!TryCalculateStaticBounds(out Bounds bounds))
        {
            EditorUtility.DisplayDialog(
                "Minimapa",
                "No se encontraron colliders estaticos suficientes para calcular los limites.",
                "Aceptar");
            return;
        }

        bounds.Expand(BoundsPadding * 2f);

        int width;
        int height;
        CalculateTextureSize(bounds.size, out width, out height);

        string folder = "Assets/MINIMAPAS/Plantillas Generadas";
        EnsureAssetFolder(folder);
        string safeSceneName = MakeSafeFileName(scene.name);
        string assetPath = folder + "/Plantilla_" + safeSceneName + ".png";

        List<RendererState> hiddenRenderers = HideNonMapRenderers();
        Camera captureCamera = null;
        RenderTexture renderTexture = null;
        Texture2D resultTexture = null;

        try
        {
            GameObject cameraObject = new GameObject("CamaraCapturaMapa_TEMP");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            captureCamera = cameraObject.AddComponent<Camera>();
            captureCamera.orthographic = true;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 1000f;
            captureCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
            captureCamera.orthographicSize = bounds.size.y * 0.5f;
            captureCamera.aspect = (float)width / height;

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            resultTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            resultTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resultTexture.Apply();
            RenderTexture.active = previousActive;

            File.WriteAllBytes(assetPath, resultTexture.EncodeToPNG());
        }
        finally
        {
            RestoreRenderers(hiddenRenderers);

            if (captureCamera != null)
            {
                Object.DestroyImmediate(captureCamera.gameObject);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            if (resultTexture != null)
            {
                Object.DestroyImmediate(resultTexture);
            }
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigureAsSingleSprite(assetPath);
        Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        SerializedObject minimapData = new SerializedObject(minimap);
        minimapData.FindProperty("useExactWorldBounds").boolValue = true;
        minimapData.FindProperty("exactWorldMin").vector2Value = bounds.min;
        minimapData.FindProperty("exactWorldMax").vector2Value = bounds.max;

        RectTransform mapRect = FindMapImage(minimap.transform);
        if (mapRect != null)
        {
            minimapData.FindProperty("mapImageRect").objectReferenceValue = mapRect;
            Image mapImage = mapRect.GetComponent<Image>();
            Undo.RecordObject(mapImage, "Asignar plantilla del minimapa");
            mapImage.sprite = generatedSprite;
            mapImage.preserveAspect = true;
            EditorUtility.SetDirty(mapImage);

            FitRectToSpriteAspect(mapRect, width, height);
        }

        minimapData.ApplyModifiedProperties();
        EditorUtility.SetDirty(minimap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = generatedSprite;
        EditorGUIUtility.PingObject(generatedSprite);
        EditorUtility.DisplayDialog(
            "Plantilla generada",
            "Se genero y conecto:\n" + assetPath +
            "\n\nLimite minimo: " + (Vector2)bounds.min +
            "\nLimite maximo: " + (Vector2)bounds.max +
            "\nResolucion: " + width + " x " + height,
            "Aceptar");
    }

    private static bool TryCalculateStaticBounds(out Bounds result)
    {
        Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        bool found = false;
        result = default;

        foreach (Collider2D collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger || IsPlayer(collider.gameObject))
            {
                continue;
            }

            Rigidbody2D body = collider.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                continue;
            }

            if (!found)
            {
                result = collider.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(collider.bounds);
            }
        }

        return found && result.size.x > 0.01f && result.size.y > 0.01f;
    }

    private static List<RendererState> HideNonMapRenderers()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        List<RendererState> states = new List<RendererState>();

        foreach (Renderer renderer in renderers)
        {
            Rigidbody2D body = renderer.GetComponentInParent<Rigidbody2D>();
            bool dynamicObject = body != null && body.bodyType == RigidbodyType2D.Dynamic;
            if (!IsPlayer(renderer.gameObject) && !dynamicObject)
            {
                continue;
            }

            states.Add(new RendererState(renderer, renderer.enabled));
            renderer.enabled = false;
        }

        return states;
    }

    private static void RestoreRenderers(List<RendererState> states)
    {
        foreach (RendererState state in states)
        {
            if (state.Renderer != null)
            {
                state.Renderer.enabled = state.WasEnabled;
            }
        }
    }

    private static bool IsPlayer(GameObject gameObject)
    {
        Transform current = gameObject.transform;
        while (current != null)
        {
            if (current.CompareTag("Player") || current.GetComponent<PlayerController>() != null)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private static void CalculateTextureSize(Vector3 worldSize, out int width, out int height)
    {
        if (worldSize.x >= worldSize.y)
        {
            width = LongestSidePixels;
            height = Mathf.Max(64, Mathf.RoundToInt(LongestSidePixels * worldSize.y / worldSize.x));
        }
        else
        {
            height = LongestSidePixels;
            width = Mathf.Max(64, Mathf.RoundToInt(LongestSidePixels * worldSize.x / worldSize.y));
        }
    }

    private static void ConfigureAsSingleSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static RectTransform FindMapImage(Transform root)
    {
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rect in rects)
        {
            if (rect.name == "Imagen_Minimapa" && rect.GetComponent<Image>() != null)
            {
                return rect;
            }
        }
        return null;
    }

    private static void FitRectToSpriteAspect(RectTransform rect, int textureWidth, int textureHeight)
    {
        Undo.RecordObject(rect, "Ajustar proporcion del minimapa");
        float maxWidth = 800f;
        float maxHeight = 800f;
        float aspect = (float)textureWidth / textureHeight;
        Vector2 size = aspect >= 1f
            ? new Vector2(maxWidth, maxWidth / aspect)
            : new Vector2(maxHeight * aspect, maxHeight);
        rect.sizeDelta = size;
        EditorUtility.SetDirty(rect);
    }

    private static void EnsureAssetFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidCharacter, '_');
        }
        return value.Replace(' ', '_');
    }

    private readonly struct RendererState
    {
        public readonly Renderer Renderer;
        public readonly bool WasEnabled;

        public RendererState(Renderer renderer, bool wasEnabled)
        {
            Renderer = renderer;
            WasEnabled = wasEnabled;
        }
    }
}
