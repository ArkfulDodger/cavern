using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PhysicsObject : MonoBehaviour
{
    #region Variables

    //[Header("Gravity")]
    protected float terminalVelocityY = -10.0f;
    protected const float defaultGravityY = -28.125f;
    protected Vector2 gravity = new Vector2(0, defaultGravityY);
    protected float gravityModifier = 1.0f;

    //[Header("Movement")]
    protected float minMoveDistance = 0.01f;
    protected Vector2 velocity;
    protected Vector2 inputVelocity;
    protected float horizontalPush;

   // [Header("Collision")]
    bool slopesReduceVelocity;
    protected float colliderShell = 0.01f;
    protected Rigidbody2D rb2D;
    protected Collider2D baseCollider;
    protected BoxCollider2D boxCollider;
    protected CapsuleCollider2D capsuleCollider;
    protected CircleCollider2D headCollider;
    protected int layerMask;
    protected ContactFilter2D contactFilter;
    protected RaycastHit2D[] raycastHitArray = new RaycastHit2D[16];
    protected int nearestHitIndex;

   // [Header("Grounding")]
    protected float minGroundNormalY = 0.65f;
    protected float maxCeilingNormalY = -0.65f;
    protected bool grounded;
    protected Vector2 groundNormal;
    protected Vector2 groundForward;
    protected bool slopeChange;

    //[Header("Debugging")]
    protected bool debugMove;
    protected bool debugEscapes;
    protected bool debugCorner;
    protected bool debugRedirect;
    protected bool debugJitters;
    protected bool debugHorizontalInput;
    protected bool debugHorizontalPush;
    protected bool debugVertical;
    bool debugOn;
    Color debugColor1;
    Color debugColor2;

    #endregion

    #region Awake, Start, Update, Fixed Update

    protected virtual void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        baseCollider = boxCollider;
        headCollider = GetComponent<CircleCollider2D>();
    }

    protected virtual void Start()
    {
        // Initialize rigidbody
        rb2D.bodyType = RigidbodyType2D.Kinematic;
        rb2D.useFullKinematicContacts = true;

        // Initialize contactFilter
        Physics2D.IgnoreLayerCollision(6, 7, true); // ensures manual physics will ignore tunnel/player collisions
        Physics2D.IgnoreLayerCollision(6, 3, true);
        Physics2D.IgnoreLayerCollision(6, 10, true);
        Physics2D.IgnoreLayerCollision(6, 8, true);
        layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
        Physics2D.IgnoreLayerCollision(6, 7, false); // ensures game physics will detect tunnel/player collisions
        Physics2D.IgnoreLayerCollision(6, 3, false);
        Physics2D.IgnoreLayerCollision(6, 10, false);
        Physics2D.IgnoreLayerCollision(6, 8, false);
        contactFilter.SetLayerMask(layerMask);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;
    }

    protected virtual void Update()
    {
        inputVelocity = Vector2.zero;
    }

    protected virtual void FixedUpdate()
    {
        // Update Velocity
        UpdateVelocityForGravity();
        velocity.x = inputVelocity.x;

        // Apply Movement
        FixedUpdateHorizontalMove();
        if (horizontalPush != 0)
            FixedUpdateHorizontalPush();
        FixedUpdateVerticalMove();
    }

        #region Update Methods

        void UpdateVelocityForGravity()
        {
            if (grounded && inputVelocity == Vector2.zero)
            {
                return;
            }
            else
            {
                velocity += gravity * gravityModifier * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, terminalVelocityY);
            }
        }

        void FixedUpdateHorizontalMove()
        {
            if (debugHorizontalInput)
                DebugMoveActivate("H1", Color.green, Color.red);
            Vector2 moveVector = grounded ? groundForward : Vector2.right;
            float moveStrength = inputVelocity.x * Time.deltaTime;
            Vector2 move = AdjustForCollision(moveVector * moveStrength, false);
            Move(move);
            debugOn = false;
        }

        void FixedUpdateHorizontalPush()
        {
            if (debugHorizontalPush)
                DebugMoveActivate("H2", Color.cyan, Color.magenta);
            Vector2 moveVector = Vector2.right;
            float moveStrength = Mathf.Max(Mathf.Abs(horizontalPush * Time.deltaTime), minMoveDistance * 2) * Mathf.Sign(horizontalPush);
            Vector2 move = AdjustForCollision(moveVector * moveStrength, false);
            if (move.magnitude < Mathf.Abs(moveStrength))
                CheckVTrap(Mathf.Abs(moveStrength));
            Move(move);
            horizontalPush = 0;
            debugOn = false;
        }

        void FixedUpdateVerticalMove()
        {
            if (debugVertical)
                DebugMoveActivate("V", Color.white, Color.yellow);
            Vector2 moveVector = Vector2.up;
            float moveStrength = velocity.y * Time.deltaTime;
            Vector2 move = AdjustForCollision(moveVector * moveStrength, true);
            Move(move);
            debugOn = false;
        }

        #endregion

    #endregion

    #region Move and Collision Handling

    protected void Move(Vector2 move)
    {
        if (move.magnitude >= minMoveDistance)
        {
            rb2D.position += move;

            if (debugMove)
                Debug.Log("Move of (" + move.x + ", " + move.y + ") applied");
        }
    }

    protected bool CollisionDetected(Vector2 direction, float distance)
    {
        bool collisionDetected = false;

        int numberOfHits = rb2D.Cast(direction, contactFilter, raycastHitArray, distance + colliderShell);

        if (numberOfHits > 0)
        {            
            for (int i = 0; i < numberOfHits; i++)
            {
                if (debugOn)
                    DebugCollisionHit(raycastHitArray[i], i, direction);

                float distanceToHit = raycastHitArray[i].distance - colliderShell;

                if (distanceToHit < distance)
                {
                    float dotProduct = Vector2.Dot(direction, raycastHitArray[i].normal);

                    if (distanceToHit < 0 && dotProduct > 0)
                    {
                        if (debugEscapes)
                            Debug.Log("Escaped Block-In");
                    }
                    else
                    {
                        collisionDetected = true;
                        distance = distanceToHit;
                        nearestHitIndex = i;
                    }
                }
            }
        }

        return collisionDetected;
    }

    protected Vector2 AdjustForCollision(Vector2 move, bool isVerticalMove)
    {
        Vector2 direction = move.normalized;
        float distance = move.magnitude;

        if (distance > minMoveDistance || (isVerticalMove && distance > 0))
        {
            bool startedGrounded = grounded ? true : false;

            if (isVerticalMove)
                grounded = false;
            else
                slopeChange = false;
            
            if (CollisionDetected(direction, distance))
            {
                distance = raycastHitArray[nearestHitIndex].distance - colliderShell;
                Vector2 collisionNormal = raycastHitArray[nearestHitIndex].normal;
                Vector2 collisionPoint = raycastHitArray[nearestHitIndex].point;

                // Special adjustments for horizontal movement
                if (!isVerticalMove)
                {
                    velocity.x = 0;

                    if (grounded)
                    {
                        if (collisionNormal.y >= minGroundNormalY)
                        {
                            slopeChange = true;
                            SetGroundNormal(collisionNormal);
                        }
                        else if ((collisionNormal.y > 0.001 && (direction * distance).x < minMoveDistance * 2))
                        {
                            distance = 0;

                            if (debugJitters)
                                Debug.Log("Stopped Slope Approach with min move exception");
                        }
                    }
                }

                // Special adjustments for vertical movement
                else
                {
                    if (collisionNormal.y >= minGroundNormalY)
                    {
                        if (!startedGrounded)
                            Land(collisionPoint);
                        else
                            SetAsGrounded();

                        if (!slopeChange)
                            SetGroundNormal(collisionNormal);
                        collisionNormal = Vector2.up;
                    }
                    else if (collisionNormal.y <= maxCeilingNormalY)
                    {
                        HitCeiling();
                        collisionNormal = Vector2.down;
                    }

                    if (!grounded && collisionNormal.y > maxCeilingNormalY)
                    {
                        RedirectVelocityAlongSurface(collisionNormal);
                    }
                }
            }
        }

        return direction * distance;
    }

    void RedirectVelocityAlongSurface(Vector2 collisionNormal)
    {
        float projection = Vector2.Dot(velocity, collisionNormal);

        if (debugRedirect)
        {
            Debug.Log("velocity: " + velocity);
            Debug.Log("collision normal: " + collisionNormal);
            Debug.Log("projection: " + projection);
        }

        // if colliding with opposing surface, set horizontal push to slide along surface
        if (projection < 0)
        {
            Vector2 adjustedVelocity = velocity - projection * collisionNormal;
            if (debugRedirect)
            {
                Debug.Log("Adjusted Velocity: " + adjustedVelocity);
                Debug.DrawRay(raycastHitArray[nearestHitIndex].point, adjustedVelocity, Color.magenta, Time.deltaTime);
            }
            if (slopesReduceVelocity)
                velocity.y = adjustedVelocity.y;
            horizontalPush = adjustedVelocity.x;
        }

        // if colliding with corner or flat wall, adjust away from wall or corner
        else if (velocity != Vector2.zero && projection == 0)
        {
            if (debugCorner)
                Debug.Log("Corner Case avoided!");
            horizontalPush = collisionNormal.x > 0 ? minMoveDistance : - minMoveDistance;
        }
    }

    protected virtual void Land(Vector2 collisionPoint)
    {
        SetAsGrounded();
    }

    void SetAsGrounded()
    {
        grounded = true;
        gravityModifier = 1.0f;
        velocity = Vector2.zero;
    }

    protected void SetGroundNormal(Vector2 newNormal)
    {
        groundNormal = newNormal;
        if (Mathf.Abs(groundNormal.x) < 0.001)
            groundNormal = Vector2.up;

        groundForward = new Vector2(groundNormal.y, -groundNormal.x);
    }

    public bool IsGrounded()
    {
        return grounded;
    }

    protected virtual void HitCeiling()
    {
        // Set in Child
    }

    protected void CheckVTrap(float pushStrength)
    {
        // Checks for corner case where object is grounded between two opposing non-ground slopes

        Vector2 pointA = baseCollider.bounds.min;
        Vector2 pointB = new Vector2(pointA.x + baseCollider.bounds.size.x, pointA.y);
        int layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer);

        RaycastHit2D hitA = Physics2D.Raycast(pointA, Vector2.left, pushStrength + colliderShell, layerMask);
        RaycastHit2D hitB = Physics2D.Raycast(pointB, Vector2.right, pushStrength + colliderShell, layerMask);
        //Debug.DrawRay(pointA, Vector3.left * (pushStrength + colliderShell), Color.red, Time.deltaTime);
        //Debug.DrawRay(pointB, Vector3.right * (pushStrength + colliderShell), Color.yellow, Time.deltaTime);

        if (hitA && hitB)
        {
            if (hitA.normal.y < minGroundNormalY && hitA.normal.x > 0 && hitB.normal.y < minGroundNormalY && hitB.normal.x < 0)
            {
                //Debug.Log("VTrap Fixed");
                Land(pointA);
                SetGroundNormal(Vector2.up);
                FixVTrap();
            }
        }
    }

    protected virtual void FixVTrap()
    {
        // To Set in Child Entities
    }

    #endregion

    #region Debug Methods

    void DebugMoveActivate(string tag, Color color1, Color color2)
    {
        debugOn = true;
        Debug.Log(tag);
        debugColor1 = color1;
        debugColor2 = color2;
    }

    void DebugCollisionHit(RaycastHit2D hit, int i, Vector2 direction)
    {
        float colorDim = i * 0.4f;
        Color color1 = new Color(debugColor1.r - colorDim, debugColor1.g - colorDim, debugColor1.b - colorDim, 255);
        Color color2 = new Color(debugColor2.r - colorDim, debugColor2.g - colorDim, debugColor2.b - colorDim, 255);

        // Get collision point and surface normal
        Vector2 point = hit.point;
        Vector2 normal = hit.normal;

        // Draw ray from collision point away at surface normal angle
        Debug.DrawRay(point, normal, color1, Time.deltaTime);
        Debug.DrawRay(point, Vector2.Perpendicular(normal) * 0.1f, color1, Time.deltaTime);
        Debug.DrawRay(point, Vector2.Perpendicular(normal) * -0.1f, color1, Time.deltaTime);

        // Print Hit Details to Log
        Debug.Log("-- Hit[" + i + "] is " + hit.collider + "at " + point + " with a Normal of " + normal);

        // Draw Ray from collision point to movement origin
        Debug.DrawRay(point, -direction.normalized * hit.distance, color2, Time.deltaTime);
    }

    #endregion
}
