using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Cinemachine;
using System;


public enum Form {worm, claws, legs, glider, deep}
public enum State {overworld, climb, glide, tunnel, debug}
public enum Direction {up, down, left, right}

[RequireComponent (typeof (SpriteRenderer), typeof (Animator))]
public class PlayerController : PhysicsObject
{
    #region Variables

        //[Header("Speed")]
        float walkSpeed = 3.125f; // original game value: 3.125
        float climbSpeed = 2.1875f;  // original game value: 2.1875
        float jumpVelocity = 11f; // original game value: 10.9375
        float glideDX = 4.6875f; // original game value: 4.6875
        float glideDY = -1.09375f; // original game value: -1.09375
        float glideTurn = 25f; // original game value: 25
        float tunnelEnterExitSpeed = 1.09375f; // original game value: 1.09375
        float tunnelSpeed = 3.125f; // original game value: 3.125
        float fastDropModifier = 3f;
        float headBumpVelocityModifier = 0.85f;
        float climbJumpModifier = 1f;
        float speedModifier = 1f;
        float speed;

        [Header("Forms and States")]
        public Form form = Form.glider;
        [SerializeField] RuntimeAnimatorController legsAnimator;
        [SerializeField] AnimatorOverrideController clawsAnimator;
        [SerializeField] AnimatorOverrideController wormAnimator;
        [SerializeField] State state;
        
        bool listenLearned;
        bool listening;
        // bool transforming;
        float transformFadeTime = 3.3f;
        float transformPauseTime = 0.5f;
        float transformIntakeTime = 0.5f;
        float transformBloomTime = 0.2f;
        float transformHoldTime = 1f;
        float transformDimTime = 2f;



        //[Header("Climbing")]
        float grabDistance = 0.38f;
        float grabDistanceAirModifier = 1.3f;
        float grabTestOffset = 0.0001f;
        bool grabActive;
        float dismountTimeMod = 2f;
        Vector2 wallNormal;
        Vector2 climbUp;
        Transform grabPoint;
        Transform footPoint;
        Vector2 grabPointLocalPosition;
        Vector2 footPointLocalPosition;
        float climbBodyLength;
        float climbBodyWidth;
        ContactPoint2D[] headContactPoints = new ContactPoint2D[16];
        ContactPoint2D[] bodyContactPoints = new ContactPoint2D[16];
        bool cresting;
        float crestingVal;

        //[Header("Tunneling")]
        bool prioritizeVertical;
        bool atEntrance;
        public float autoEntryTime;
        bool assessingAutoEntry;
        public bool entering;
        public bool exiting;
        
        List<Vector2> entryPoints = new List<Vector2>();
        bool exitMoveInitiated;
        bool reentryAvailable;

        // Blinking
        float minBlinkTime = 3;
        float maxBlinkTime = 6;
        int doubleBlinkChance = 3;
        float blinkTimer = 0;
        float blinkTime = 3;
        float doubleBlinkTime = 0.23f;

        [Header("Input Handling")]
        [SerializeField] bool updateLocked;
        [SerializeField] bool inputLocked;
        [SerializeField] bool jumpTrigger;
        [SerializeField] bool jumpRelease;
        [SerializeField] bool glideTrigger;
        [SerializeField] bool glideRelease;
        [SerializeField] bool grabTrigger;
        [SerializeField] bool enterTrigger;
        [SerializeField] bool horizontalInputOverride;
        [SerializeField] float horizontalInput;
        [SerializeField] bool verticalInputOverride;
        [SerializeField] float verticalInput;
        [SerializeField] bool runActive;
        [SerializeField] bool horizontalInputLock;
        Vector2 faceDirection = Vector2.right;

        //[Header("More Debugging")]
        bool debugClimbing;

        // Component Variables
        SpriteRenderer spriteRenderer;
        SpriteRenderer spriteOverlay;
        Animator animator;
        CinemachineVirtualCamera vCamPlayer;
        PlayerAudio playerAudio;
        PlayerLight playerLight;

    #endregion

    #region Start, Update, and Fixed Update

    protected override void Awake()
    {
        base.Awake();
        vCamPlayer = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineVirtualCamera>();
    }

    private void OnEnable()
    {
        EventBroker.DeepSenseExit += ReleaseUpdateLock;
        EventBroker.EnterDialogue += EnterDialogue;
        EventBroker.ExitDialogue += ExitDialogue;
        EventBroker.InitiateTransformation += InitiateTransformation;
        EventBroker.LookStart += InitiateLook;
        EventBroker.ExitPlayerBegin += ExitPlayer;
        EventBroker.StartFinalDialogue += FreezePlayerForEnding;
    }

    private void OnDisable()
    {
        EventBroker.DeepSenseExit -= ReleaseUpdateLock;
        EventBroker.EnterDialogue -= EnterDialogue;
        EventBroker.ExitDialogue -= ExitDialogue;
        EventBroker.InitiateTransformation -= InitiateTransformation;
        EventBroker.LookStart -= InitiateLook;
        EventBroker.ExitPlayerBegin -= ExitPlayer;
        EventBroker.StartFinalDialogue -= FreezePlayerForEnding;
    }

    protected override void Start()
    {
        base.Start();

        // Get Necessary Components and References
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteOverlay = transform.Find("SpriteOverlay").GetComponent<SpriteRenderer>();
        spriteOverlay.enabled = false;
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<PlayerAudio>();
        playerLight = transform.Find("Player Light").GetComponent<PlayerLight>();
        grabPoint = transform.Find("GrabPoint");
        grabPointLocalPosition = grabPoint.localPosition;
        footPoint = transform.Find("FootPoint");
        footPointLocalPosition = footPoint.localPosition;
        climbBodyLength = Vector2.Distance((Vector2)grabPoint.position, (Vector2)footPoint.position);
        climbBodyWidth = Mathf.Abs(footPoint.localPosition.x) * 2;

        // Set Interpolation Off for Player
        rb2D.interpolation = RigidbodyInterpolation2D.None;

        // Initialize State
        EnterState(State.overworld);
        if (CollisionDetected(Vector2.down, grabTestOffset) && raycastHitArray[nearestHitIndex].normal.y >= minGroundNormalY)
            grounded = true;
        else
            grounded = false;

        // Initialize Form
        if (GameManager.instance.isNewGame)
        {
            SetForm(Form.worm);
            listenLearned = false;
        }
        else
        {
            SetForm(form);
            listenLearned = true;
        }
    }

    protected override void Update()
    {
        // if (!transforming)
        //     DevUpdateListen();

        if (!updateLocked && !inputLocked)
        {
            DevUpdateForm();

            base.Update();

            switch (state)
            {
                case State.overworld:
                {
                    UpdateHorizontalInput();
                    UpdateDeepSenseInput();
                    UpdateEntranceInput();
                    UpdateFaceDirection();
                    UpdateGlideInput();
                    UpdateJumpInput();
                    UpdateClimbInput();
                    break;
                }
                
                case State.climb:
                {
                    UpdateVerticalInput();
                    UpdateDeepSenseInput();
                    UpdateEntranceInput();
                    UpdateJumpInput();
                    break;
                }

                case State.glide:
                {
                    UpdateHorizontalInput();
                    UpdateGlideInput();
                    break;
                }
                
                case State.tunnel:
                {
                    UpdateTunnelInput();
                    UpdateTunnelFaceDirection();
                    UpdateDeepSenseInput();
                    break;
                }

                case State.debug:
                {
                    break;
                }
                
                default:
                {
                    Debug.LogError("no state assigned for Player update");
                    break;
                }
            }
        }

        UpdateAnimator();
    }

    protected override void FixedUpdate()
    {
        if (updateLocked)
            return;
        
        switch (state)
        {
            case State.overworld:
            {
                base.FixedUpdate();
                break;
            }

            case State.climb:
            {
                FixedUpdateClimb();
                break;
            }

            case State.glide:
            {
                // Set vertical and horizontal velocity of glide based on direction
                velocity.y = glideDY;
                if (velocity.x != glideDX * faceDirection.x)
                {
                    velocity.x += glideTurn * faceDirection.x * Time.deltaTime;
                    velocity.x = Mathf.Clamp(velocity.x, -glideDX, glideDX);
                }
                float velXSave = velocity.x; // cache x velocity
                
                // Execute Glide Movement
                GlideMove();
                
                // If horizontal collision this frame and not currently turning, attempt to climb
                if (velocity.x == 0)
                {
                    velocity.x = velXSave; // restore chached x velocity to prevent stuttering

                    if (faceDirection.x == transform.localScale.x)
                    {
                        if (CanClimb(out RaycastHit2D grabHit))
                            GlideWallGrab(grabHit);
                    }
                }
                
                break;
            }

            case State.tunnel:
            {
                velocity = inputVelocity;
                if (velocity != Vector2.zero)
                    if (exitMoveInitiated)
                        StartCoroutine(ExitTunnel());
                    else
                        StartCoroutine(TunnelMove());
                break;
            }

            case State.debug:
            {
                break;
            }

            default:
            {
                Debug.LogError("no state assigned for Player fixed update");
                break;
            }
        }
    }

    #region Update Methods
    

    
    void UpdateHorizontalInput()
    {
        // Get current horizontal input unless overriding (for debugging)
        if (!horizontalInputOverride && !inputLocked)
            horizontalInput = Input.GetAxisRaw("Horizontal");

        // Apply input to velocity at current speed unless locked (for corner cases)
        if (!horizontalInputLock)
            inputVelocity.x = horizontalInput * speed * speedModifier;
    }

    void UpdateVerticalInput()
    {
        // Get current vertical input unless overriding (for debugging)
        if (!verticalInputOverride && !inputLocked)
            verticalInput = Input.GetAxisRaw("Vertical");

        // Apply input to velocity at current speed
        inputVelocity.y = verticalInput * speed * speedModifier;
    }

