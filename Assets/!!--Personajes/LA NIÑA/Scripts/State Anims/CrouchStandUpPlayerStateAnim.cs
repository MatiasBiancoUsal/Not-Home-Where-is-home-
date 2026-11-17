using UnityEngine;

// La niña LEVANTANDOSE despues de estar agachada. Es la animacion de agacharse al reves,
// asi parece que se vuelve a parar en vez de pegar un salto al idle.
// En el Animator es el valor 22 del parametro "stateAnim".
public class CrouchStandUpPlayerStateAnim : StatesAnimsAbstract
{
    public const int ID = 22;

    public CrouchStandUpPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", ID, ref animPlayer);
    }
}
