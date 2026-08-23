using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("Panel del mapa")]
    [SerializeField] private GameObject minimapPanel;

    [Header("Posicion del personaje")]
    [Tooltip("La imagen grande del mapa. Si queda vacia, se busca dentro del panel.")]
    [SerializeField] private RectTransform mapImageRect;
    [Tooltip("Marcador de la luz. Si queda vacio, se crea automaticamente usando un sprite Flash del Player.")]
    [SerializeField] private RectTransform playerMarker;
    [Tooltip("Dibujo del marcador. Arrastrar aca Simbolo_niña. Si queda vacio, se usa el sprite Flash del Player como antes.")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Vector2 markerSize = new Vector2(32f, 32f);
    [SerializeField] private Color markerColor = Color.white;

    [Header("Zoom")]
    [SerializeField] private bool zoomActivado = true;
    [Tooltip("Rueda del mouse, o las teclas + y -.")]
    [SerializeField] private float zoomMinimo = 1f;
    [SerializeField] private float zoomMaximo = 3f;
    [Tooltip("Con cuanto zoom se abre el mapa. Subilo si queres que arranque mas grande.")]
    [SerializeField] private float zoomInicial = 1f;
    [Tooltip("Cuanto cambia el zoom por cada click de la rueda.")]
    [SerializeField] private float pasoDeZoom = 0.25f;
    [Tooltip("Que tan suave acompaña el zoom. Mas alto = mas directo.")]
    [SerializeField] private float suavizadoDelZoom = 12f;
    [Tooltip("Al acercar, el mapa se va corriendo para que la niña quede en el centro.")]
    [SerializeField] private bool centrarEnElJugador = true;
    [Range(0f, 1f)]
    [Tooltip("Cuanto acompaña el simbolo al zoom del mapa.\n" +
             "1 = crece junto con el mapa (queda siempre del mismo tamaño respecto al dibujo).\n" +
             "0 = queda siempre del mismo tamaño en pantalla, no importa el zoom.\n" +
             "En el medio crece, pero menos que el mapa.")]
    [SerializeField] private float elMarcadorCreceConElZoom = 1f;

    [Header("Arrastrar el mapa")]
    [Tooltip("Click izquierdo sostenido sobre el mapa para moverlo y explorarlo. Click DERECHO lo vuelve a centrar.")]
    [SerializeField] private bool arrastreActivado = true;
    [Tooltip("Que tan rapido acompaña el mapa al mouse. 1 = pegado al cursor.")]
    [SerializeField] private float velocidadDeArrastre = 1f;
    [Tooltip("Que tan suave vuelve el mapa al centro con el click derecho. Mas bajo = mas lento y elegante.")]
    [SerializeField] private float suavizadoAlCentrar = 8f;

    [Header("Calibracion del dibujo (ajustar mirando el juego)")]
    [Tooltip("Cuanto del ancho y del alto de la imagen ocupa REALMENTE el nivel dibujado.\n" +
             "1 = el dibujo llega justo a los bordes de la imagen.\n" +
             "0.8 = el dibujo esta un 20% mas chico adentro del PNG (tiene margenes).\n" +
             "Bajalo si el marcador se va MAS LEJOS que el dibujo cuando caminas hasta la punta del nivel.")]
    [SerializeField] private Vector2 escalaDelDibujo = Vector2.one;
    [Tooltip("Corrimiento del marcador, en pixeles de la pantalla. Positivo en X lo mueve a la derecha, " +
             "positivo en Y lo mueve hacia arriba. Sirve si el dibujo quedo descentrado dentro del PNG.")]
    [SerializeField] private Vector2 desplazamientoDelDibujo = Vector2.zero;

    [Header("Limites de la zona")]
    [Tooltip("Usa limites exactos generados por la herramienta de captura.")]
    [SerializeField] private bool useExactWorldBounds;
    [SerializeField] private Vector2 exactWorldMin;
    [SerializeField] private Vector2 exactWorldMax;
    [Tooltip("Opcional: dos objetos vacios que marcan las esquinas del nivel. Si faltan, se calculan usando los colliders estaticos.")]
    [SerializeField] private Transform worldBottomLeft;
    [SerializeField] private Transform worldTopRight;

    private float zoomObjetivo = 1f;
    private float zoomActual = 1f;
    private Vector3 posicionOriginalDelMapa;
    private bool guardePosicionOriginal;

    private Vector2 desplazamientoManual;
    private Vector2 desplazamientoObjetivo;
    private bool arrastrando;

    private Transform player;
    private Bounds worldBounds;
    private bool hasWorldBounds;
    private bool isOpen;
    private float timeScaleBeforeOpening = 1f;
    private int lastClosedFrame = -1;

    public bool IsOpen => isOpen;
    public int LastClosedFrame => lastClosedFrame;

    private void Awake()
    {
        FindReferences();
        CalculateWorldBounds();
        CreateMarkerIfNeeded();
    }

    private void Start()
    {
        CloseMinimap(false);
    }

    private void Update()
    {
        // Mientras el mapa esta abierto seguimos actualizando la posicion del marcador.
        // El juego esta congelado, pero asi el marcador acompaña el zoom y cualquier
        // cambio del panel sin quedarse pegado donde estaba al abrirlo.
        if (isOpen)
        {
            // Se reaplica el aspecto del marcador cada frame: asi se puede cambiar el
            // tamaño, el color o el dibujo desde el Inspector con el mapa abierto y
            // verlo al instante, sin cerrar y volver a abrir.
            CreateMarkerIfNeeded();

            LeerEntradaDeZoom();
            LeerArrastre();
            UpdateMarkerPosition();
            AplicarZoom();
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMinimap();
            return;
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMinimap();
        }
    }

    public void ToggleMinimap()
    {
        if (isOpen)
        {
            CloseMinimap();
        }
        else
        {
            OpenMinimap();
        }
    }

    public void OpenMinimap()
    {
        if (isOpen)
        {
            return;
        }

        FindReferences();

        if (!hasWorldBounds)
        {
            CalculateWorldBounds();
        }

        CreateMarkerIfNeeded();

        // Cada vez que se abre, el zoom vuelve al inicial y el mapa a su lugar
        // (no se arrastra el estado de la vez anterior).
        zoomObjetivo = Mathf.Clamp(zoomInicial, zoomMinimo, zoomMaximo);
        zoomActual = zoomObjetivo;
        desplazamientoManual = Vector2.zero;
        desplazamientoObjetivo = Vector2.zero;
        arrastrando = false;

        UpdateMarkerPosition();
        AplicarZoom();

        isOpen = true;

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MinimapUI: falta asignar el Panel_Minimapa en el Inspector.");
        }

        timeScaleBeforeOpening = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMinimap()
    {
        CloseMinimap(true);
    }

    private void CloseMinimap(bool restoreTime)
    {
        bool wasOpen = isOpen;
        isOpen = false;

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(false);
        }

        if (restoreTime && wasOpen)
        {
            Time.timeScale = timeScaleBeforeOpening;
            lastClosedFrame = Time.frameCount;
        }
    }

    private void FindReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (mapImageRect == null && minimapPanel != null)
        {
            Image[] images = minimapPanel.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image.gameObject.name == "Imagen_Minimapa")
                {
                    mapImageRect = image.rectTransform;
                    break;
                }
            }
        }

        // Guardamos donde esta el mapa sin zoom, para poder volver siempre a esa posicion.
        if (mapImageRect != null && !guardePosicionOriginal)
        {
            posicionOriginalDelMapa = mapImageRect.localPosition;
            guardePosicionOriginal = true;
        }
    }

    private void CreateMarkerIfNeeded()
    {
        if (mapImageRect == null)
        {
            return;
        }

        // Se crea solo la primera vez.
        if (playerMarker == null)
        {
            GameObject markerObject = new GameObject("MarcadorLuzJugador", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerObject.transform.SetParent(mapImageRect, false);

            playerMarker = markerObject.GetComponent<RectTransform>();
            playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
            playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            playerMarker.pivot = new Vector2(0.5f, 0.5f);
            playerMarker.SetAsLastSibling();
        }

        // El aspecto se aplica SIEMPRE, aunque el marcador ya existiera: asi los
        // cambios del Inspector (sprite, tamaño, color) se ven sin tener que borrarlo.
        Image markerImage = playerMarker.GetComponent<Image>();
        if (markerImage == null)
        {
            markerImage = playerMarker.gameObject.AddComponent<Image>();
        }

        playerMarker.sizeDelta = markerSize;
        markerImage.raycastTarget = false;
        markerImage.color = markerColor;
        markerImage.preserveAspect = true;

        // 1) El dibujo elegido a mano en el Inspector (Simbolo_niña) manda.
        if (markerSprite != null)
        {
            markerImage.sprite = markerSprite;
            return;
        }

        // 2) Si no hay ninguno puesto, se usa el sprite Flash del Player, como antes.
        if (markerImage.sprite == null && player != null)
        {
            SpriteRenderer[] playerSprites = player.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in playerSprites)
            {
                if (spriteRenderer.name.StartsWith("Flash") && spriteRenderer.sprite != null)
                {
                    markerImage.sprite = spriteRenderer.sprite;
                    break;
                }
            }
        }
    }

    private void CalculateWorldBounds()
    {
        if (useExactWorldBounds)
        {
            Vector3 minimum = exactWorldMin;
            Vector3 maximum = exactWorldMax;
            worldBounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            hasWorldBounds = worldBounds.size.x > 0.01f && worldBounds.size.y > 0.01f;
            return;
        }

        if (worldBottomLeft != null && worldTopRight != null)
        {
            Vector3 minimum = Vector3.Min(worldBottomLeft.position, worldTopRight.position);
            Vector3 maximum = Vector3.Max(worldBottomLeft.position, worldTopRight.position);
            worldBounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            hasWorldBounds = worldBounds.size.x > 0.01f && worldBounds.size.y > 0.01f;
            return;
        }

        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        bool foundCollider = false;

        foreach (Collider2D collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger || collider.transform == player ||
                collider.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            Rigidbody2D body = collider.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                continue;
            }

            if (!foundCollider)
            {
                worldBounds = collider.bounds;
                foundCollider = true;
            }
            else
            {
                worldBounds.Encapsulate(collider.bounds);
            }
        }

        hasWorldBounds = foundCollider && worldBounds.size.x > 0.01f && worldBounds.size.y > 0.01f;

        if (!hasWorldBounds)
        {
            Debug.LogWarning("MinimapUI: no se pudieron calcular los limites de esta zona.");
        }
    }

    // ---------- Zoom ----------

    private void LeerEntradaDeZoom()
    {
        if (!zoomActivado)
        {
            return;
        }

        float cambio = 0f;

        if (Mouse.current != null)
        {
            float rueda = Mouse.current.scroll.ReadValue().y;
            if (rueda > 0f) cambio += pasoDeZoom;
            else if (rueda < 0f) cambio -= pasoDeZoom;
        }

        if (Keyboard.current != null)
        {
            // Acepta el + y el - de arriba y los del teclado numerico.
            if (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame)
            {
                cambio += pasoDeZoom;
            }
            if (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame)
            {
                cambio -= pasoDeZoom;
            }
        }

        if (cambio != 0f)
        {
            zoomObjetivo = Mathf.Clamp(zoomObjetivo + cambio, zoomMinimo, zoomMaximo);
        }
    }

    // Click sostenido sobre el mapa para moverlo y explorarlo (sobre todo con zoom).
    private void LeerArrastre()
    {
        if (!arrastreActivado || Mouse.current == null)
        {
            return;
        }

        // Click derecho: vuelve a centrar el mapa. No salta de golpe: se le pone el destino
        // en cero y el mapa viaja solo hasta ahi (ver el Lerp en AplicarZoom).
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            desplazamientoObjetivo = Vector2.zero;
            arrastrando = false;
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && ElMouseEstaSobreElMapa())
        {
            arrastrando = true;
        }

        if (!Mouse.current.leftButton.isPressed)
        {
            arrastrando = false;
        }

        if (!arrastrando)
        {
            return;
        }

        // El movimiento del mouse viene en pixeles de PANTALLA; el mapa se mueve en
        // unidades del Canvas. Si el Canvas escala con la resolucion, hay que dividir
        // por su factor o el arrastre se siente mas rapido o mas lento que el cursor.
        float factor = 1f;
        Canvas canvas = mapImageRect != null ? mapImageRect.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.scaleFactor > 0.01f)
        {
            factor = canvas.scaleFactor;
        }

        desplazamientoObjetivo += Mouse.current.delta.ReadValue() * (velocidadDeArrastre / factor);

        // Tope, para que no se pueda mandar el mapa tan lejos que se pierda de vista.
        if (mapImageRect != null)
        {
            Vector2 tope = mapImageRect.rect.size * zoomActual * 0.5f;
            desplazamientoObjetivo.x = Mathf.Clamp(desplazamientoObjetivo.x, -tope.x, tope.x);
            desplazamientoObjetivo.y = Mathf.Clamp(desplazamientoObjetivo.y, -tope.y, tope.y);
        }

        // Mientras se arrastra, el mapa va PEGADO al cursor: sin suavizado, o se sentiria
        // como si el mapa flotara atras de la mano.
        desplazamientoManual = desplazamientoObjetivo;
    }

    private bool ElMouseEstaSobreElMapa()
    {
        // Para no arrastrar cuando el click arranca fuera del panel (por ejemplo en la X).
        RectTransform zona = minimapPanel != null ? minimapPanel.transform as RectTransform : mapImageRect;
        if (zona == null)
        {
            return true;
        }

        Canvas canvas = zona.GetComponentInParent<Canvas>();
        Camera camara = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(zona, Mouse.current.position.ReadValue(), camara);
    }

    private void AplicarZoom()
    {
        if (mapImageRect == null)
        {
            return;
        }

        // El mapa se abre con el timeScale en 0, asi que todo va en tiempo real.
        zoomActual = Mathf.Lerp(zoomActual, zoomObjetivo, Mathf.Max(0.01f, suavizadoDelZoom) * Time.unscaledDeltaTime);

        mapImageRect.localScale = Vector3.one * zoomActual;

        // El desplazamiento viaja suave hasta su destino. Al arrastrar los dos valores son
        // iguales (no hace nada), y al centrar con el click derecho el destino pasa a ser
        // cero y el mapa vuelve deslizandose en vez de saltar.
        desplazamientoManual = Vector2.Lerp(desplazamientoManual, desplazamientoObjetivo,
                                            Mathf.Max(0.01f, suavizadoAlCentrar) * Time.unscaledDeltaTime);

        // Al acercar, corremos el mapa para que la niña quede en el centro. La correccion
        // se aplica de a poco: en el zoom minimo se ve el mapa entero y centrado como
        // siempre, y a medida que se acerca va siguiendo al marcador.
        Vector3 destino = posicionOriginalDelMapa;

        if (centrarEnElJugador && playerMarker != null && zoomMaximo > zoomMinimo)
        {
            float cuantoSeguir = Mathf.InverseLerp(zoomMinimo, zoomMaximo, zoomActual);
            Vector3 corrimiento = -playerMarker.localPosition * zoomActual;
            destino = posicionOriginalDelMapa + corrimiento * cuantoSeguir;
        }

        mapImageRect.localPosition = destino + (Vector3)desplazamientoManual;

        // El simbolo es hijo del mapa, asi que por defecto se agranda junto con el.
        // Con escala 1 crece igual que el mapa; con 1/zoom se compensa y queda del mismo
        // tamaño en pantalla. El valor del Inspector elige el punto entre esos dos.
        if (playerMarker != null)
        {
            float tamanioFijo = 1f / Mathf.Max(0.01f, zoomActual);
            float escala = Mathf.Lerp(tamanioFijo, 1f, Mathf.Clamp01(elMarcadorCreceConElZoom));
            playerMarker.localScale = Vector3.one * escala;
        }
    }

    private void UpdateMarkerPosition()
    {
        if (player == null || playerMarker == null || mapImageRect == null || !hasWorldBounds)
        {
            return;
        }

        // 0 = borde izquierdo/abajo del nivel, 1 = borde derecho/arriba.
        float normalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, player.position.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, player.position.y);

        // CALIBRACION: encoge o estira el area util desde el centro, y despues la corre.
        // Sirve cuando el dibujo no ocupa exactamente el mismo recuadro que la plantilla
        // (por ejemplo si quedo con margenes alrededor).
        normalizedX = (normalizedX - 0.5f) * escalaDelDibujo.x + 0.5f;
        normalizedY = (normalizedY - 0.5f) * escalaDelDibujo.y + 0.5f;

        // Se mapea al rectangulo COMPLETO de la imagen.
        // Antes se dejaba media altura de marcador de margen en cada borde, y eso comprimia
        // todo el mapeo: cuanto mas grande el marcador, mas se desfasaba la posicion.
        Rect mapRect = mapImageRect.rect;

        float markerX = Mathf.Lerp(mapRect.xMin, mapRect.xMax, normalizedX);
        float markerY = Mathf.Lerp(mapRect.yMin, mapRect.yMax, normalizedY);

        playerMarker.anchoredPosition = new Vector2(markerX, markerY) + desplazamientoDelDibujo;
    }
}
