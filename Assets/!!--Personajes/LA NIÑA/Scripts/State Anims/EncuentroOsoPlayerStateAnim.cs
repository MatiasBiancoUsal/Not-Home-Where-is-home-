using UnityEngine;

// Animacion de la niña cuando encuentra al osito, justo antes de que arranque la
// cinematica. La dispara CinematicaFrames al preparar la escena, en lugar del idle.
//
// OJO CON EL NUMERO: va en 20 y NO en 15, porque el 15 ya lo usa el pisoton
// (StompPlayerStateAnim). Del 1 al 19 esta todo tomado:
//   1 idle, 2 run, 3 salto, 4 caida, 5 doble salto, 6 dash, 7/8 trepar,
//   9 muerte, 10/11/12 combo, 13 ataque arriba, 14 ataque abajo, 15 pisoton,
//   16/17/18/19 super salto.
// En el Animator, la transicion de EncuentroOso_animation tiene que estar en 20.
public class EncuentroOsoPlayerStateAnim : StatesAnimsAbstract
{
    public EncuentroOsoPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 20, ref animPlayer);
    }
}
