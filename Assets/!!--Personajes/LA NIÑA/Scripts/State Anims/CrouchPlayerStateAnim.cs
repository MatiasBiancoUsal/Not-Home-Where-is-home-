using UnityEngine;

// La niña agachada. Se usa cuando se esconde bajo el piso de la embestida del boss final
// (ver EscondersePlayer). En el Animator es el valor 21 del parametro "stateAnim".
public class CrouchPlayerStateAnim : StatesAnimsAbstract
{
    public const int ID = 21;

    public CrouchPlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", ID, ref animPlayer);
    }
}
