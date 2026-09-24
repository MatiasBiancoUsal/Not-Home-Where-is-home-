using UnityEngine;

// ============================================================
//  TOTALES DE PUNTOS DE CADA ZONA
//  Un asset con cuantos puntos se pueden juntar en cada zona. Sirve para que el
//  marcador pueda mostrar el total DEL JUEGO sin tener que visitar todas las zonas.
//
//  NO se llena a mano: lo escribe solo el comando
//  "Not Home > Puntos > Contar los puntos de todas las zonas".
//  Cada vez que se agregan o sacan monedas, se vuelve a correr ese comando.
//
//  Vive en Assets/Resources/TotalesDePuntos.asset
// ============================================================
public class TotalesDePuntos : ScriptableObject
{
    [System.Serializable]
    public class TotalDeZona
    {
        public string escena;
        public int puntos;
    }

    [Tooltip("Lo escribe el comando 'Not Home > Puntos > Contar los puntos de todas las zonas'.")]
    public TotalDeZona[] zonas;

    [Tooltip("Cuando se conto por ultima vez.")]
    public string ultimoConteo;

    // Cuantos puntos hay en TODO el juego.
    public int TotalDelJuego()
    {
        if (zonas == null) return 0;

        int total = 0;
        foreach (TotalDeZona z in zonas)
        {
            if (z != null) total += Mathf.Max(0, z.puntos);
        }
        return total;
    }

    // Cuantos puntos hay en una zona (0 si no figura).
    public int DeLaZona(string escena)
    {
        if (zonas == null || string.IsNullOrEmpty(escena)) return 0;

        foreach (TotalDeZona z in zonas)
        {
            if (z != null && z.escena == escena) return Mathf.Max(0, z.puntos);
        }
        return 0;
    }
}
