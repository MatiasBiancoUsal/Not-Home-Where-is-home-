using UnityEngine;

public class StatesAnimsAbstract
{
    private string name;
    private int animationId;

    public void ActiveAnimation(string nameAnim, int animId, ref Animator playerAnimator)
    {
        name = nameAnim;
        animationId = animId;
        playerAnimator.SetInteger(name, animationId);
    }
}
