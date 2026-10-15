using UnityEngine;

[DisallowMultipleComponent]
public class ParallaxSuave : MonoBehaviour
{
    [Range(0f, 0.5f)] public float intensidadX = 0.08f;
    [Range(0f, 0.5f)] public float intensidadY = 0.035f;

    private Camera camara;
    private Vector3 posicionInicial;
    private Vector3 posicionInicialCamara;

    private void Start()
    {
        camara = Camera.main;
        posicionInicial = transform.position;
        if (camara != null) posicionInicialCamara = camara.transform.position;
    }

    private void LateUpdate()
    {
        if (camara == null)
        {
            camara = Camera.main;
            if (camara == null) return;
            posicionInicialCamara = camara.transform.position;
        }

        Vector3 desplazamiento = camara.transform.position - posicionInicialCamara;
        transform.position = posicionInicial + new Vector3(
            desplazamiento.x * intensidadX,
            desplazamiento.y * intensidadY,
            0f);
    }
}
