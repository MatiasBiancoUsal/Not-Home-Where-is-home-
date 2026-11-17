#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ============================================================
//  ESTADO "LEVANTARSE" EN EL ANIMATOR DE LA NIÑA
//
//  La niña ya tiene la animacion de AGACHARSE (valor 21 de "stateAnim"). Para que al
//  soltar la S se vea como se vuelve a parar, hay una copia de esa animacion con los
//  dibujos al reves: CrouchStandUp_animation.
//
//  Este comando agrega esa animacion al Animator como un estado nuevo y le engancha la
//  flecha desde "Any State" con la condicion stateAnim == 22. Es exactamente lo mismo
//  que hacer a mano: arrastrar el clip al Animator + click derecho en Any State >
//  Make Transition + poner la condicion. Solo que sin equivocarse en ningun numero.
//
//  Se puede correr las veces que haga falta: si el estado ya existe, no lo duplica.
// ============================================================
public static class MenuAnimacionLevantarse
{
    private const string CONTROLADOR = "Assets/!!--Personajes/LA NIÑA/animations/PlayerAnimatorController.controller";
    private const string CLIP        = "Assets/!!--Personajes/LA NIÑA/animations/Crouch/CrouchStandUp_animation.anim";
    private const string PARAMETRO   = "stateAnim";
    private const string ESTADO      = "CrouchStandUp_animation";
    private const int    VALOR       = 22;

    [MenuItem("Not Home/Animaciones/Agregar el estado LEVANTARSE (22) a la niña", false, 20)]
    private static void Agregar()
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLADOR);
        if (ctrl == null)
        {
            Debug.LogError("[Levantarse] No encontre el Animator en " + CONTROLADOR);
            return;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CLIP);
        if (clip == null)
        {
            Debug.LogError("[Levantarse] No encontre la animacion en " + CLIP);
            return;
        }

        AnimatorStateMachine maquina = ctrl.layers[0].stateMachine;

        // ¿Ya esta el estado? (por si se corre el comando dos veces)
        AnimatorState estado = null;
        foreach (ChildAnimatorState hijo in maquina.states)
        {
            if (hijo.state != null && hijo.state.name == ESTADO)
            {
                estado = hijo.state;
                break;
            }
        }

        if (estado == null)
        {
            estado = maquina.AddState(ESTADO, new Vector3(520f, 420f, 0f));
            Debug.Log("[Levantarse] Cree el estado " + ESTADO + ".");
        }

        estado.motion = clip;
        estado.speed = 1f;

        // ¿Ya existe la flecha desde Any State con la condicion stateAnim == 22?
        foreach (AnimatorStateTransition t in maquina.anyStateTransitions)
        {
            if (t.destinationState != estado) continue;

            foreach (AnimatorCondition c in t.conditions)
            {
                if (c.parameter == PARAMETRO && Mathf.Approximately(c.threshold, VALOR))
                {
                    EditorUtility.SetDirty(ctrl);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Levantarse] Listo: el estado y la flecha ya estaban puestos. No toque nada mas.");
                    return;
                }
            }
        }

        AnimatorStateTransition flecha = maquina.AddAnyStateTransition(estado);
        flecha.hasExitTime = false;
        flecha.hasFixedDuration = true;
        flecha.duration = 0f;
        flecha.exitTime = 0f;
        flecha.offset = 0f;
        flecha.canTransitionToSelf = false;
        flecha.AddCondition(AnimatorConditionMode.Equals, VALOR, PARAMETRO);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        Debug.Log("[Levantarse] Listo. El Animator de la niña ahora tiene el estado " + ESTADO +
                  " con la flecha desde Any State (stateAnim == " + VALOR + ").");
    }
}
#endif
