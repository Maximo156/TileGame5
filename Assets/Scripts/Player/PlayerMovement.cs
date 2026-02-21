using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour, IHittable
{
    public delegate void PlayerChangedChunks(Vector2Int newChunk);
    public static event PlayerChangedChunks OnPlayerChangedChunks;

    public static Transform PlayerTransform;

    public float speed;
    public float acceleration;
    public float deceletation;
    public bool useModifier = true;

    private Vector2 movementDir;
    private Vector2 oldMovementDir;
    private Rigidbody2D rb2d;

    private Timer hitTimer;
    private InputController inputController;

    // Start is called before the first frame update
    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        PlayerTransform = transform;
        inputController = GetComponent<InputController>();
    }

    private void Start()
    {
        PortalBlock.OnPortalBlockUsed += PortalUsed;
    }

    private Vector2Int LastChunk = new Vector2Int(1000, 10000);
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!inputController.AllowMovement)
        {
            rb2d.linearVelocity = Vector2.zero;
            return;
        }
        var curSpeed = rb2d.linearVelocity.magnitude;
        if (movementDir.magnitude > 0 && curSpeed >= 0 && hitTimer?.Expired != false)
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
        rb2d.linearVelocity = oldMovementDir * curSpeed;

        var CurChunk = Vector2Int.FloorToInt(new Vector2(transform.position.x, transform.position.y) / WorldConfig.ChunkWidth);
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

    private void PortalUsed(string _, PortalBlock __, Vector2Int worldPos)
    {
        transform.position = worldPos.ToVector3Int() + Vector3.one * 0.5f;
    }

    public void Hit(HitData info)
    {
        hitTimer = new Timer(0.2f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneUnloaded += ResetEvent;
    }

    static void ResetEvent(Scene _)
    {
        OnPlayerChangedChunks = null;
    }
}
