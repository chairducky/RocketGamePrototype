using Assets.Scripts.Model.Interfaces;
using Player;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(GameInputManager))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ISlowable
{
    public static Vector2 PlayerPosition;

    [Header("Movement on ground")]
    public float walkTargetSpeed;
    public float OriginalWalkSpeed { get; private set; }
    [Range(0, 1)] [SerializeField] private float acceleration;
    [Range(0, 1)] [SerializeField] private float deceleration;

    [Header("Jump and air movement")]
    public float jumpForce;
    public float fallMultiplier;
    [Range(0, 1)][SerializeField] private float airAcceleration;
    [Range(0, 1)][SerializeField] private float airDeceleration;
    [Range(0, 1)][SerializeField] private float jumpUpDeceleration;
    [Range(0, 1)][SerializeField] private float airOverSpeedDeceleration;
    [Space]
    [Range(0, 1)][SerializeField] private float inWebVelocityDeceleration;

    [Header("Walls")]
    [SerializeField] private float maxWallSlideDownSpeed;
    [Range(0, 1)] public float wallSlideUpDeceleration;
    [Range(0, 1)] public float wallSlideDownAcceleration;
    [SerializeField] private float wallJumpMovementEffectDuration;

    [Header("Rocket jump")]
    [Range(0, 1)] [SerializeField] private float rocketJumpDeceleration;

    [NonSerialized] public bool IsJumping;
    [NonSerialized] public bool InRocketJump;
    [NonSerialized] public bool IsWallJumping;
    [NonSerialized] public bool IsWallGrab;

    private bool _inSpiderWeb;

    private GameInputManager _input;
    private Rigidbody2D _rb;
    private CollisionDetector _collisionDetector;

    public UnityEvent onJump = new(); 
    public UnityEvent onWallJump = new();
    public UnityEvent onRocketJump = new();


    private void Awake()
    {
        //Application.targetFrameRate = 144;
        _input = GetComponent<GameInputManager>();
        _rb = GetComponent<Rigidbody2D>();
        _collisionDetector = GetComponent<CollisionDetector>();
        OriginalWalkSpeed = walkTargetSpeed;
    }

    private void Update()
    {
        PlayerPosition = transform.position;

        if (_rb.velocity.y < 0)
        {
            IsJumping = false;
            InRocketJump = false;
        }

        if (_input.jump)
        {
            if (_collisionDetector.onGround)
                Jump(new Vector2(0, 1), false);
            else if (_collisionDetector.onWall)
                WallJump();
        }

        _input.jump = false;
    }

    private void FixedUpdate()
    {
        if (_inSpiderWeb)
        {
            _rb.velocity = Vector2.Lerp(_rb.velocity, Vector2.zero, inWebVelocityDeceleration);
        }

        Walk();
        CalculateInAirVelocity();
        if (_collisionDetector.onWall && _input.move.x != 0 && !Mathf.Sign(_collisionDetector.wallSide).Equals(Mathf.Sign(_input.move.x)))
            CalculateOnWallSlideVelocity();
    }

    private void Walk()
    {
        var targetSpeed = _input.move.x * walkTargetSpeed;
        var accelRate = Mathf.Abs(_input.move.x) > 0
            ? (_collisionDetector.onGround ? acceleration : airAcceleration)
            : (_collisionDetector.onGround ? deceleration : airDeceleration);

        if (IsWallJumping)
            accelRate /= 3;

        if (Mathf.Abs(_rb.velocity.x) > walkTargetSpeed && !_collisionDetector.onGround)
            accelRate = airOverSpeedDeceleration;

        var horizontalVelocity = Mathf.Lerp(_rb.velocity.x, targetSpeed, accelRate);

        _rb.velocity = new Vector2(horizontalVelocity, _rb.velocity.y);
    }
    
    
    private void Jump(Vector2 dir, bool onWall)
    {
        onJump.Invoke();
        IsJumping = true;
        _rb.velocity = new Vector2(_rb.velocity.x, 0) + dir * jumpForce;
    }

    private void WallJump()
    {
        onWallJump.Invoke();
        Vector2 wallDir = _collisionDetector.onRightWall ? Vector2.left : Vector2.right;

        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        IsJumping = true;
        StartCoroutine(WallJumpMovementEffectCooldown());
    }

    private IEnumerator WallJumpMovementEffectCooldown()
    {
        IsWallJumping = true;
        yield return new WaitForSeconds(wallJumpMovementEffectDuration);
        IsWallJumping = false;
    }

    private void CalculateOnWallSlideVelocity()
    {
        if (_rb.velocity.y > 0)
        {
            var upVelocity = Mathf.Lerp(_rb.velocity.y, 0, wallSlideUpDeceleration);
            _rb.velocity = new Vector2(_rb.velocity.x, upVelocity);
        }
        
        else if (_rb.velocity.y <= 0)
        {
            var downVelocity = Mathf.Lerp(_rb.velocity.y, -maxWallSlideDownSpeed, wallSlideDownAcceleration);
            _rb.velocity = new Vector2(_rb.velocity.x, downVelocity);
        }
    }

    private void CalculateInAirVelocity()
    {
        if(_rb.velocity.y < 0)
        {
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        var velocity = _rb.velocity;
        
        if (IsJumping || InRocketJump)
        {
            var decelerationMultiplier = InRocketJump ? rocketJumpDeceleration : jumpUpDeceleration;
            velocity = new Vector2(velocity.x, Mathf.Lerp(velocity.y, 0, decelerationMultiplier));
            _rb.velocity = velocity;
        }
    }

    public void RocketJump()
    {
        onRocketJump.Invoke();
        InRocketJump = true;
    }

    public bool IsWalking()
    {
        return Mathf.Abs(_rb.velocity.x) > 0 && _collisionDetector.onGround;
    }

    public int GetMoveDirection()
    {
        return (int)Mathf.Sign(_rb.velocity.x);
    }

    public void Slow(bool isSlow)
    {
        _inSpiderWeb = isSlow;
    }

    public void SetWalkSpeed(float value)
    {
        walkTargetSpeed = value;
    }
}
