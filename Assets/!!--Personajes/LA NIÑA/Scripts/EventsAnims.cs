using UnityEngine;

public class EventsAnims : MonoBehaviour
{

    private PlayerAttacks playerAttacks;

    private void Awake()
    {
        playerAttacks = GetComponent<PlayerAttacks>();
    }

    public void OntAttackHit()
    {
        playerAttacks.ActiveHitbox();
    }

    public void OnAttackEnd()
    {
        playerAttacks.EndAttack();
    }
}
