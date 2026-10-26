# Pendientes de Not Home

Notas para retomar el trabajo (sirven también para abrir una sesión nueva de Claude en
otra computadora: alcanza con decirle "leé PENDIENTES.md").

Última actualización: 26/10/2026.

---

## 0. Al abrir el proyecto en otra computadora

1. `git pull`
2. Abrir el proyecto con **Unity 6000.3.9f1** (la misma versión; otra lo re-importa entero).
3. La primera apertura tarda bastante: Unity reconstruye la carpeta `Library` desde cero.
4. **Analytics**: cuando el profesor transfiera el proyecto hay que volver a vincularlo en
   `Project Settings → Services` antes de hacer la build. Los eventos del dashboard no se
   pierden.

Cosas del proyecto que conviene saber:

- **Reload Domain está desactivado.** Las variables `static` NO se reinician al dar Play:
  sobreviven de una sesión a la otra. Por eso varios scripts tienen un método marcado con
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` que las resetea a mano. Si
  agregás un `static` con estado, acordate de reiniciarlo ahí.
- **Finales de línea**: ya hay un `.gitattributes` que le dice a git que no toque los
  archivos de Unity. Sin eso, en Windows git corrompe las tipografías de TextMeshPro y el
  juego deja de mostrar textos (y `git status` no lo muestra). No hace falta configurar
  nada a mano.
- **Medir geometría** (si hay piso, cuánto cae, si choca): hay que hacerlo con un comando
  de editor que use `Physics2D.Raycast`, como
  `Assets/ScriptsGeneralesEscenas/Editor/MenuLlegadaPuertas.cs`. Leer los `.unity` con un
  script da resultados FALSOS: casi todo el nivel son prefab instances, y los tiles no
  miden una celda (el de piedra mide 5 x 2,9 unidades y se coloca cada 5 celdas).

---

## 1. Analytics — lo que falta

El código ya manda los 6 eventos. Están en
`Assets/ScriptsGeneralesEscenas/Analytics/AnalyticsJuego.cs`.

### ✅ El problema encontrado (ya resuelto el 26/10/2026)

En el dashboard el evento estaba creado como **`DesbloquearHabilidades`** (plural) pero el
juego manda **`DesbloquearHabilidad`** (singular), así que Unity lo descartaba:

> *DesbloquearHabilidad failed to import because the event schema was not found.*

Se creó el evento en singular desde el botón "Create Event" del cartel de error (toma los
parámetros del evento que ya había llegado) y se **desactivó** el viejo en plural (Unity no
deja borrar eventos, solo apagarlos).

### Estado de los 6 eventos

| Evento | En el dashboard |
|---|---|
| PartidaIniciada | ✅ |
| EntrarZona | ✅ (ya llegaron 2 eventos reales) |
| Muerte | ✅ (ya llegó 1 evento real) |
| DesbloquearHabilidad | ✅ creado y habilitado |
| PuntosInsuficientes | ✅ |
| JuegoTerminado | ✅ |

Queda **probar los 6 jugando** (ver "Cómo probar" más abajo) y confirmar que suben los
contadores de "Valid Events".

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

**Correr `Not Home → Tilemaps → Arreglar los colliders (que camine liso)`** y listo: lo
hace solo en las 6 zonas.

El Tilemap Collider 2D hace una cajita por tile y la niña se engancha en las juntas (por
eso se le dispara la animación de salto al caminar). El comando le pone a cada tilemap de
piso un **Composite Collider 2D** (que suelda todas las cajitas en un contorno continuo)
más un **Rigidbody 2D en Static**, y deja el Tilemap Collider en `Composite Operation =
Merge` con `Extrusion Factor = 0.01`.

Solo toca tilemaps que ya tienen Tilemap Collider 2D y están en la capa Ground, así no le
agrega colliders a la decoración.

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
- **Tilemaps → Arreglar los colliders (que camine liso)**.
- **Analytics → Mandar eventos desde el editor (PRUEBA)**.

Atajos de testeo (solo en el editor): **H** desbloquea las habilidades de la zona,
**P** regala los puntos justos de la flor, **Shift+N** salta de zona, **F5** regenera
las monedas, **F9** diagnostica al player.
