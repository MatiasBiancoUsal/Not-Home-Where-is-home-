using Unity.Cinemachine;
using UnityEngine;

// ============================================================
//  TELETRANSPORTAR PLAYER
//  Va en un objeto VACIO puesto donde tiene que aparecer la niña. Su posicion es el
//  punto de destino (en la Scene se ve como un circulo verde).
//
//  Pensado para usarse al final de una cinematica: en la CinematicaFrames, en el
//  evento "En Negro Antes De Volver", arrastrar este objeto y elegir
//  TeletransportarPlayer > Teletransportar(). Asi el salto pasa con la pantalla en
//  negro y no se ve.
//
//  Tambien mueve la camara de golpe, para que no se vea el paneo cruzando el mapa.
// ============================================================
public class TeletransportarPlayer : MonoBehaviour
{
    [Tooltip("La niña. Si lo dejas vacio, la busca sola por el tag de abajo.")]
    public Transform player;
    public string tagJugador = "Player";

    [Tooltip("Frena a la niña al llegar, asi no aparece arrastrando la velocidad que traia.")]
    public bool frenarVelocidad = true;

    public void Teletransportar()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(tagJugador);
            if (go != null) player = go.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("TeletransportarPlayer: no encontre a la niña (tag '" + tagJugador + "').", this);
            return;
        }

        Vector3 destino = new Vector3(transform.position.x, transform.position.y, player.position.z);
        Vector3 delta = destino - player.position;

        // Auto Sync Transforms esta apagado: con Rigidbody hay que mover el rb Y el transform,
        // si no la fisica la devuelve al lugar viejo en el proximo paso.
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = destino;
            if (frenarVelocidad)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        player.position = destino;
        Physics2D.SyncTransforms();

        // La camara salta junto con la niña en vez de cruzar el mapa con damping.
        CinemachineCore.OnTargetObjectWarped(player, delta);
        foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
        {
            cam.PreviousStateIsValid = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.7f, transform.position + Vector3.right * 0.7f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.7f, transform.position + Vector3.up * 0.7f);
    }
}
