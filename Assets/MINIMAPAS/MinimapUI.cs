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
    [SerializeField] private Vector2 markerSize = new Vector2(32f, 32f);
    [SerializeField] private Color markerColor = Color.white;

    [Header("Limites de la zona")]
    [Tooltip("Usa limites exactos generados por la herramienta de captura.")]
    [SerializeField] private bool useExactWorldBounds;
    [SerializeField] private Vector2 exactWorldMin;
    [SerializeField] private Vector2 exactWorldMax;
    [Tooltip("Opcional: dos objetos vacios que marcan las esquinas del nivel. Si faltan, se calculan usando los colliders estaticos.")]
    [SerializeField] private Transform worldBottomLeft;
    [SerializeField] private Transform worldTopRight;

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
        UpdateMarkerPosition();

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
    }

    private void CreateMarkerIfNeeded()
    {
        if (playerMarker != null || mapImageRect == null)
        {
            return;
        }

        GameObject markerObject = new GameObject("MarcadorLuzJugador", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObject.transform.SetParent(mapImageRect, false);

        playerMarker = markerObject.GetComponent<RectTransform>();
        playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarker.pivot = new Vector2(0.5f, 0.5f);
        playerMarker.sizeDelta = markerSize;
        playerMarker.SetAsLastSibling();

        Image markerImage = markerObject.GetComponent<Image>();
        markerImage.raycastTarget = false;
        markerImage.color = markerColor;
        markerImage.preserveAspect = true;

        if (player != null)
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

    private void UpdateMarkerPosition()
    {
        if (player == null || playerMarker == null || mapImageRect == null || !hasWorldBounds)
        {
            return;
        }

        float normalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, player.position.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, player.position.y);

        Rect mapRect = mapImageRect.rect;
        float halfMarkerWidth = playerMarker.rect.width * 0.5f;
        float halfMarkerHeight = playerMarker.rect.height * 0.5f;

        float markerX = Mathf.Lerp(mapRect.xMin + halfMarkerWidth, mapRect.xMax - halfMarkerWidth, normalizedX);
        float markerY = Mathf.Lerp(mapRect.yMin + halfMarkerHeight, mapRect.yMax - halfMarkerHeight, normalizedY);

        playerMarker.anchoredPosition = new Vector2(markerX, markerY);
    }
}
