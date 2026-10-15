#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AgregarParticulasZona1
{
    private const string Nombre = "AMBIENTE - MOTAS SUTILES";

    [MenuItem("Not Home/Decoracion/Agregar particulas sutiles Zona 1")]
    public static void Crear()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name != "Zona 1")
        {
            Debug.LogWarning("Abri la escena Zona 1 antes de agregar las particulas.");
            return;
        }

        GameObject anterior = GameObject.Find(Nombre);
        if (anterior != null) Object.DestroyImmediate(anterior);

        Sprite spriteSuave = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/-------DISENO NIVELES/Zona 3/desenfoque.png");

        GameObject go = new GameObject(Nombre);
        Camera camara = Camera.main;
        if (camara != null)
        {
            go.transform.SetParent(camara.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 10f);
        }
        else go.transform.position = new Vector3(-74f, -76f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 350;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.075f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.24f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.58f, 0.8f, 1f, 0.20f),
            new Color(0.9f, 0.97f, 1f, 0.38f));
        main.gravityModifier = -0.004f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 25f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        // Emite solamente alrededor de la cámara. Los ladrillos, que se dibujan
        // en orden 6, ocultan las motas y dejan verlas solo en los huecos azules.
        shape.scale = new Vector3(32f, 20f, 0.1f);
        shape.randomDirectionAmount = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.055f, 0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
        noise.frequency = 0.12f;
        noise.scrollSpeed = 0.035f;
        noise.damping = true;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.78f, 1f), 0f),
                new GradientColorKey(new Color(0.9f, 0.97f, 1f), 0.5f),
                new GradientColorKey(new Color(0.55f, 0.78f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f, 0.22f),
                new GradientAlphaKey(0.38f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradiente;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Elementos 2 Detras";
        // El Tilemap de Zona 1 usa esta capa con orden 6. En orden 5 las motas
        // quedan delante del fondo azul, pero siguen ocultas por los ladrillos.
        renderer.sortingOrder = 5;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = CrearMaterialMotas(spriteSuave);

        ps.Play();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("Particulas ambientales sutiles agregadas en Zona 1.");
    }

    private static Material CrearMaterialMotas(Sprite sprite)
    {
        const string ruta = "Assets/-------DISENO NIVELES/Zona1/Arte/MotasZona1.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "MotasZona1" };
            AssetDatabase.CreateAsset(material, ruta);
        }

        if (sprite != null) material.mainTexture = sprite.texture;
        material.color = Color.white;
        EditorUtility.SetDirty(material);
        return material;
    }
}
#endif
