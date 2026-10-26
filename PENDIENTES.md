# Pendientes de Not Home

Notas para retomar el trabajo (sirven también para abrir una sesión nueva de Claude en
otra computadora: alcanza con decirle "leé PENDIENTES.md").

Última actualización: 26/10/2026.

---

## 1. Analytics — lo que falta

El código ya manda los 6 eventos. Están en
`Assets/ScriptsGeneralesEscenas/Analytics/AnalyticsJuego.cs`.

### ❌ El problema encontrado

En el dashboard de Unity el evento está creado como **`DesbloquearHabilidades`** (plural)
pero el juego manda **`DesbloquearHabilidad`** (singular). Unity lo descarta y muestra:

> *DesbloquearHabilidad failed to import because the event schema was not found.*

**Arreglo:** en Event Manager, en ese cartel naranja, apretar **"Create Event"** — crea el
evento con el nombre y los parámetros que el juego ya mandó. El `DesbloquearHabilidades`
viejo no se puede borrar; se deja desactivado (Enabled en off).

### Estado de los 6 eventos

| Evento | En el dashboard |
|---|---|
| PartidaIniciada | ✅ |
| EntrarZona | ✅ |
| Muerte | ✅ (ya llegó 1 evento real) |
| DesbloquearHabilidad | ❌ está como `DesbloquearHabilidades` |
| PuntosInsuficientes | ✅ |
| JuegoTerminado | ✅ |

### Parámetros que manda el juego (tienen que coincidir EXACTO)

| Evento | Parámetros | Tipo |
|---|---|---|
| PartidaIniciada | tipoInicio / nombreZona | string / string |
| EntrarZona | nombreZona / numeroZona / puntosGlobales | string / int / int |
| Muerte | nombreZona / causaMuerte / muertesEnZona / segundosEnZona | string / string / int / int |
| DesbloquearHabilidad | nombreHabilidad / costoHabilidad / puntosRestantes / nombreZona | string / int / int / string |
| PuntosInsuficientes | nombreHabilidad / costoHabilidad / puntosGlobales / nombreZona | string / int / int / string |
| JuegoTerminado | segundosSesion / muertesSesion | int / int |

Ojo: los parámetros ahora son **por environment**. Hay que revisarlos en el mismo
environment donde se mira (el selector que dice `production`).

### Cómo probar

1. Menú **Not Home → Analytics → Mandar eventos desde el editor (PRUEBA)**.
   Queda tildado mientras está prendido. La build manda siempre, sin importar esto.
2. Jugar: arrancar partida → entrar a Zona 1 → pasar a Zona 2 → morir con pinchos →
   tocar una flor sin puntos → apretar **P** y comprar la habilidad.
3. El código hace `Flush()` cuando corre en el editor, así que los contadores del
   dashboard se mueven en segundos (no hay que esperar el minuto de siempre).
4. **Apagar el menú** cuando se confirme, así las pruebas del equipo no ensucian los datos.

### Antes de la build final

Cuando el profesor transfiera el proyecto hay que **volver a vincularlo** en
Project Settings → Services. Los eventos no se pierden.

---

## 2. Puertas

Correr **Not Home → Puertas → Revisar la llegada (que entre caminando)**. Quedan dos:

- **Zona 6, `PUERTA Z6 ARRIBA 4 --> Z4 ABAJO 2`**: no hay piso debajo, la niña aparece
  en el aire. Es por donde se llega viniendo de Zona 4.
- **Zona 3, `PUERTA Z3 ABAJO 1 --> Z4 ABAJO 1`**: choca contra `'piedra'` a 0,1 unidades
  al entrar. Correr la puerta hacia adentro o bajarle `Segundos Caminando Al Entrar`.

Las otras 16 están bien.

---

## 3. Tilemaps — que no camine a los brincos

Falta **Composite Collider 2D + Rigidbody 2D (Static)** y, en el Tilemap Collider 2D,
`Composite Operation = Merge` y `Extrusion Factor = 0.01`. En el Composite,
`Geometry Type = Polygons`. Zonas 2 y 3 están bien hechas y sirven de modelo.

- Zona 1: `Zona1_Modulo_2x2_Tilemap` y `Plataformas TileMap` (a esta le falta solo el Composite).
- Zona 4: `Zona1_Modulo_2x2_Tilemap` (el Tilemap grande ya está hecho).
- Zona 5: `Tilemap`.
- Zona 6: `Tilemap` — tiene todo, solo falta poner `Composite Operation = Merge`.

---

## 4. Boss final

- Animaciones de **Volador** y **Saltarín**.
- Sonidos del boss y música de la pelea.
- Apagar **"Empezar Al Dar Play"** cuando la cinemática maneje el arranque.

---

## Comandos propios del proyecto (menú "Not Home")

- **Puntos → Contar los puntos de todas las zonas** — recuenta las monedas y actualiza
  `Assets/Resources/TotalesDePuntos.asset` (lo que muestra el marcador).
- **Puertas → Revisar la llegada (que entre caminando)**.
- **Carteles → Copiar el diseño de la flor modelo a todas las zonas** — toma
  `Zona 3 > FlorDash` como modelo y deja todos los carteles iguales.
- **Analytics → Mandar eventos desde el editor (PRUEBA)**.

Atajos de testeo (solo en el editor): **H** desbloquea las habilidades de la zona,
**P** regala los puntos justos de la flor, **Shift+N** salta de zona, **F5** regenera
las monedas, **F9** diagnostica al player.
