using UnityEngine;
using Unity.Cinemachine;

// Hace que la camara se ponga mas AGIL en vertical cuando la niña cae rapido,
// asi no se queda atras y el usuario ve hacia donde cae. Cuando va normal, vuelve al seguimiento suave.
// Este script va EN la Cinemachine Camera (la que tiene el Position Composer).
[RequireComponent(typeof(CinemachinePositionComposer))]
public class CameraFallCatchUp : MonoBehaviour
{
    [Header("Caida rapida")]
    public float fallSpeed = -10f;          // a partir de esta velocidad de caida consideramos "cae rapido"
    public float normalDampingY = 1f;       // damping vertical normal (seguimiento suave)
    public float fastFallDampingY = 0.15f;  // damping vertical al caer rapido (camara mas agil)
    public float changeSpeed = 8f;          // que tan rapido pasa de un damping al otro

    private CinemachinePositionComposer composer;
    private Rigidbody2D playerRb;

    private void Awake()
    {
        composer = GetComponent<CinemachinePositionComposer>();
    }

    private void Update()
    {
        // si todavia no tenemos el rigidbody del player, lo buscamos
        if (playerRb == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) playerRb = player.rb;
            if (playerRb == null) return;
        }

        // objetivo: damping bajo si cae rapido, damping normal si no
        float targetDampingY = (playerRb.linearVelocity.y < fallSpeed) ? fastFallDampingY : normalDampingY;

        // lo cambiamos de a poco para que la transicion sea suave (no de un saque)
        Vector3 damping = composer.Damping;
        damping.y = Mathf.Lerp(damping.y, targetDampingY, changeSpeed * Time.deltaTime);
        composer.Damping = damping;
    }
}
