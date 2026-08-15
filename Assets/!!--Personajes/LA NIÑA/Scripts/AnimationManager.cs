using UnityEngine;

public class AnimationManager
{
   private StatesAnimsAbstract actualState;

    public void SetState(StatesAnimsAbstract newState)
    {
        actualState = newState;
    }
}
