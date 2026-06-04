using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerController playerController;
    private bool isFacingRight = true; // variable booleana para verificar si el player esta mirando a la derecha
    private bool isMoving; // verifica si el player se esta moviendo o no

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    
    public void Move() // controla el movimiento del player
    {
        // vector donde se almacena el valor del movimiento del player que se recibe desde el playerController
        Vector2 move = playerController.controles.Player.Move.ReadValue<Vector2>();

        // aplicamos la velocidad en x al rigidbody del player controller
        playerController.rb.linearVelocity = new Vector2(move.x * playerController.speed, playerController.rb.linearVelocity.y);

        // dentro del bool isMoving, guardamos si el player se mueve o no
        isMoving = move.x != 0;

        // llamado a la mecanica de FLIP
        if (move.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (move.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip() // controla la direccion del player
    {
        isFacingRight = !isFacingRight;

        // Damos vuelta SOLO el eje X usando un Vector3, para NO tocar la Y ni la Z.
        // OJO: antes se usaba Vector2, que dejaba la escala Z en 0 y eso rompia
        // el calculo de la camara (errores "Assertion failed... IsFinite(distanceForSort)").
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    public bool IsMoving // getter de la variable de isMoving que se actualiza en el metodo de Move para saber si el player se esta moviendo o no
    {
        get { return isMoving; }
    }
}