    void UpdateFaceDirection()
    {
        // Update right/left direction to match last received input
        if ((transform.localScale.x < 0 && horizontalInput > 0) || (transform.localScale.x > 0 && horizontalInput < 0))
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            faceDirection = horizontalInput > 0 ? Vector2.right : Vector2.left;
        }
    }

    void UpdateDeepSenseInput()
    {
        if (form < Form.deep || inputVelocity != Vector2.zero ||
            state == State.glide || (state == State.overworld && !grounded))
            return;

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            updateLocked = true;
            EventBroker.DeepSenseEnterCall();
        }
    }

    void ReleaseUpdateLock()
    {
        updateLocked = false;
    }

    void UpdateEntranceInput()
    {
        if (!atEntrance)
            return;
        
        Direction enterDirection = 0;

        // Enter Up if not grounded
        if ((((Input.GetKey(KeyCode.UpArrow) || enterTrigger) && (state == State.overworld && !grounded) || state == State.climb))
            && CanEnterTunnel(KeyCode.UpArrow))
        {
            // Debug.Log("Entered Up from Input");
            enterDirection = Direction.up;
        }

        // Enter Down if grounded
        else if ((Input.GetKeyDown(KeyCode.DownArrow) || (reentryAvailable && Input.GetKey(KeyCode.DownArrow)) || enterTrigger) &&
            grounded && CanEnterTunnel(KeyCode.DownArrow))
        {
            // Debug.Log("Entered Down from Input");
            enterDirection = Direction.down;
        }


        // Enter Left immediately if facing left and pressed or able to reenter and down, and in climb or overworld
        else if (((faceDirection.x < 0 && Input.GetKeyDown(KeyCode.LeftArrow)) || (reentryAvailable && Input.GetKey(KeyCode.LeftArrow)) || enterTrigger) &&
            (state == State.climb || state == State.overworld) && CanEnterTunnel(KeyCode.LeftArrow))
        {
            // Debug.Log("Entered Left from Input");
            enterDirection = Direction.left;
        }
        // Assess auto enter Left if input is correct but did not immediately enter
        else if (horizontalInput < 0 && !assessingAutoEntry && CanEnterTunnel(KeyCode.LeftArrow))
        {
            // Debug.Log("Assessing Auto-Left from Input");
            StartCoroutine(AssessAutoEnter(Direction.left));
            return;
        }

        // Enter Right immediately if facing right and pressed or able to reenter and down, and in climb or overworld
        else if (((faceDirection.x > 0 && Input.GetKeyDown(KeyCode.RightArrow)) || (reentryAvailable && Input.GetKey(KeyCode.RightArrow)) || enterTrigger) &&
        (state == State.climb || (state == State.overworld)) && CanEnterTunnel(KeyCode.RightArrow))
        {
            // Debug.Log("Entered Right from Input");
            enterDirection = Direction.right;
        }
        // Assess auto Right enter if input is correct but did not immediately enter
        else if (horizontalInput > 0 && !assessingAutoEntry && CanEnterTunnel(KeyCode.RightArrow))
        {
            // Debug.Log("Assessing Auto-Right from Input");
            StartCoroutine(AssessAutoEnter(Direction.right));
            return;
        }

        // If no entry occurred turn off reentry and play fall sound if needed
        else
        {
            if (reentryAvailable)
            {
                reentryAvailable = false;
                if (!grounded && state == State.overworld)
                    playerAudio.PlayOneShotVocalFromList(playerAudio.fallVocals);
            }
            return;
        }

        // If entry occurring, enter
        StopAllCoroutines();
        StartCoroutine(EnterTunnel(enterDirection, GetEntryPoint()));
    }

    void UpdateJumpInput()
    {
        switch (state)
        {
            // Overworld jumping only allowed if grounded, mid-air release increases fall speed
            case State.overworld:
            {
                if (form >= Form.legs)
                {
                    if ((grounded & Input.GetButtonDown("Jump")) || (grounded & jumpTrigger))
                    {
                        Jump();
                    }

                    if ((!grounded & Input.GetButtonUp("Jump")) || (!grounded & jumpRelease))
                    {
                        jumpRelease = false;
                        gravityModifier = fastDropModifier;
                    }
                }
                break;
            }

            // Climb jumping modifies jump velocity
            case State.climb:
            {
                if (Input.GetButtonDown("Jump") || jumpTrigger)
                {
                    // rotate to upright around different points based on incline to prevent grab point ever being inside a wall
                    if (wallNormal.y < 0)
                        RotateToUprightAround(grabPoint.position);
                    else 
                        RotateToUprightAround(spriteRenderer.bounds.center);

                    Jump();

                    if (Input.GetKey(KeyCode.DownArrow) || form < Form.legs)
                        velocity.y = 0;
                    else
                        velocity.y *= climbJumpModifier;

                }
                break;
            }

            case State.glide:
            {
                // Do nothing if entered glide state this update
                break;
            }

            default:
            {
                Debug.LogError("Jump attempted in invalid state");
                break;
            }
        }
    }

    void UpdateGlideInput()
    {
        switch (state)
        {
            case State.overworld:
            {
                if (!grounded && (Input.GetButtonDown("Jump") || glideTrigger))
                {
                    // If climbable surface immediately ahead, climb, don't glide
                    if (form >= Form.claws && CanClimb(out RaycastHit2D grabHit))
                    {
                        playerAudio.PlayOneShotSound(playerAudio.hitWall);
                        StopAllCoroutines();
                        StartCoroutine(GrabWall(grabHit));
                    }
                    else if (form >= Form.glider)
                        EnterState(State.glide);
                }
                break;
            }

            case State.glide:
            {
                // Exit Glide if jump button released
                if (Input.GetButtonUp("Jump") || glideRelease)
                {
                    EnterState(State.overworld);
                    break;
                }

                // Update the active face direction to match last received horizontal input (ignore 0)
                if ((faceDirection.x < 0 && horizontalInput > 0) || (faceDirection.x > 0 && horizontalInput < 0))
                    faceDirection = horizontalInput > 0 ? Vector2.right : Vector2.left;

                // Flip sprite and adjust rotation if velocity and scale don't match (if sprite direction doesn't match movement)
                if (Mathf.Sign(velocity.x) != Mathf.Sign(transform.localScale.x))
                {
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    transform.rotation = Quaternion.Euler(0, 0, -90 * transform.localScale.x);
                }

                break;
            }
        }
    }

    void UpdateClimbInput()
    {
        if (form >= Form.claws)
        {
            // allow climb attempts again if grab button released since last lock
            if (!grabActive && Input.GetKeyUp(KeyCode.UpArrow))
                grabActive = true;
            
            if (grabActive && ((Input.GetKey(KeyCode.UpArrow) || grabTrigger)))
            {
                if (CanClimb(out RaycastHit2D grabHit))
                {
                    StopAllCoroutines();
                    StartCoroutine(GrabWall(grabHit));
                }
            }
        }
    }

    void UpdateTunnelInput()
    {
        // Get current horizontal input unless overriding (for debugging)
        if (!horizontalInputOverride && !inputLocked)
            horizontalInput = Input.GetAxisRaw("Horizontal");

        // Get current vertical input unless overriding (for debugging)
        if (!verticalInputOverride && !inputLocked)
            verticalInput = Input.GetAxisRaw("Vertical");

        RestrictTunnelInput();

        inputVelocity.x = horizontalInput * speed * speedModifier;
        inputVelocity.y = verticalInput * speed * speedModifier;
    }

    void UpdateTunnelFaceDirection()
    {
        // face direction matches last non-zero input velocity direction
        if (inputVelocity != Vector2.zero)
        {
            faceDirection = inputVelocity.normalized;

            // change scale to match "left" or "non-left" face direction
            if (faceDirection.x < 0 && transform.localScale.x > 0 || faceDirection.x >= 0 && transform.localScale.x < 0)
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

            // ensure rotation matches current face direction rotation
            if (transform.eulerAngles.z != -90 * faceDirection.x)
                transform.rotation = Quaternion.Euler(0, 0, -90 * faceDirection.x);
        }
    }

    void UpdateAnimator()
    {
        animator.SetBool("listening", listening);
        animator.SetBool("climbing", state == State.climb);
        animator.SetBool("grounded", grounded);
        animator.SetFloat("horizontal input", horizontalInput);
        animator.SetFloat("vertical input", verticalInput);
        animator.SetFloat("glide velocity", Mathf.Abs(velocity.x)/glideDX);
        animator.SetBool("cresting", cresting);
        animator.SetFloat("crest", crestingVal);
        animator.SetFloat("face horizontal", faceDirection.x);
        animator.SetFloat("face vertical", faceDirection.y);
        animator.SetBool("tunnel move", inputVelocity.magnitude > 0 && state == State.tunnel);
        if (IsBlink())
            animator.SetTrigger("blink");

        UpdateMovementAudio();
        // gliding is updated in transitions to prevent premature rotation
    }

    void UpdateMovementAudio()
    {
        if (state == State.overworld && grounded && inputVelocity.x != 0 && !entering)
        {
            if (playerAudio.sfxSource.clip != playerAudio.overworldMove || !playerAudio.sfxSource.isPlaying)
                playerAudio.StartSoundLoop(playerAudio.overworldMove);
        }
        else
            playerAudio.StopSoundLoopImmediately(playerAudio.overworldMove);

        if (state == State.climb && inputVelocity.y != 0 && !cresting && !entering)
        {
            if (playerAudio.sfxSource.clip != playerAudio.climb || !playerAudio.sfxSource.isPlaying)
                playerAudio.StartSoundLoop(playerAudio.climb);
        }
        else
            playerAudio.StopSoundLoopImmediately(playerAudio.climb);
    }

    #endregion

    #region Fixed Update Methods

    void FixedUpdateClimb()
    {
        // Get proposed move this frame based on current wall
        velocity = inputVelocity.y * climbUp;
        Vector2 move = velocity * Time.deltaTime;

        if (move != Vector2.zero)
        {
            // Adjust Move based on change in wall, then execute
            move = AdjustClimbMove(move);
            ClimbMove(move);

            // Reverse movement if it results in collision and/or grounding
            if (IsHeadBump() || IsBaseBump() || grounded)
                ReverseClimbMove(move);

            // Get off wall if grounded
            if (grounded)
            {
                StopAllCoroutines();
                StartCoroutine(GetOffWall());
            }
        }
    }

    #endregion

    #endregion

    #region State and Form Handling
    void EnterState(State newState)
    {
        ExitState(state);

        switch (newState)
        {
            case State.overworld:
            {
                state = newState;
                speed = walkSpeed;
                speedModifier = 1f;
                gravityModifier = 1f;
                if (transform.rotation != Quaternion.identity)
                    StartCoroutine(SetStateRotation(Quaternion.identity));
                grabActive = Input.GetKey(KeyCode.UpArrow) ? false : true; // wait until grab button released to allow next grab attempt
                verticalInput = 0;
                inputVelocity.y = 0;
                break;
            }
            
            case State.climb:
            {
                state = newState;
                speed = climbSpeed;
                gravityModifier = 1f;
                horizontalInputLock = false; // reset in case exiting VTrap
                horizontalInput = 0;
                SetGroundNormal(Vector2.up);
                grounded = false; // always default grounded to false while climbing for landing detection
                break;
            }

            case State.glide:
            {                
                state = newState;
                velocity = new Vector2(glideDX * faceDirection.x, glideDY);
                speedModifier = 1f;
                gravityModifier = 1f;
                playerAudio.StartSoundLoop(playerAudio.glide);
                StartCoroutine(SetStatePositionAndRotation(GlideStartPosition(), Quaternion.Euler(0, 0, -90 * faceDirection.x)));
                break;
            }

            case State.tunnel:
            {
                state = newState;
                speed = tunnelSpeed;
                speedModifier = 1f;
                exitMoveInitiated = false;
                break;
            }

            default:
            {
                Debug.LogError("Attempted to enter unlisted state");
                break;
            }
        }
    }

    void ExitState(State currentState)
    {
        switch (currentState)
        {
            case State.glide:
            {
                playerAudio.StopSoundLoopImmediately(playerAudio.glide);
                break;
            }

            case State.climb:
            {
                if (cresting)
                {
                    cresting = false;
                    updateLocked = false;
                    StopAllCoroutines();
                }
                break;
            }

            default:
            {
                break;
            }
        }
    }

    public State GetState()
    {
        return state;
    }

    void DevUpdateForm()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetForm(Form.worm);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetForm(Form.claws);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetForm(Form.legs);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetForm(Form.glider);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SetForm(Form.deep);
    }

    // void DevUpdateListen()
    // {
    //     // if (Input.GetKeyDown(KeyCode.Return))
    //     // {
    //     //     if (!listening && CanListen())
    //     //         EnterDialogue(transform.position);
    //     //     else
    //     //         ExitDialogue();
    //     // }

    //     if (listening)
    //     {
    //         if (Input.GetKeyDown(KeyCode.Alpha6))
    //             StartCoroutine(TransformSequence(Form.worm));
    //         else if (Input.GetKeyDown(KeyCode.Alpha7))
    //             StartCoroutine(TransformSequence(Form.claws));
    //         else if (Input.GetKeyDown(KeyCode.Alpha8))
    //             StartCoroutine(TransformSequence(Form.legs));
    //         else if (Input.GetKeyDown(KeyCode.Alpha9))
    //             StartCoroutine(TransformSequence(Form.glider));
    //         else if (Input.GetKeyDown(KeyCode.Alpha0))
    //             StartCoroutine(TransformSequence(Form.deep));
    //     }
    // }

    void SetForm(Form newForm)
    {
        form = newForm;

        switch (form)
        {
            case Form.worm:
            {
                capsuleCollider.enabled = true;
                boxCollider.enabled = false;
                baseCollider = capsuleCollider;
                animator.runtimeAnimatorController = wormAnimator;
                break;
            }

            case Form.claws:
            {
                capsuleCollider.enabled = true;
                boxCollider.enabled = false;
                baseCollider = capsuleCollider;
                animator.runtimeAnimatorController = clawsAnimator;
                break;
            }

            case Form.legs:
            {
                capsuleCollider.enabled = false;
                boxCollider.enabled = true;
                baseCollider = boxCollider;
                animator.runtimeAnimatorController = legsAnimator;
                break;
            }

            case Form.glider:
            {
                capsuleCollider.enabled = false;
                boxCollider.enabled = true;
                baseCollider = boxCollider;
                animator.runtimeAnimatorController = legsAnimator;
                break;
            }

            case Form.deep:
            {
                capsuleCollider.enabled = false;
                boxCollider.enabled = true;
                baseCollider = boxCollider;
                animator.runtimeAnimatorController = legsAnimator;
                break;
            }

            default:
            {
                Debug.LogError("attempted to set invalid form");
                break;
            }   
        }

        playerAudio.UpdateFormSounds();
    }

    public bool CanListen()
    {
        if (state == State.overworld && grounded)
            return true;
        else
            return false;
    }

    void EnterDialogue()
    {
        // if (state == State.overworld)
        //     FaceTowardPoint(center);

        updateLocked = true;
        if (listenLearned)
            listening = true;
        horizontalInput = 0;
        verticalInput = 0;
        inputVelocity = Vector2.zero;
    }

    void FaceTowardPoint(Vector2 point)
    {
        if (transform.position.x < point.x)
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            faceDirection = Vector2.right;
        }
        else if (transform.position.x > point.x)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
            faceDirection = Vector2.left;
        }
    }

    void ExitDialogue()
    {
        updateLocked = false;
        listening = false;
    }

    #endregion

    #region Sequences

    void InitiateTransformation()
    {
        Form targetForm = Enum.IsDefined(typeof(Form), form + 1) ? form + 1 : form;

        StartCoroutine(TransformSequence(targetForm));
    }

    IEnumerator TransformSequence(Form newForm)
    {
        EventBroker.TransformBegunCall();
        // transforming = true;
        //updateLock already on from Listen status

        //Fade Out
        float time = 0f;
        float duration = transformFadeTime;

        while (time < duration)
        {
            spriteRenderer.color = Vector4.Lerp(Color.white, Color.black, time/duration);
            playerLight.StandardLightLerp(playerLight.foregroundLight, playerLight.currentLightPreset.foregroundLightSettings,
                playerLight.transformationOut.foregroundLightSettings, time/duration);
            playerLight.StandardLightLerp(playerLight.faceLight, playerLight.currentLightPreset.faceLightSettings,
                playerLight.transformationOut.faceLightSettings, time/duration);

            time += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = Color.black;
        playerLight.SetLightToSettings(playerLight.foregroundLight, playerLight.transformationOut.foregroundLightSettings);
        playerLight.SetLightToSettings(playerLight.faceLight, playerLight.transformationOut.faceLightSettings);
        
        
        //Hold for pause
        yield return new WaitForSeconds(transformPauseTime);


        //Change for whoosh and hold
        spriteRenderer.color = Vector4.zero;
        SetForm(newForm);
        spriteOverlay.enabled = true;
        playerAudio.PlayOneShotSound(playerAudio.transformWhoosh);
        yield return new WaitForSeconds(transformIntakeTime);

        //Transform and blast back in
        if (form == Form.glider)
            animator.Play("PostTransformationGlider", 0);
        else
            animator.Play("PostTransformation", 0);

        spriteOverlay.enabled = false;
        spriteRenderer.color = Vector4.one;
        playerAudio.PlayOneShotSound(playerAudio.transformTone);
        EventBroker.UpgradeCall();

        time = 0;
        duration = transformBloomTime;
        while (time < duration)
        {
            playerLight.StandardLightLerp(playerLight.foregroundLight, playerLight.transformationOut.foregroundLightSettings,
                playerLight.transformationIn.foregroundLightSettings, time/duration);
            playerLight.StandardLightLerp(playerLight.faceLight, playerLight.transformationOut.faceLightSettings, 
                playerLight.transformationIn.faceLightSettings, time/duration);

            time += Time.deltaTime;
            yield return null;
        }
        playerLight.SetLightToSettings(playerLight.foregroundLight, playerLight.transformationIn.foregroundLightSettings);
        playerLight.SetLightToSettings(playerLight.faceLight, playerLight.transformationIn.faceLightSettings);

        yield return new WaitForSeconds(transformHoldTime);

        //Fade to normal
        time = 0;
        duration = transformDimTime;
        while (time < duration)
        {
            playerLight.StandardLightLerp(playerLight.foregroundLight, playerLight.transformationIn.foregroundLightSettings,
                playerLight.currentLightPreset.foregroundLightSettings, time/duration);
            playerLight.StandardLightLerp(playerLight.faceLight, playerLight.transformationIn.faceLightSettings, 
                playerLight.currentLightPreset.faceLightSettings, time/duration);

            time += Time.deltaTime;
            yield return null;
        }
        playerLight.SetLightToSettings(playerLight.foregroundLight, playerLight.currentLightPreset.foregroundLightSettings);
        playerLight.SetLightToSettings(playerLight.faceLight, playerLight.currentLightPreset.faceLightSettings);

        animator.Play("Listen");
        EventBroker.TransformCompleteCall();
        // transforming = false;
        yield return null;
    }

    
    void InitiateLook()
    {
        StartCoroutine(LookSequence());
    }
    
    IEnumerator LookSequence()
    {
        yield return new WaitForSeconds(1);

        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

        yield return new WaitForSeconds(0.7f);

        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        
        yield return new WaitForSeconds(1.5f);

        listenLearned = true;
        listening = true;

        yield return new WaitForSeconds(1);

        EventBroker.LookEndCall();
    }

    public void StartZombieX(Vector2 point, bool vertical, bool groundingReq, float pause)
    {
        // Debug.Log("Zombie Called");
        StartCoroutine(ZombieToX(point, vertical, groundingReq, pause));
    }

    IEnumerator ZombieToX(Vector2 point, bool vertical, bool groundingReq, float pause)
    {
        // Debug.Log("Zombie Coroutine Started");
        // Debug.Log("Zombie routine started");

        inputLocked = true;
        bool targetReached = false;

        while (!targetReached)
        {
            horizontalInput = point.x < transform.position.x ? -1 : 1;
            verticalInput = point.y < transform.position.y ? -1 : 1;
            if (state == State.tunnel)
                UpdateTunnelInput();
            else
            {
                UpdateHorizontalInput();
                UpdateVerticalInput();
            }

            if (vertical)
                targetReached = Mathf.Abs(transform.position.y - point.y) < 0.1f;
            else
                targetReached = Mathf.Abs(transform.position.x - point.x) < 0.1f;

            yield return null;
        }
        //Debug.Log("reached target point");
        horizontalInput = 0;
        verticalInput = 0;
        inputVelocity = Vector2.zero;

        if (state == State.glide)
            EnterState(State.overworld);

        if (state == State.climb)
        {
            if (cresting)
            {                
                while (state == State.climb)
                {
                    yield return null;
                }
            }
            else
            {
                // Debug.LogError("Could not zombie out of climb");
                yield break;
            }
        }

        if (groundingReq)
        {
            while (!grounded)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(pause);
    
        
        inputLocked = false;

        ZombieOut(GameManager.instance.currentZombieOutEvent);
    }

    void ZombieOut(ZombieOutEvent zEvent)
    {
        // Debug.Log("Zombie Out Called");
        // Debug.Log("Zombie Out called for " + zEvent);
        switch (zEvent)
        {
            case ZombieOutEvent.cavernListen:
            {
                GameManager.instance.UpdateListenRegion(Region.centralCavern);
                EventBroker.EnterDialogueCall();
                break;
            }

            case ZombieOutEvent.deepListen:
            {
                GameManager.instance.UpdateListenRegion(Region.deep);
                EventBroker.EnterDialogueCall();
                break;
            }

            case ZombieOutEvent.ascentListen:
            {
                GameManager.instance.UpdateListenRegion(Region.finalAscent);
                EventBroker.EnterDialogueCall();
                break;
            }

            case ZombieOutEvent.enterExitChamber:
            {
                EventBroker.EnteredExitChamberCall();
                break;
            }

            case ZombieOutEvent.leaveExitChamber:
            {
                EventBroker.LeftExitChamberCall();
                break;
            }

            default:
            {
                break;
            }
        }
    }

    void ExitPlayer()
    {
        updateLocked = true;

        StopAllCoroutines();
    }

    void FreezePlayerForEnding()
    {
        EnterState(State.overworld);
        grounded = true;
        inputVelocity = Vector2.zero;
        horizontalInput = 0;
        verticalInput = 0;
    }

    #endregion

    #region Overworld
    void Jump()
    {
        playerAudio.PlayOneShotVocalFromList(playerAudio.jumpVocals);

        // Reset Input controls related to jumping
        jumpTrigger = false;
        horizontalInputLock = false;

        // apply jump velocity and set groundedness
        velocity.y = jumpVelocity;
        grounded = false;

        // Transition to Overworld state if not already
        if (state != State.overworld)
            EnterState(State.overworld);
    }

    bool CanClimb(out RaycastHit2D grabHit)
    {
        // fire ray forward from grab point for the current grabb distance based on grounding
        float rayDistance = grabDistance * (!grounded && state == State.overworld ? grabDistanceAirModifier : 1);
        Vector2 rayStartPoint = (Vector2)grabPoint.position - (faceDirection * colliderShell);
        grabHit = Physics2D.Raycast(rayStartPoint, faceDirection, rayDistance, layerMask);
        if (debugClimbing)
        {
            Debug.DrawRay(rayStartPoint, faceDirection * rayDistance, Color.green, Time.deltaTime);
        }

        // return true if climbable surface hit
        if (grabHit && IsClimbNormal(grabHit.normal))
        {
            // Debug.Log("hit " + grabHit.collider.name);
            // Debug.Log("hit at: (" + grabHit.point.x + ", " + grabHit.point.y + ")");
            // Debug.Log("surface norm: (" + grabHit.normal.x + ", " + grabHit.normal.y + ")");
            return true;
        }
        else
            return false;
    }

    bool IsBlink()
    {
        // If grounded and idle, blink on a randomized timer
        if (grounded && Mathf.Approximately(horizontalInput, 0))
        {
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= blinkTime)
            {
                // if not already in a double blink, set whether next blink is a double blink
                if (blinkTime != doubleBlinkTime && UnityEngine.Random.Range(0, doubleBlinkChance) == 0)
                    blinkTime = doubleBlinkTime;

                // if not a double blink, set time to next blink
                else
                    blinkTime = UnityEngine.Random.Range(minBlinkTime, maxBlinkTime);

                blinkTimer = 0;
                return true;
            }

            return false;
        }
        // if not idle, but next blink was set to be a double blink, reset to other blink interval instead
        else if (blinkTime == doubleBlinkTime)
            blinkTime = UnityEngine.Random.Range(minBlinkTime, maxBlinkTime);

        blinkTimer = 0;
        return false;
    }

    protected override void FixVTrap()
    {
        base.FixVTrap();

        // if in a V Trap, prevent horizontal input from being applied to avoid falling loops
        horizontalInputLock = true;
        horizontalInput = 0;
    }

    protected override void HitCeiling()
    {
        base.HitCeiling();

        // hitting the ceiling reduces velocity if still holding jump button or kills it if not holding jump button
        if (Input.GetButton("Jump"))
            velocity.y *= headBumpVelocityModifier;
        else
            velocity = Vector2.zero;
            
    }

    #endregion

    #region Climbing

        #region Climb Movement Handling
        Vector2 AdjustClimbMove(Vector2 move)
        {
            RaycastHit2D rayHit;
            
            rayHit = TestForHitsAlongClimbPath(move);
            if (HitWall(rayHit))
                return AdjustClimbToNewCocaveWall(rayHit);
            else if (HitCeiling(rayHit))
                return AdjustClimbForImpasse();

            rayHit = TestForCurrentWallAtMove(move);
            if (HitCurrentWall(rayHit))
                return AdjustClimbAlongPath(move);

            rayHit = TestForHitAroundWallCorner(move);
            if (HitWall(rayHit))
                return AdjustClimbToNewConvexWall(rayHit);
            else if (velocity.y > 0)
            {
                if (HitGroundOrNothing(rayHit))
                    return AdjustClimbForCrest(move);
                else if (HitCeiling(rayHit))
                    return AdjustClimbForImpasse();
            }
            
            return AdjustClimbForFall();
        }

        void ClimbMove(Vector2 move)
        {
            transform.SetPositionAndRotation(transform.position + (Vector3)move, transform.rotation);
        }

        void ReverseClimbMove(Vector2 move)
        {
            transform.SetPositionAndRotation(transform.position - (Vector3)move, transform.rotation);
            SetWallNormal(FindCurrentWallNormal());
            inputVelocity.y = 0; // set inputVelocity to 0 so a reversed movement does not accidentally ground player during rotation
            RotateToWallPoint(grabPoint.position);
        }

        bool IsHeadBump()
        {
            // Return whether head collider is currently colliding with a ceiling or opposing wall during climb

            if (inputVelocity.y > 0)
            {
                int contactNum = headCollider.GetContacts(contactFilter, headContactPoints);

                if (contactNum > 0)
                {
                    for (int i = 0; i < contactNum; i++)
                    {
                        if (headContactPoints[i].normal.y <= maxCeilingNormalY || Mathf.Sign(headContactPoints[i].normal.x) == faceDirection.x)
                        {
                            //Debug.DrawRay(headContactPoints[i].point, headContactPoints[i].normal, Color.green, Time.deltaTime);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool IsBaseBump()
        {
        // return whether base collider is currently colliding with either a floor or an opposing surface to the current wall
        bool baseBumped = false;

        if (inputVelocity.y < 0)
        {
            int contactNum = baseCollider.GetContacts(contactFilter, bodyContactPoints);

            if (contactNum > 0)
            {
                for (int i = 0; i < contactNum; i++)
                {
                    // Set grounded if colliding with ground
                    if (bodyContactPoints[i].normal.y >= minGroundNormalY)
                    {
                        grounded = true;
                        SetGroundNormal(bodyContactPoints[i].normal);
                        baseBumped = true;
                    }
                    else if (Mathf.Sign(bodyContactPoints[i].normal.x) == faceDirection.x)
                    {
                        baseBumped = true;
                    }
                }
            }
        }

        return baseBumped;
    }

        #region Climb Move Adjusment Methods

            #region Climb Adjustment Raycasts
            RaycastHit2D TestForHitsAlongClimbPath(Vector2 move)
            {
                // start test ray just outside of current wall
                Vector2 startPoint = (Vector2)grabPoint.position + (wallNormal * grabTestOffset);

                // fire ray along current projected move path
                RaycastHit2D rayHit = Physics2D.Raycast(startPoint, move.normalized, move.magnitude, layerMask);

                return rayHit;
            }

            RaycastHit2D TestForCurrentWallAtMove(Vector2 move)
            {
                // start test ray just outside of current wall and at end of current projected move
                Vector2 startPoint = (Vector2)grabPoint.position + (wallNormal * grabTestOffset) + move;

                // fire ray toward expected wall
                RaycastHit2D rayHit = Physics2D.Raycast(startPoint, -wallNormal, grabTestOffset * 2, layerMask);

                return rayHit;
            }

            RaycastHit2D TestForHitAroundWallCorner(Vector2 move)
            {
                // start test ray just outside of current wall and at end of current projected move
                Vector2 startPoint = (Vector2)grabPoint.position + (wallNormal * grabTestOffset) + move;

                // fire ray straight ahead in face direction for the grab distance
                RaycastHit2D rayHit = Physics2D.Raycast(startPoint, faceDirection, grabDistance, layerMask);

                return rayHit;
            }

            #endregion
            
            #region Climb Adjustment Bools
            bool HitWall(RaycastHit2D rayHit)
            {
                if (rayHit && IsClimbNormal(rayHit.normal))
                    return true;
                else
                    return false;
            }

            bool HitCurrentWall(RaycastHit2D rayHit)
            {
                if (rayHit && wallNormal == RoundedWallNormal(rayHit.normal))
                    return true;
                else
                    return false;
            }

            bool HitGroundOrNothing(RaycastHit2D rayHit)
            {
                if (!rayHit || rayHit.normal.y >= minGroundNormalY)
                    return true;
                else
                    return false;
            }

            bool HitCeiling(RaycastHit2D rayHit)
            {
                if (rayHit && rayHit.normal.y <= maxCeilingNormalY)
                    return true;
                else
                    return false;
            }
            #endregion

            #region Climb Move Adjustments
            Vector2 AdjustClimbToNewCocaveWall(RaycastHit2D rayHit)
            {
                // Update wall normal and rotation for new wall
                SetWallNormal(rayHit.normal);
                RotateToWallPoint(rayHit.point);

                // Adjust move to be vector from current grab point to new wall point
                return rayHit.point - (Vector2)grabPoint.position;
            }

            Vector2 AdjustClimbAlongPath(Vector2 move)
            {
                // Update rotation based on new grab point along move
                RotateToWallPoint((Vector2)grabPoint.position + move);

                // return move as projected
                return move;
            }

            Vector2 AdjustClimbToNewConvexWall(RaycastHit2D rayHit)
            {
                // Start Coroutine for moving around corner
                StopAllCoroutines();
                StartCoroutine(GrabWall(rayHit));

                // Zero out move to give control to coroutine
                return Vector2.zero;
            }

            Vector2 AdjustClimbForCrest(Vector2 move)
            {
                StopAllCoroutines();
                StartCoroutine(Crest(move));

                // Adjust movement to zero so crest can handle all movement
                return Vector2.zero;
            }

            Vector2 AdjustClimbForImpasse()
            {                
                // Adjust move to zero so gap stops movement
                return Vector2.zero;
            }

            Vector2 AdjustClimbForFall()
            {
                //TODO: Create Fall Transition, giving a visual/sound cue that allows the player to climb back up to avoid falling or actually fall
                EnterState(State.overworld);

                // adjust move to zero so overworld movement handling can take over
                return Vector2.zero;
            }
            #endregion

        #endregion

        #endregion

        #region Climb Rotation and Surface Normal Handling
        void RotateToWallPoint(Vector2 wallPoint)
        {
            // get rotation in degrees needed to move from current rotation to the rotation if latched on to the target point
            float rotationAngle = Vector2.SignedAngle(GetCurrentBodyUp(), GetClimbBodyUpAtPoint(wallPoint));

            // rotate to the new rotation around the grab point
            transform.RotateAround(grabPoint.position, Vector3.forward, rotationAngle);
        }

        void RotateToUprightAround(Vector2 rotationPoint)
        {
            // get rotation in degrees needed to move from current rotation to the rotation if latched on to the target point
            float rotationAngle = Vector2.SignedAngle(GetCurrentBodyUp(), Vector2.up);

            // rotate to the new rotation around the grab point
            transform.RotateAround(rotationPoint, Vector3.forward, rotationAngle);
        }

        Vector2 GetCurrentBodyUp()
        {
            Vector2 currentBodyUp = grabPoint.position - footPoint.position;
            return currentBodyUp.normalized;
        }

        Vector2 GetClimbBodyUpAtPoint(Vector2 topPoint)
        {
            //NOTE: MUST ALWAYS SET WALL NORMAL TO MATCH THIS POINT PRIOR TO RUNNING METHOD

            // start raycast from just outside of wall point being tested
            Vector2 point1 = topPoint + (wallNormal * grabTestOffset);
            
            // look for collisions along wall to where foot would be
            RaycastHit2D point2 = Physics2D.Raycast(point1, -climbUp, climbBodyLength, layerMask);
            //Debug.DrawRay(point1, -climbUp * climbBodyLength, Color.red, Time.deltaTime);

            if (point2)
            {
                //Debug.DrawRay(point2.point, point2.normal, Color.magenta, Time.deltaTime);
                int infiniteLoopCounter = 1
                ;
                // Continue testing along wall until a suitable foothold is found
                while (true)
                {
                    if (infiniteLoopCounter >= 30)
                    {
                        Debug.LogError("broke infinite loop in finding body up");
                        speedModifier = 1f; // default speed modifier to 1
                        return climbUp;
                    }
                    // if climbing downward and rotation would have you touching ground, set grounded and update ground normal
                    if (inputVelocity.y < 0 && point2.normal.y >= minGroundNormalY)
                    {
                        //Debug.Log("Hit ground due to rotation");
                        grounded = true;
                        SetGroundNormal(point2.normal);
                    }

                    
                    float grabToPointLength = Vector2.Distance(topPoint, point2.point);
                    Vector2 vectorUpSurface = new Vector2(point2.normal.y * faceDirection.x, Mathf.Abs(point2.normal.x));
                    float angleBetween = 180 - Vector2.Angle(climbUp, vectorUpSurface);

                    // if the grab to point length can already accomodate the climb body, stop here
                    if (grabToPointLength >= climbBodyLength)
                    {
                        Vector2 footPoint = point2.point;
                        Vector2 climbBodyUp = topPoint - footPoint;
                        speedModifier = (angleBetween / 180); // no change in angle (angleBetween = 180) should be no change in speed
                        return climbBodyUp.normalized;
                    }

                    // Use trigonometry to calculate how far to check along the new wall for the foothold
                    float distanceToCheck = GetThirdSideOfSSATriangle(angleBetween, climbBodyLength, grabToPointLength);
                    RaycastHit2D point3 = Physics2D.Raycast(point2.point + (point2.normal * grabTestOffset), -vectorUpSurface, distanceToCheck, layerMask);

                    // Debug.Log("grabToPointLength: " + grabToPointLength);
                    // Debug.Log("climbBodyLength: " + climbBodyLength);
                    // Debug.Log("vectorUpSurface: " + vectorUpSurface);
                    // Debug.Log("angleBetween:" + angleBetween);
                    // Debug.Log("distanceToCheck: " + distanceToCheck);
                    // Debug.DrawRay(point2.point + (point2.normal * grabTestOffset), -vectorUpSurface * distanceToCheck, Color.red, Time.deltaTime);

                    // if new surface is hit, update test point and continue loop
                    if (point3)
                    {
                        // Debug.Log("point3: (" + point3.point.x + ", " + point3.point.y + ")");
                        // Debug.Log("point3 normal: (" + point3.normal.x + ", " + point3.normal.y + ")");
                        point2 = point3;
                    }

                    // if no new surface hit, calculate body up from foot point to top point and adjust climb speed based on angle difference
                    else
                    {
                        Vector2 footPoint = point2.point - (vectorUpSurface * distanceToCheck);
                        Vector2 climbBodyUp = topPoint - footPoint;
                        speedModifier = (angleBetween / 180); // no change in angle (angleBetween = 180) should be no change in speed
                        return climbBodyUp.normalized;
                    }

                    infiniteLoopCounter++;
                    // Debug.Log("loop #: " + infiniteLoopCounter);
                }
            }

            // if nothing is hit, body up matches the up of the current wall
            else
            {
                // Debug.Log("no collision");
                speedModifier = 1f; // default speed modifier to 1
                return climbUp;
            }
        }

        float GetThirdSideOfSSATriangle(float angle, float oppositeSide, float adjacentSide)
        {
            // Trig calculation that returns the third side of an SSA triangle

            float angleB = Mathf.Deg2Rad * angle;
            float sideb = oppositeSide;
            float sidec = adjacentSide;

            float angleC = Mathf.Asin((sidec * Mathf.Sin(angleB))/sideb);
            float angleA = (Mathf.Deg2Rad * 180) - angleB - angleC;
            float sidea = (Mathf.Sin(angleA) * sideb) / Mathf.Sin(angleB);

            return sidea;
        }

        bool IsClimbNormal(Vector2 normal)
        {
            if (normal.y > maxCeilingNormalY && normal.y < minGroundNormalY)
                return true;
            else
                return false;
        }

        Vector2 FindCurrentWallNormal()
        {
            // start ray just outside of current wall (assuming you are on one)
            Vector2 startPoint = (Vector2)grabPoint.position - (faceDirection * grabTestOffset);

            // pass ray directly back through current wall to see what is hit
            RaycastHit2D wallHit = Physics2D.Raycast(startPoint, faceDirection, grabTestOffset * 2, layerMask);
            //Debug.DrawRay(startPoint, faceDirection * (grabTestOffset * 2), Color.magenta, Time.deltaTime);

            if (wallHit)
                return RoundedWallNormal(wallHit.normal);
            else
            {
                Debug.LogError("Could Not Find Current Wall");
                return -faceDirection; // return flat wall as a default
            }
        }

        void SetWallNormal(Vector2 newNormal)
        {
            wallNormal = RoundedWallNormal(newNormal);
            climbUp = new Vector2(newNormal.y * faceDirection.x, Mathf.Abs(newNormal.x));
        }

        Vector2 RoundedWallNormal(Vector2 exactWallNormal)
        {
            // if wall is nearly vertical, treat as vertical

            if (Mathf.Abs(exactWallNormal.y) < 0.001)
                return -faceDirection;
            else
                return exactWallNormal;
        }

        #endregion

        #region Climb Transition Coroutines
        IEnumerator GrabWall(RaycastHit2D grabHit)
        {
            // lock update to give full player control to Coroutine
            updateLocked = true;

            yield return new WaitForFixedUpdate();

            // set wall normal based on grab point
            SetWallNormal(grabHit.normal);

            // Get start and end rotation of transform Z in Euler angles
            float rotationChange = Vector2.SignedAngle(GetCurrentBodyUp(), GetClimbBodyUpAtPoint(grabHit.point));
            float startRotationZ = transform.rotation.eulerAngles.z;
            float endRotationZ = startRotationZ + rotationChange;

            // Get start and end position of the grab point
            Vector2 startPoint = grabPoint.position;
            Vector2 endPoint = grabHit.point;

            // initialize timer and scale the duration based off the size of the rotation and position changes
            float time = 0;
            float duration = Mathf.Max(Mathf.Abs(rotationChange) / 200, grabHit.distance / (speed * 1.5f));

            while(time < duration)
            {
                // Lerp the overall rotation to find the rotation difference required this frame, then rotate around the grab point
                float targetRotation = Mathf.LerpAngle(startRotationZ, endRotationZ, time/duration);
                float rotationDifference = targetRotation - transform.rotation.eulerAngles.z;
                transform.RotateAround(grabPoint.position, Vector3.forward, rotationDifference);

                // Lerp the grab point position to find the movement required this frame, then add that movement to the transform
                Vector2 targetPosition = Vector2.Lerp(startPoint, endPoint, time/duration);
                Vector2 positionDifference = targetPosition - (Vector2)grabPoint.position;
                transform.position += (Vector3)positionDifference;

                time += Time.deltaTime;
                yield return null;
            }

            // Get and apply final rotation change needed to reach final rotation
            float finalRotation = endRotationZ - transform.rotation.eulerAngles.z;
            transform.RotateAround(grabPoint.position, Vector3.forward, finalRotation);

            // Get and apply final move needed to reach final position
            Vector2 finalAdjust = endPoint - (Vector2)grabPoint.position;
            transform.position += (Vector3)finalAdjust;
            
            // Enter climb state and return control to update
            if (state != State.climb)
                EnterState(State.climb);
            updateLocked = false;
            yield return null;
        }

        IEnumerator GetOffWall()
        {
            updateLocked = true;
            Quaternion startingRotation = transform.rotation;

            // scale duration based on rotation change (time is zero if already upright)
            float duration = (1 - GetCurrentBodyUp().y) * dismountTimeMod;
            float time = 0;

            // Adjust position and rotation for duration
            while(time < duration)
            {
                transform.position += (Vector3)BackOutOfCurrentCollisions();
                transform.rotation = Quaternion.Lerp(startingRotation, Quaternion.identity, time/duration);

                time += Time.deltaTime;
                yield return new WaitForFixedUpdate();;
            }

            // Finalize Rotation and continue to adjust position
            transform.position += (Vector3)BackOutOfCurrentCollisions();
            transform.rotation = Quaternion.identity;
            yield return new WaitForFixedUpdate();

            int infiniteLoopCounter = 0;
            // Continue to back out of collisions until move required is negligible
            while(true)
            {
                if (infiniteLoopCounter >= 50)
                {
                    Debug.LogError("infinite loop broken in wall dismount");
                    break;
                }

                // Update Position
                Vector2 move = BackOutOfCurrentCollisions();
                if (move.magnitude < minMoveDistance)
                    break;
                transform.position += (Vector3)move;

                infiniteLoopCounter++;
                yield return new WaitForFixedUpdate();
            }

            EnterState(State.overworld);
            updateLocked = false;
            yield return null;
        }

        Vector2 BackOutOfCurrentCollisions()
        {
            // default move to zero (assume no movement needed)
            Vector2 move = Vector2.zero;

            // Get current contact hits on rigidbody
            int rbHits = rb2D.GetContacts(contactFilter, bodyContactPoints);

            // Get strongest single X and Y movement to clear current collisions upward and backward
            for (int i = 0; i < rbHits; i++)
            {
                Debug.DrawRay(bodyContactPoints[i].point, (bodyContactPoints[i].normal * bodyContactPoints[i].separation), Color.red, Time.deltaTime);

                // get the move required to get this point of contact out of the collider
                Vector2 moveOut = bodyContactPoints[i].normal * Mathf.Abs(bodyContactPoints[i].separation);

                // if horizontal movement to clear is backward move and is stronger than current move x, update move x
                if (Mathf.Sign(moveOut.x) != Mathf.Sign(faceDirection.x) && Mathf.Abs(moveOut.x) > Mathf.Abs(move.x))
                    move.x = moveOut.x;

                // if vertical movment to clear is stronger upward than current move y, update move y
                if (moveOut.y > move.y)
                    move.y = moveOut.y;
            }

            // clamp movement to player's movement speed
            if (move.magnitude > speed * Time.deltaTime)
                move = move.normalized * (speed * Time.deltaTime);

            // If no upward movement was required, adjust downward for gravity up to collision point
            if (move.y < minMoveDistance)
            {
                // reset horizontal push and set downward velocity
                horizontalPush = 0;
                velocity = Vector2.down;

                // adjust downward movement to stop at collision and update horizontal push (apply minimum push)
                Vector2 landingMove = AdjustForCollision(Vector2.down * (speed * Time.deltaTime), true);
                if (horizontalPush != 0)
                    horizontalPush = Mathf.Max(Mathf.Abs(horizontalPush) * Time.deltaTime, minMoveDistance * 2) * Mathf.Sign(horizontalPush);

                // Update y trajectory and add any push to x trajectory
                move.y = landingMove.y;
                move.x += horizontalPush;
            }

            return move;
        }

        IEnumerator Crest(Vector2 move)
        {
            //TODO: reverse Crest if collide along the way, or transition to grab if coming up along a wall

            cresting = true;
            crestingVal = 0; // first frame of crest animation

            // lock input to give control to Coroutine
            updateLocked = true;
            velocity = Vector2.zero;

            // cache hit for starting point in case move needs to be reversed
            RaycastHit2D startingWallHit = Physics2D.Raycast((Vector2)grabPoint.position - (faceDirection * grabTestOffset), faceDirection, grabTestOffset * 2, layerMask);
            
            // get corner point and initialize necessary variables
            Vector2 cornerPoint = GetCornerPoint(move, out bool isPeak);
            Vector2 startPoint, endPoint;
            float time, duration;

            // if on underhang, rotate to vertical first before ascending
            if(wallNormal.y < 0)
            {
                // Get start and end rotation of transform Z in Euler angles
                float rotationChange = Vector2.SignedAngle(GetCurrentBodyUp(), Vector2.up);
                float startRotationZ = transform.rotation.eulerAngles.z;
                float endRotationZ = startRotationZ + rotationChange;

                // Get start and end position of the grab point
                startPoint = grabPoint.position;
                endPoint = cornerPoint;

                // initialize timer and scale the duration based off the size of the rotation and position changes
                time = 0;
                duration = Mathf.Max(Mathf.Abs(rotationChange) / 200);

                while(time < duration)
                {
                    // Lerp the overall rotation to find the rotation difference required this frame, then rotate around the grab point
                    float targetRotation = Mathf.LerpAngle(startRotationZ, endRotationZ, time/duration);
                    float rotationDifference = targetRotation - transform.rotation.eulerAngles.z;
                    transform.RotateAround(grabPoint.position, Vector3.forward, rotationDifference);

                    // Lerp the grab point position to find the movement required this frame, then add that movement to the transform
                    Vector2 targetPosition = Vector2.Lerp(startPoint, endPoint, time/duration);
                    Vector2 positionDifference = targetPosition - (Vector2)grabPoint.position;
                    transform.position += (Vector3)positionDifference;

                    time += Time.deltaTime;
                    yield return new WaitForFixedUpdate();
                }

                // Get and apply final rotation change needed to reach final rotation
                float finalRotation = endRotationZ - transform.rotation.eulerAngles.z;
                transform.RotateAround(grabPoint.position, Vector3.forward, finalRotation);

                // Get and apply final move needed to reach final position
                Vector2 finalAdjust = endPoint - (Vector2)grabPoint.position;
                transform.position += (Vector3)finalAdjust;
            }

            playerAudio.PlayOneShotVocalFromList(playerAudio.crestVocals);

            // start at corner point, and set target end point where the full body would be off the current wall
            startPoint = cornerPoint;
            endPoint = startPoint + (Vector2.up * climbBodyLength);
            SetWallNormal(Vector2.right * -faceDirection); // TODO: FIX: Should't this be multiplied by "-faceDirection"?

            // set duration as time it takes to travel body length
            time = 0;
            duration = climbBodyLength / speed;

            while (time < duration)
            {
                if (time > duration/2)
                    crestingVal = 0.33f; // second frame of crest animation

                // Lerp Position and Set rotation based on grab point
                Vector2 targetPosition = Vector2.Lerp(startPoint, endPoint, time/duration);
                Vector2 positionChange = targetPosition - (Vector2)grabPoint.position;
                transform.position += (Vector3)positionChange;
                RotateToWallPoint(grabPoint.position);

                time += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Finalize position and rotation for top of ledge
            Vector2 finalPositionChange = endPoint - (Vector2)grabPoint.position;
            transform.position += (Vector3)finalPositionChange;
            transform.rotation = Quaternion.identity;
            yield return new WaitForFixedUpdate();

            crestingVal = 0.66f; // third frame of crest animation

            // Set Landing distance to as full body length (or half, if landing on a peak)
            float landingDistance = isPeak ? climbBodyWidth / 2 : climbBodyWidth;
            Vector2 landingVector = faceDirection;

            // if the ground slopes upward, adjust travel distance for slope
            if (groundNormal.x != 0 && Mathf.Sign(groundNormal.x) != Mathf.Sign(faceDirection.x))
            {
                landingDistance = climbBodyWidth * Mathf.Sqrt(groundNormal.x * groundNormal.x + groundNormal.y * groundNormal.y);
                landingVector = groundForward * faceDirection.x;
            }

            // if you would collide with something before fully on the ledge, shorten to collision
            if (CollisionDetected(landingVector, landingDistance))
                landingDistance = raycastHitArray[nearestHitIndex].distance - colliderShell;

            time = 0;
            duration = landingDistance / speed; // TODO: add min duration based on what looks good for the animation
            Vector3 startPosition = transform.position;
            Vector3 endPosition = transform.position + (Vector3)(landingVector * landingDistance);

            while (time < duration)
            {
                if (time > 0)
                    ApplyLandingMove();
                    
                if (time > duration/2)
                    crestingVal = 1f; // fourth frame of crest animation

                transform.position += (Vector3)(landingVector * (speed * Time.deltaTime));
                time += Time.deltaTime;
                
                yield return new WaitForFixedUpdate();
            }

            ApplyLandingMove();
            transform.position += (Vector3)(landingVector * (speed * Time.deltaTime));
            yield return new WaitForFixedUpdate();

            ApplyLandingMove();
            grounded = true;
            SetGroundNormal(raycastHitArray[nearestHitIndex].normal);
            yield return new WaitForFixedUpdate();

            cresting = false;
            crestingVal = 0;
            EnterState(State.overworld);
            updateLocked = false;
            yield return null;
        }

        void ApplyLandingMove()
        {
            Vector2 landingMove;

            if (CollisionDetected(Vector2.down, speed * Time.deltaTime))
                landingMove = Vector2.down * (raycastHitArray[nearestHitIndex].distance - colliderShell);
            else
                landingMove = Vector2.down * (speed * Time.deltaTime);

            transform.position += (Vector3)landingMove;
        }

        Vector2 GetCornerPoint(Vector2 move, out bool isPeak)
        {
            // only works if "move" moves Grab Point off a wall along it's climbUp
            Vector2 point1 = grabPoint.position;
            //Debug.DrawRay((Vector2)grabPoint.position, climbUp * move.magnitude, Color.yellow, Time.deltaTime);
            
            Vector2 rayStartPoint = point1 + move - (wallNormal * grabTestOffset);
            RaycastHit2D hitAroundCorner = Physics2D.Raycast(rayStartPoint, -climbUp, move.magnitude * 2, layerMask);

            if (!hitAroundCorner)
            {
                // if no hit, assume full move needed
                Debug.LogError("No corner point found for wall");
                isPeak = true;
                return point1 + move;
            }

            if (hitAroundCorner.normal.y >= minGroundNormalY)
            {
                isPeak = false;
                SetGroundNormal(hitAroundCorner.normal);
            }
            else
            {
                isPeak = true;
            }
            //Debug.DrawRay(rayStartPoint, -move * 2, Color.red, Time.deltaTime);

            Vector2 point2 = hitAroundCorner.point;
            Vector2 ray2Direction = new Vector2(hitAroundCorner.normal.y * -faceDirection.x, hitAroundCorner.normal.x * faceDirection.x);

            Vector2 cornerPoint = GetIntersectionOfTwoLines(point1, climbUp, point2, ray2Direction);
            //Debug.DrawLine(point2, cornerPoint, Color.magenta, Time.deltaTime);

            return cornerPoint;
        }

        Vector2 GetIntersectionOfTwoLines(Vector2 line1point, Vector2 line1direction, Vector2 line2point, Vector2 line2direction)
        {
            Vector2 A1 = line1point;
            Vector2 A2 = line1point + line1direction;
            Vector2 B1 = line2point;
            Vector2 B2 = line2point + line2direction;

            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
        
            if (tmp == 0)
            {
                // No solution!
                return Vector2.zero;
            }
        
            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
        
        
            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }
    #endregion

    #endregion

    #region Gliding

    void GlideMove()
    {
        // horizontal movement
        Vector2 move = Vector2.right * velocity.x * Time.deltaTime;
        move = AdjustForCollision(move, false);
        Move(move);

        // horizontal push
        if (horizontalPush != 0)
        {
            move = AdjustForCollision(Vector2.right * horizontalPush * Time.deltaTime, false);
            rb2D.position += move;
            horizontalPush = 0;
        }
        
        // vertical movement
        move = Vector2.up * velocity.y * Time.deltaTime;
        move = AdjustForCollision(move, true);
        Move(move);

        if (state == State.glide && grounded)
        {
            //Debug.Log("Caught Missed Landing");
            Land(raycastHitArray[nearestHitIndex].point);
        }
    }

    void GlideWallGrab(RaycastHit2D grabHit)
    {
        // get adjustment needed to be at front of glide  body rather than middle
        float adjust = Mathf.Abs(grabPoint.localPosition.y) - (climbBodyWidth * 0.5f);
        Vector2 adjustDirection = grabHit.point.x > transform.position.x ? Vector2.right : Vector2.left;

        // enter climb state for animation
        EnterState(State.climb);

        // rotate to upright and move
        transform.rotation = Quaternion.identity;
        transform.position += (Vector3)(adjustDirection * adjust);

        // update animator so no longer gliding
        animator.SetBool("gliding", state == State.glide);

        // set local positions of grab and foot point to defaults for Grab Wall calculations
        grabPoint.localPosition = grabPointLocalPosition;
        footPoint.localPosition = footPointLocalPosition;

        playerAudio.PlayOneShotSound(playerAudio.hitWall);

        StartCoroutine(GrabWall(grabHit));
    }

    protected override void Land(Vector2 collisionPoint)
    {
        base.Land(collisionPoint);

        if (state == State.glide)
        {
            EnterState(State.overworld);

            // if ground will not be under body when switching to upright, adjust body forward to where it will be
            float distanceFromLandingPoint = Mathf.Abs(collisionPoint.x - transform.position.x);
            if (distanceFromLandingPoint >= climbBodyWidth * 0.5f)
            {
                float adjust = Mathf.Abs(grabPoint.localPosition.y) - (climbBodyWidth * 0.5f);
                Vector2 adjustDirection = collisionPoint.x > transform.position.x ? Vector2.right : Vector2.left;
                Move(adjustDirection * adjust);
            }
        }

        playerAudio.PlayOneShotSound(playerAudio.land);
    }

    Vector2 GlideStartPosition()
    {
        // Get position of glide body needed where it is not colliding with anything in front
        
        float testDistance = spriteRenderer.bounds.extents.y - grabPointLocalPosition.x;
        Vector2 glidePosition = transform.position;
        if (CollisionDetected(faceDirection, testDistance))
            glidePosition -= (faceDirection * testDistance);

        return glidePosition;
    }

    IEnumerator SetStatePositionAndRotation(Vector2 newPosition, Quaternion newRotation)
    {
        // used for transitioning into the glide state where rotation and animations are in sync

        updateLocked = true;
        yield return new WaitForFixedUpdate();

        transform.position = newPosition;
        transform.rotation = newRotation;
        animator.SetBool("gliding", state == State.glide);
        updateLocked = false;
        yield return null;
    }

    IEnumerator SetStateRotation(Quaternion newRotation)
    {
        // used for transitioning into the overworld state where rotation and animations are in sync

        updateLocked = true;
        yield return new WaitForFixedUpdate();

        transform.rotation = newRotation;
        animator.SetBool("gliding", state == State.glide);
        updateLocked = false;
        yield return null;
    }

    #endregion

    #region Tunneling

    public void FlagAtEntrance(Direction direction)
    {
        atEntrance = true;

        if ((direction == Direction.left && horizontalInput < 0) || (direction == Direction.right && horizontalInput > 0))
        {
            // Debug.Log("Assessing Auto-Enter " + direction + " from flag");
            StartCoroutine(AssessAutoEnter(direction));
        }
    }

    IEnumerator AssessAutoEnter(Direction direction)
    {
        // Debug.Log("Assessing Auto Entry in Coroutine");
        assessingAutoEntry = true;
        float timer = 0;
        while (timer < autoEntryTime)
        {
            // check input is still down
            if (direction == Direction.right ? (horizontalInput > 0) : (horizontalInput < 0))
            {
                timer += Time.deltaTime;
                yield return null;
            }
            else
            {
                assessingAutoEntry = false;
                // Debug.Log("Breaking Auto Entry Coroutine");
                yield break;
            }
        }

        if (CanEnterTunnel(direction == Direction.right ? KeyCode.RightArrow : KeyCode.LeftArrow))
        {
            // Debug.Log("Finished Auto Entry Coroutine and Entering Tunnel");
            StartCoroutine(EnterTunnel(direction, GetEntryPoint()));
        }
    }

    public void FlagLeftEntrance()
    {
        atEntrance = false;
    }

    bool CanEnterTunnel(KeyCode key)
    {
        entryPoints.Clear();
        int contactNum = rb2D.Cast(Vector2.zero, raycastHitArray);

        for (int i = 0; i < contactNum; i++)
        {
            TunnelEntrances tunnelEntrances = raycastHitArray[i].collider.GetComponent<TunnelEntrances>();

            if (tunnelEntrances != null &&
                tunnelEntrances.CanEnterHereWithInput(key, raycastHitArray[i].point, out Vector2 entryPoint))
            {
                entryPoints.Add(entryPoint);
                // Debug.Log("Added Entry Point: " + entryPoint);
            }
        }

        if (entryPoints.Count > 0)
            return true;
        else
            return false;
    }

    Vector2 GetEntryPoint()
    {
        Vector2 entryPoint = entryPoints[0];

        if (entryPoints.Count > 1)
        {
            Vector2 centerPoint = spriteRenderer.bounds.center;

            for (int i = 1; i < entryPoints.Count; i++)
            {
                float distanceToCurrent = (entryPoint - centerPoint).magnitude;
                float distanceToThis = (entryPoints[i] - centerPoint).magnitude;

                entryPoint = distanceToThis < distanceToCurrent ? entryPoints[i] : entryPoint;
            }
        }

        return entryPoint;
    }

    IEnumerator EnterTunnel(Direction direction, Vector2 entryPoint)
    {
        updateLocked = true;
        assessingAutoEntry = false;

        switch (direction)
        {
            case Direction.down:
            {
                // Walk until centered above tunnel entrance
                horizontalInput = entryPoint.x >= transform.position.x ? 1f : -1f;
                inputVelocity.x = horizontalInput * speed * speedModifier;
                UpdateFaceDirection();
                UpdateAnimator();

                int infiniteLoopCounter = 0;
                while ((horizontalInput < 0 && transform.position.x > entryPoint.x) ||
                    (horizontalInput > 0 && transform.position.x < entryPoint.x))
                {
                    if (infiniteLoopCounter >= 30)
                    {
                        Debug.LogError("broke infinite loop in Tunnel Entry");
                        break;
                    }
                    base.FixedUpdate();
                    yield return new WaitForFixedUpdate();
                }
                transform.SetPositionAndRotation(new Vector3(entryPoint.x, transform.position.y, transform.position.z), transform.rotation);

                entering = true;
                EventBroker.TunnelEntryBegunCall();
                GameManager.instance.EnterTunnel();
                playerAudio.PlayOneShotSound(playerAudio.tunnelEnterExit);

                // Set variables for Lerping position into tunnel
                speed = tunnelEnterExitSpeed;
                Vector2 playerCenter = spriteRenderer.bounds.center;
                Vector2 destinationPoint = entryPoint + (Vector2.down * 0.5f);
                float time = 0;
                float duration = (destinationPoint - playerCenter).magnitude / speed;

                // Update Animator and relevant variables for Entry
                horizontalInput = 0f;
                verticalInput = -1f;
                inputVelocity = Vector2.down * speed * speedModifier;
                UpdateTunnelFaceDirection();
                UpdateAnimator();
                ChangeCameraYDampingTo(20);
                animator.SetTrigger("enter1");

                // Lerp into tunnel
                while (time < duration)
                {
                    transform.position = Vector2.Lerp(playerCenter, destinationPoint, time/duration);
                    if (vCamPlayer.transform.position.y > transform.position.y)
                        ChangeCameraYDampingTo(0.1f);
                    time+= Time.deltaTime;
                    yield return null;
                }
                transform.position = destinationPoint;

                break;
            }

            case Direction.up:
            {
                // Align position and rotation to entry
                speed = climbSpeed;
                Vector2 startPoint = spriteRenderer.bounds.center;
                Vector2 turnPoint = entryPoint + (Vector2.down * (climbBodyLength/2));
                Vector2 destinationPoint = entryPoint + (Vector2.up * 0.5f);
                Quaternion startRotation = transform.rotation;

                horizontalInput = 0;
                verticalInput = 1;
                inputVelocity = Vector2.up * speed * speedModifier;
                UpdateTunnelFaceDirection();
                UpdateAnimator();
                animator.SetTrigger("enter1");

                float time = 0;
                float duration = (turnPoint - startPoint).magnitude / speed;

                while (time < duration)
                {
                    float damping = Mathf.Lerp(2.0f, 1.5f, time/duration);
                    ChangeCameraYDampingTo(damping);
                    transform.position = Vector2.Lerp(startPoint, turnPoint, time/duration);
                    transform.rotation = Quaternion.Lerp(startRotation, Quaternion.identity, time/duration);
                    time += Time.deltaTime;
                    yield return null;
                }

                entering = true;
                EventBroker.TunnelEntryBegunCall();
                GameManager.instance.EnterTunnel();
                playerAudio.PlayOneShotSound(playerAudio.tunnelEnterExit);

                speed = tunnelEnterExitSpeed;
                time = 0;
                duration = (destinationPoint - startPoint).magnitude / speed;

                while (time < duration)
                {
                    float damping = Mathf.Lerp(1.5f, 0.1f, time/duration);
                    ChangeCameraYDampingTo(damping);
                    transform.position = Vector2.Lerp(turnPoint, destinationPoint, time/duration);
                    time += Time.deltaTime;
                    yield return null;
                }
                transform.position = destinationPoint;

                animator.SetTrigger("enter2");
                ChangeCameraYDampingTo(0.1f);

                break;
            }

            case Direction.left:
            {
                switch (state)
                {
                    case State.overworld:
                    {
                        // Walk until against wall
                        horizontalInput = -1;
                        inputVelocity.y = 0;
                        inputVelocity.x = horizontalInput * speed * speedModifier;
                        velocity = inputVelocity;
                        UpdateFaceDirection();
                        UpdateAnimator();

                        int infiniteLoopCounter = 0;
                        while (!AgainstEntryWall(entryPoint))
                        {
                            if (infiniteLoopCounter >= 100)
                            {
                                Debug.LogError("broke infinite loop in Tunnel Entry");
                                break;
                            }
                            base.FixedUpdate();
                            yield return new WaitForFixedUpdate();
                        }
                        break;
                    }

                    case State.climb:
                    {
                        // Climb until centered in entrance
                        verticalInput = spriteRenderer.bounds.center.y > entryPoint.y ? -1 : 1;
                        horizontalInput = 0;
                        inputVelocity.x = 0;
                        inputVelocity.y = verticalInput * speed * speedModifier;
                        UpdateAnimator();
                        if (verticalInput < 0)
                            ChangeCameraYDampingTo(1f);

                        int infiniteLoopCounter = 0;
                        while (spriteRenderer.bounds.center.y != entryPoint.y)
                        {
                            if (infiniteLoopCounter >= 100)
                            {
                                Debug.LogError("broke infinite loop in Tunnel Entry");
                                break;
                            }

                            FixedUpdateClimb();
                            if ((verticalInput < 0 && spriteRenderer.bounds.center.y < entryPoint.y) || (verticalInput > 0 && spriteRenderer.bounds.center.y > entryPoint.y))
                            {
                                float adjust = entryPoint.y - spriteRenderer.bounds.center.y;
                                transform.position += Vector3.up * adjust;
                            }
                            yield return new WaitForFixedUpdate();
                        }
                        break;
                    }
                }

                entering = true;
                EventBroker.TunnelEntryBegunCall();
                GameManager.instance.EnterTunnel();
                playerAudio.PlayOneShotSound(playerAudio.tunnelEnterExit);

                // Set variables for Lerping position into tunnel
                speed = tunnelEnterExitSpeed;
                Vector2 startPoint = spriteRenderer.bounds.center;
                Vector2 turnPoint = new Vector2(startPoint.x, entryPoint.y);
                Vector2 destinationPoint = entryPoint + (Vector2.left * 0.5f);
                float time = 0;
                float duration = (turnPoint - startPoint).magnitude / speed;

                // Update Animator and relevant variables for Entry
                verticalInput = 0;
                inputVelocity = Vector2.left * speed * speedModifier;
                UpdateAnimator();
                animator.SetTrigger("enter1");
                ChangeCameraYDampingTo(1f);

                // Lerp to tunnel center
                while (time < duration)
                {
                    transform.position = Vector2.Lerp(startPoint, turnPoint, time/duration);
                    time+= Time.deltaTime;
                    yield return null;
                }
                
                // Lerp inside tunnel
                time = 0;
                duration = (destinationPoint - turnPoint).magnitude / speed;
                while (time < duration)
                {
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Enter 1") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        UpdateTunnelFaceDirection();
                        animator.SetTrigger("enter2");
                    }
                        
                    transform.position = Vector2.Lerp(turnPoint , destinationPoint, time/duration);
                    time+= Time.deltaTime;
                    yield return null;
                }
                transform.position = destinationPoint;

                ChangeCameraYDampingTo(0.1f);
                break;

            }

            case Direction.right:
            {
                switch (state)
                {
                    case State.overworld:
                    {
                        // Walk until against wall
                        horizontalInput = 1;
                        inputVelocity.y = 0;
                        inputVelocity.x = horizontalInput * speed * speedModifier;
                        velocity = inputVelocity;
                        UpdateFaceDirection();
                        UpdateAnimator();

                        int infiniteLoopCounter = 0;
                        while (!AgainstEntryWall(entryPoint))
                        {
                            if (infiniteLoopCounter >= 100)
                            {
                                Debug.LogError("broke infinite loop in Tunnel Entry");
                                break;
                            }
                            base.FixedUpdate();
                            yield return new WaitForFixedUpdate();
                        }
                        break;
                    }

                    case State.climb:
                    {
                        // Climb until centered in entrance
                        verticalInput = spriteRenderer.bounds.center.y > entryPoint.y ? -1 : 1;
                        horizontalInput = 0;
                        inputVelocity.x = 0;
                        inputVelocity.y = verticalInput * speed * speedModifier;
                        UpdateAnimator();
                        if (verticalInput < 0)
                            ChangeCameraYDampingTo(1f);

                        int infiniteLoopCounter = 0;
                        while (spriteRenderer.bounds.center.y != entryPoint.y)
                        {
                            if (infiniteLoopCounter >= 100)
                            {
                                Debug.LogError("broke infinite loop in Tunnel Entry");
                                break;
                            }
                            FixedUpdateClimb();
                            if ((verticalInput < 0 && spriteRenderer.bounds.center.y < entryPoint.y) || (verticalInput > 0 && spriteRenderer.bounds.center.y > entryPoint.y))
                            {
                                float adjust = entryPoint.y - spriteRenderer.bounds.center.y;
                                transform.position += Vector3.up * adjust;
                            }
                            yield return new WaitForFixedUpdate();
                        }
                        break;
                    }
                }

                entering = true;
                EventBroker.TunnelEntryBegunCall();
                GameManager.instance.EnterTunnel();
                playerAudio.PlayOneShotSound(playerAudio.tunnelEnterExit);

                // Set variables for Lerping position into tunnel
                speed = tunnelEnterExitSpeed;
                Vector2 startPoint = spriteRenderer.bounds.center;
                Vector2 turnPoint = new Vector2(startPoint.x, entryPoint.y);
                Vector2 destinationPoint = entryPoint + (Vector2.right * 0.5f);
                float time = 0;
                float duration = (turnPoint - startPoint).magnitude / speed;

                // Update Animator and relevant variables for Entry
                verticalInput = 0;
                inputVelocity = Vector2.right * speed * speedModifier;
                UpdateAnimator();
                animator.SetTrigger("enter1");
                ChangeCameraYDampingTo(1f);

                // Lerp to tunnel center
                while (time < duration)
                {
                    transform.position = Vector2.Lerp(startPoint, turnPoint, time/duration);
                    time+= Time.deltaTime;
                    yield return null;
                }
                
                // Lerp inside tunnel
                time = 0;
                duration = (destinationPoint - turnPoint).magnitude / speed;
                while (time < duration)
                {
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Enter 1") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        UpdateTunnelFaceDirection();
                        animator.SetTrigger("enter2");
                    }
                        
                    transform.position = Vector2.Lerp(turnPoint , destinationPoint, time/duration);
                    time+= Time.deltaTime;
                    yield return null;
                }
                transform.position = destinationPoint;

                ChangeCameraYDampingTo(0.1f);
                break;
            }

            default:
                break;
        }
        
        EnterState(State.tunnel);
        EventBroker.TunnelEntryCompleteCall();
        entering = false;
        inputVelocity = Vector2.zero;
        updateLocked = false;

        yield return null;
    }

    bool AgainstEntryWall(Vector2 entryPoint)
    {
        float distance = Mathf.Abs(grabPoint.position.x - entryPoint.x);

        //Debug.Log("entry Point: " + entryPoint.x + ", " + entryPoint.y);

        return distance <= (colliderShell * 2);
    }

    void RestrictTunnelInput()
    {
        // set layer masks for tunnels and entrances
        int tunnelLayerMask = LayerMask.GetMask("Tunnels");
        int entranceLayerMask = LayerMask.GetMask("Tunnel Entrances");

        // default exit moves to false
        exitMoveInitiated = false;
        bool horizontalExit = false;
        bool verticalExit = false;

        // check if able to move/exit in direction of horizontal input, log if an exit move
        if (horizontalInput != 0)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * horizontalInput, 1, tunnelLayerMask);
            RaycastHit2D entranceHit = Physics2D.Raycast(transform.position, Vector2.right * horizontalInput, 1, entranceLayerMask);

            // stop input if moving into a wall but not towards an exit
            if (wallHit && !entranceHit)
                horizontalInput = 0;

            // flag as exit move if entrance ahead
            else if (entranceHit)
                horizontalExit = true;
        }

        // check if able to move/exit in direction of vertical input, log if an exit move
        if (verticalInput != 0)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.up * verticalInput, 1, tunnelLayerMask);
            RaycastHit2D entranceHit = Physics2D.Raycast(transform.position, Vector2.up * verticalInput, 1, entranceLayerMask);

            // stop input if moving into a wall but not towards an exit
            if (wallHit && !entranceHit)
                verticalInput = 0;

            // flag as exit move if entrance ahead
            else if (entranceHit)
                horizontalExit = true;
        }

        // If multiple inputs permissible, restrict to prioritized direction
        if (horizontalInput != 0 && verticalInput != 0)
        {
            if (prioritizeVertical)
            {
                horizontalInput = 0;
                horizontalExit = false;
            }
            else
            {
                verticalInput = 0;
                verticalExit = false;
            }
        }

        if (horizontalExit || verticalExit)
            exitMoveInitiated = true;
    }

    IEnumerator TunnelMove()
    {
        updateLocked = true;

        playerAudio.PlayOneShotSound(playerAudio.tunnelMove);

        Vector3 destination = transform.position + (Vector3)faceDirection;

        int infiniteLoopCounter = 0;
        while (transform.position != destination)
        {
            if (infiniteLoopCounter >= 30)
            {
                transform.position = destination;
                Debug.LogError("broke infinite loop in Tunnel Move");
                break;
            }

            UpdateMovePriority();

            Vector2 tunnelVelocity = faceDirection * speed;
            Vector2 move = tunnelVelocity * Time.deltaTime;
            Vector2 finalMove = destination - transform.position;

            move = move.magnitude < finalMove.magnitude ? move : finalMove;
            rb2D.position += move;

            infiniteLoopCounter++;
            yield return new WaitForFixedUpdate();
        }

        UpdateMovePriority();
        inputVelocity = Vector2.zero;
        updateLocked = false;
    }

    void UpdateMovePriority()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            prioritizeVertical = true;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            prioritizeVertical = false;
    }

    IEnumerator ExitTunnel()
    {
        updateLocked = true;
        exiting = true;
        bool climbOnExit = false;
        bool landOnExit = false;

        EventBroker.TunnelExitBegunCall();
        GameManager.instance.ExitTunnel();
        playerAudio.PlayOneShotSound(playerAudio.tunnelEnterExit);

        //Debug.Log("Tunnel Exit Started");

        if (faceDirection == Vector2.up)
        {
            speed = tunnelEnterExitSpeed;
            Vector2 startPoint = transform.position;
            Vector2 destinationBasePoint = startPoint + (Vector2.up * 0.5f);
            Vector2 destinationPoint = destinationBasePoint + (Vector2.up * spriteRenderer.bounds.extents.y);
            
            animator.SetTrigger("exit1");
            ChangeCameraYDampingTo(2f);

            float timer = 0;
            float duration = (destinationPoint - startPoint).magnitude / speed;

            while(timer < duration)
            {
                transform.position = Vector2.Lerp(startPoint, destinationPoint, timer/duration);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.position = destinationBasePoint;
            EnterState(State.overworld);
            grounded = true;
            inputVelocity = Vector2.zero;
            velocity = Vector2.zero;
            faceDirection = Vector2.right;
            UpdateAnimator();
            animator.SetTrigger("exit2");
            ChangeCameraYDampingTo(0.1f);
        }

        else if (faceDirection == Vector2.down)
        {
            speed = tunnelEnterExitSpeed;
            Vector2 startPoint = transform.position;
            Vector2 destinationBasePoint = startPoint + (Vector2.down * (0.5f + spriteRenderer.bounds.size.y));
            Vector2 destinationPoint = destinationBasePoint + (Vector2.up * spriteRenderer.bounds.extents.y);
            
            animator.SetTrigger("exit1");

            float timer = 0;
            float duration = (destinationPoint - startPoint).magnitude / speed;

            while(timer < duration)
            {
                float damping = Mathf.Lerp(2.0f, 0.1f, timer/duration);
                    ChangeCameraYDampingTo(damping);
                transform.position = Vector2.Lerp(startPoint, destinationPoint, timer/duration);
                timer += Time.deltaTime;
                yield return null;
            }
            ChangeCameraYDampingTo(0.1f);
            transform.position = destinationBasePoint;

            EnterState(State.overworld);
            grounded = false;
            verticalInput = 0;
            horizontalInput = 0;
            inputVelocity = Vector2.zero;
            velocity = Vector2.zero;
            faceDirection = Vector2.right;

            //ADD BACK IN TO AUTO-GRAB WALL UPON DOWN EXIT
            // if (form >= Form.claws)
            // {
            //     RaycastHit2D testHit;
            //     faceDirection = Vector2.right;
            //     if (CanClimb(out testHit))
            //         climbOnExit = true;
            //     else
            //     {
            //         faceDirection = Vector2.left;
            //         if (CanClimb(out testHit))
            //             climbOnExit = true;
            //         else
            //             faceDirection = Vector2.right;
            //     }
            // }

            UpdateAnimator();
            animator.SetTrigger("exit2");
            yield return new WaitForFixedUpdate();
        }

        else
        {
            speed = tunnelEnterExitSpeed;
            Vector2 startPoint = transform.position;
            Vector2 turnPoint = startPoint + faceDirection * (0.5f + spriteRenderer.bounds.extents.y);
            Vector2 destinationBasePoint = turnPoint + (Vector2.down * 0.5f);
            Vector2 destinationPoint = destinationBasePoint + (Vector2.up * spriteRenderer.bounds.extents.x);

            if (ExitingToGround(destinationBasePoint))
                landOnExit = true;
            else if (form >= Form.claws)
                climbOnExit = true;

            animator.SetTrigger("exit1");            

            float timer = 0;
            float duration = (turnPoint - startPoint).magnitude / speed;
            float yOffsetGoal = -spriteRenderer.bounds.extents.x;
            while (timer < duration)
            {
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("ExitRL-1") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                {
                    transform.rotation = Quaternion.identity;
                    horizontalInput = climbOnExit ? -horizontalInput : horizontalInput;
                    UpdateFaceDirection();
                    animator.SetTrigger("exit2");
                }
                float yOffset = Mathf.Lerp(0, yOffsetGoal, timer/duration);
                SetCameraYOffset(yOffset);
                transform.position = Vector2.Lerp(startPoint, turnPoint, timer/duration);
                timer += Time.deltaTime;
                yield return null;
            }

            SetCameraYOffset(yOffsetGoal);
            timer = 0;
            duration = (destinationPoint - turnPoint).magnitude / speed;
            while (timer < duration)
            {
                transform.position = Vector2.Lerp(turnPoint, destinationPoint, timer/duration);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.position = destinationBasePoint;

            if (climbOnExit)
            {
                EnterState(State.climb);
            }
            else
            {
                EnterState(State.overworld);
                grounded = landOnExit ? true : false;
            }

            inputVelocity = Vector2.zero;
            velocity = Vector2.zero;
            UpdateAnimator();
            animator.SetTrigger("exit2");
            ChangeCameraYDampingTo(0);
            SetCameraYOffset(0);
            
            yield return new WaitForFixedUpdate();

            ChangeCameraYDampingTo(0.1f);
        }
        
        reentryAvailable = true;
        EventBroker.TunnelExitCompleteCall();
        exiting = false;
        updateLocked = false;

        if (climbOnExit && CanClimb(out RaycastHit2D grabHit))
            StartCoroutine(GrabWall(grabHit));

        yield return null;
    }

    bool ExitingToGround(Vector2 destinationBasePoint)
    {
        Vector2 startPoint = destinationBasePoint - (faceDirection * (climbBodyWidth * 0.5f)) + Vector2.up * colliderShell;
        RaycastHit2D groundHit = Physics2D.Raycast(startPoint, Vector2.down, colliderShell + grabTestOffset, layerMask);
        Debug.DrawRay(startPoint, Vector2.down * (colliderShell + grabTestOffset), Color.red, Time.deltaTime);

        if (groundHit && groundHit.normal.y >= minGroundNormalY)
            return true;
        else
            return false;
    }

    void ChangeCameraYDampingTo(float yDamping)
    {
        vCamPlayer.GetCinemachineComponent<CinemachineTransposer>().m_YDamping = yDamping;
    }

    void SetCameraYOffset(float yOffset)
    {
        vCamPlayer.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset.y = yOffset;
    }

    #endregion
}