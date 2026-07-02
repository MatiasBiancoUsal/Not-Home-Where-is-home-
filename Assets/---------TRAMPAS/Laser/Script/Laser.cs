using System.Collections;
using UnityEngine;

public class LaserLoop : MonoBehaviour
{
    public GameObject LaserVisual;
    public float TiempoEncendido = 2f;
    public float TiempoApagado = 1f;
    void Start()
    {
        StartCoroutine(CicloLaser());
    }

    IEnumerator CicloLaser()
    {
        while (true)
        {
            LaserVisual.SetActive(true);
            yield return new WaitForSeconds(TiempoEncendido);

            LaserVisual.SetActive(false);
            yield return new WaitForSeconds(TiempoApagado);
        }
    }  

}
