using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public delegate void PlayerChangedChunks(Vector2Int newChunk);
    public static event PlayerChangedChunks OnPlayerChangedChunks;
    public static Transform PlayerTransform;

    public float speed;
    public float acceleration;
    public float deceletation;
    public bool useModifier = true;

    private float curSpeed;

    private Vector2 movementDir;
    private Vector2 oldMovementDir;
    private Rigidbody2D rb2d;

    // Start is called before the first frame update
    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        PlayerTransform = transform;
    }

    private void Start()
    {
        PortalBlock.OnPortalBlockUsed += PortalUsed;
    }

    private Vector2Int LastChunk = new Vector2Int(1000, 10000);
    // Update is called once per frame
    void FixedUpdate()
    {
        if(movementDir.magnitude > 0 && curSpeed >= 0)
        {
            oldMovementDir = movementDir;
            curSpeed += acceleration * speed * Time.deltaTime;
        }
        else
        {
            curSpeed -= deceletation * speed * Time.deltaTime;
        }
        var modifier = useModifier ? ChunkManager.GetMovementSpeed(Vector3Int.FloorToInt(transform.position).ToVector2Int()) : 1;
        curSpeed = Mathf.Clamp(curSpeed, 0, speed * modifier);
        rb2d.velocity = oldMovementDir * curSpeed;

        var CurChunk = Vector2Int.FloorToInt(new Vector2(transform.position.x, transform.position.y) / ChunkManager.ChunkWidth);
        if (CurChunk != LastChunk)
        {
            LastChunk = CurChunk;
            OnPlayerChangedChunks?.Invoke(CurChunk);
        }
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        movementDir = value.ReadValue<Vector2>();
    }

    private void PortalUsed(ChunkGenerator _, PortalBlock __, Vector2Int worldPos)
    {
        transform.position = worldPos.ToVector3Int() + Vector3.one * 0.5f;
    }
}
