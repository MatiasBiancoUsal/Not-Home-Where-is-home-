using UnityEngine;

public class AttackDownPlayerStateAnims : StatesAnimsAbstract
{
public AttackDownPlayerStateAnims(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 14, ref animPlayer);
    }
}
