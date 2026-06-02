using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Variables Salto")]
    public float jumpForce;
    public float groundRadius;
    public float groundCheckDistance;
    public LayerMask groundMask;
    private bool isGrounded;

    //Getters
    public bool IsGrounded 
    { 
        get { return isGrounded; }
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void OnUpdate()
    {
        //CheckGround();
        Jump();
    }

    // este metodo verifica si el player esta en contacto con un objeto o layer de tipo suelo
    public void CheckGround()
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, groundRadius, Vector2.down, groundCheckDistance, groundMask);

        // verificar si el circle cast esta colisionando con la layer ground
        if (hit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    public void Jump()
    {
        if(isGrounded)
        {
            playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, jumpForce);
        }
    }

    private void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Vector3 checkPosition = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(checkPosition, groundRadius);
    }
}
