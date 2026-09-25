using UnityEngine;

// ============================================================
//  AUTO DESTRUIR
//  Borra el objeto despues de unos segundos. Sirve para los efectos sueltos:
//  la animacion de muerte, un polvito, una chispa.
//  Va EN el objeto del efecto (normalmente un prefab con su Animator).
// ============================================================
public class AutoDestruir : MonoBehaviour
{
    [Tooltip("Segundos que dura antes de desaparecer. Ponele lo que dura tu animacion.")]
    [Min(0.05f)] public float segundos = 1f;

    private void Start()
    {
        Destroy(gameObject, segundos);
    }
}
