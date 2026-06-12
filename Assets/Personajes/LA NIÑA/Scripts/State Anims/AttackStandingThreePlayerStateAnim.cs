using UnityEngine;

public class AttackStandingThreePlayerStateAnim : StatesAnimsAbstract
{
public AttackStandingThreePlayerStateAnim(Animator animPlayer)
    {
        ActiveAnimation("stateAnim", 12, ref animPlayer);
    }
}
