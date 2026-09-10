using UnityEngine;

public class EventsAnims : MonoBehaviour
{

    private PlayerAttacks playerAttacks;
    private PlayerAudio playerAudio;

    private void Awake()
    {
        playerAttacks = GetComponent<PlayerAttacks>();
        playerAudio = GetComponent<PlayerAudio>();
    }

    public void OntAttackHit()
    {
        playerAudio?.ReproducirAtaque();
        playerAttacks.ActiveHitbox();
    }

    public void OnAttackEnd()
    {
        playerAttacks.EndAttack();
    }

    // Animation Event de RUN_animation. Al estar colocado justo cuando el pie
    // toca el suelo, el sonido acompaña la animacion aunque cambie su velocidad.
    public void OnFootstep()
    {
        playerAudio?.ReproducirPaso();
    }
}
