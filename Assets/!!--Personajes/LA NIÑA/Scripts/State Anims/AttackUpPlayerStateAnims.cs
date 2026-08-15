using UnityEngine;

public class AttackUpPlayerStateAnims : StatesAnimsAbstract
{
    public AttackUpPlayerStateAnims(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 13, ref animPlayer);
    }
}

