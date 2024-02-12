using System;
using System.Collections;
using LobbyRelaySample;
using LobbyRelaySample.ngo;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public float movementSpeed = 5.0f;
    private CharacterController characterController;

    public Camera playerCamera;

    private ClientNetworkTransform clientNetworkTransform;

    private Transform[] spawnPoints;
    
    private AnimationStateController m_AnimationStateController;

    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    public NetworkVariable<FixedString64Bytes> displayName =
        new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes(""));

    [Header("Movement")] public NetworkVariable<float> walkingSpeed = new NetworkVariable<float>(7.5f);
    public NetworkVariable<float> runningSpeed = new NetworkVariable<float>(11.5f);
    public NetworkVariable<float> jumpSpeed = new NetworkVariable<float>(8.0f);
    public NetworkVariable<float> gravity = new NetworkVariable<float>(20.0f);
    public NetworkVariable<float> lookSpeed = new NetworkVariable<float>(2.0f);
    public NetworkVariable<float> lookXLimit = new NetworkVariable<float>(45.0f);

    [HideInInspector] public bool canMove = true;
    private bool canControl = true;

    [Header("Sprint System")] public float maxStamina;
    public NetworkVariable<float> sprintDrainRate = new NetworkVariable<float>();
    public NetworkVariable<float> sprintRefillRate = new NetworkVariable<float>();
    public NetworkVariable<float> staminaPenaltyRate = new NetworkVariable<float>();
    public NetworkVariable<float> staminaRegenerationRate = new NetworkVariable<float>();
    private float currentSprintTime; // Current available sprint time
    private float currentStamina; // Current total stamina capacity

    [Header("Sprint System")] public GameObject UIArea;
    public Slider sprintTimeSlider;
    public Slider staminaSlider;

    [Header("Player Roles")] public Color catcherColor;
    public Color runnerColor;
    public Color hostageColor;

    public enum PlayerRole
    {
        None,
        Catcher,
        Runner,
        Hostage
    }

    public NetworkVariable<PlayerRole> playerRole = new NetworkVariable<PlayerRole>(PlayerRole.None);

    private Transform hostageAreaTransform; // Assign this in the Unity Editor

    public delegate void PlayerRoleChangedDelegate(Player player, PlayerRole newRole);

    public event PlayerRoleChangedDelegate OnPlayerRoleChanged;

    private GameObject sessionMonitor;

    [SerializeField] private GameObject MainCamera;

    public override void OnNetworkSpawn()
    {
        playerRole.OnValueChanged += OnRoleChanged;

        characterController = GetComponent<CharacterController>();
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        m_AnimationStateController = GetComponent<AnimationStateController>();
        hostageAreaTransform = GameObject.FindGameObjectWithTag("HostageArea").transform;

        // Ensure the camera is only enabled for the local player
        if (IsLocalPlayer)
        {
            SetDisplayNameServerRpc(
                new FixedString64Bytes(GameManager.Instance.m_LocalUser.DisplayName.Value.ToString()));

            //get gameobject tagged as mainCamera and enable it
            MainCamera.SetActive(true);

            UIArea.SetActive(true);

            PopulateSpawnPoints();

            int spawnIndex = (int)(OwnerClientId % (ulong)spawnPoints.Length);
            //SpawnPlayerInSpawnPointServerRpc(0);
            characterController.enabled = false;
            clientNetworkTransform.transform.position = spawnPoints[spawnIndex].position;

            characterController.enabled = true;

            //disable the character controller for the local player

            //cameraTransform = Camera.main.transform;
            playerCamera = GetComponentInChildren<Camera>();
            Camera.main.transform.SetParent(transform);
            //Camera.main.transform.localPosition = new Vector3(0, 1, 0); // Adjust camera position relative to the player

            TogglePlayerControl(true);

            // Initialize sprint and stamina variables
            currentSprintTime = maxStamina;
            currentStamina = maxStamina;

            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;

            sprintTimeSlider.maxValue = maxStamina;
            sprintTimeSlider.value = currentSprintTime;
        }

        //for every player that spawns, add them to the session monitor

        //its important to register the player before AddSpawnedPlayer is called because every player will call this method and after that first role pull will be called
        //if it is not registered, it will not be able to handle the role change

        sessionMonitor = GameObject.FindGameObjectWithTag("SessionMonitor");
        sessionMonitor.GetComponent<SessionMonitor>().RegisterPlayer(this);

        GameObject loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");
        loadingScreen.GetComponent<LoadingScreen>().AddSpawnedPlayer(this);
    }

    public void AllPlayersSpawned()
    {
        if (!IsLocalPlayer) return;
        RequestRoleAssignmentServerRpc(GameManager.Instance.m_LocalUser.ID.Value);
        Debug.Log("All players have spawned");
    }

    [ServerRpc]
    void RequestRoleAssignmentServerRpc(string localUserID)
    {
        //wait for 5 seconds before assigning roles
        foreach (var player in GameManager.Instance.playerRoleAssignments)
        {
            if (player.Key == localUserID)
            {
                playerRole.Value = (PlayerRole)player.Value;
            }
        }
    }

    [ServerRpc]
    public void SetDisplayNameServerRpc(FixedString64Bytes newName)
    {
        // Optional: Add validation logic here
        displayName.Value = newName;
    }

    private void OnDestroy()
    {
        playerRole.OnValueChanged -= OnRoleChanged;
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        // Perform the color change and other role-specific logic
        Color roleColor = GetColorForRole(newRole);
        //GetComponent<Renderer>().material.color = roleColor;

        // Invoke the event to notify listeners of the role change
        OnPlayerRoleChanged?.Invoke(this, newRole);

        if (newRole == PlayerRole.Hostage)
        {
            gameObject.layer = LayerMask.NameToLayer("Hostage");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("NonHostage");
        }
    }

    private Color GetColorForRole(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Catcher:
                return catcherColor;
            case PlayerRole.Runner:
                return runnerColor;
            case PlayerRole.Hostage:
                return hostageColor;
            default:
                return Color.white; // Default color if role isn't set or is None
        }
    }

    public void RequestRoleChange(PlayerRole newRole)
    {
        if (IsOwner)
        {
            SetRoleServerRpc(newRole);
        }
    }

    [ServerRpc]
    private void SetRoleServerRpc(PlayerRole newRole)
    {
        playerRole.Value = newRole;
    }

    private void PopulateSpawnPoints()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        if (spawnPointObjects.Length == 0)
        {
            Debug.LogError("No game objects found with tag 'SpawnPoint'!");
            return;
        }

        spawnPoints = new Transform[spawnPointObjects.Length];
        for (int i = 0; i < spawnPointObjects.Length; i++)
        {
            spawnPoints[i] = spawnPointObjects[i].transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        //MovePlayer();

        // Handling cursor locking and visibility toggle
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePlayerControl(false);

        // Handling relocking the cursor with a mouse click
        if (!canControl && Input.GetMouseButtonDown(0) &&
            !EventSystem.current.IsPointerOverGameObject()) // Mouse click to relock
            TogglePlayerControl(true);

        // Always Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2) (its out of cancontrol, cause it should always be applied)
        if (!characterController.isGrounded) moveDirection.y -= gravity.Value * Time.deltaTime;

        var isTryingToSprint = Input.GetKey(KeyCode.LeftShift);
        var canSprint = isTryingToSprint && currentSprintTime > 0 && currentStamina > 0;

        if (canControl)
        {
            // We are grounded, so recalculate move direction based on axes
            var forward = transform.TransformDirection(Vector3.forward);
            var right = transform.TransformDirection(Vector3.right);
            // Press Left Shift to run
            // Determine if the player is trying to sprint and has the resources to do so

            // Determine the target speed based on whether the player is sprinting or walking
            var targetSpeed = canSprint ? runningSpeed.Value : walkingSpeed.Value;

            // Apply the target speed to movement calculations
            var curSpeedX = canMove ? targetSpeed * Input.GetAxis("Vertical") : 0;
            var curSpeedY = canMove ? targetSpeed * Input.GetAxis("Horizontal") : 0;
            
            Debug.Log("curSpeedX: " + curSpeedX);
            Debug.Log("curSpeedY: " + curSpeedY);
            
            m_AnimationStateController.animator.SetFloat("Velocity X", curSpeedX);
            m_AnimationStateController.animator.SetFloat("Velocity Y", curSpeedY);
            /*
            // Set the animation state based on the player's movement
                if (curSpeedX > 0 || curSpeedY > 0)
                {
                    m_AnimationStateController.animator.SetBool("isWalking", true);
                }
                else
                {
                    m_AnimationStateController.animator.SetBool("isWalking", false);
                }
                */
            

            var movementDirectionY = moveDirection.y;
            moveDirection = forward * curSpeedX + right * curSpeedY;

            if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
                moveDirection.y = jumpSpeed.Value;
            else
                moveDirection.y = movementDirectionY;

            // Move the controller
            characterController.Move(moveDirection * Time.deltaTime);

            // Player and Camera rotation
            if (canMove)
            {
                rotationX += -Input.GetAxis("Mouse Y") * lookSpeed.Value;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit.Value, lookXLimit.Value);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed.Value, 0);
            }
 
            // Listen for role assignment
            if (Input.GetKeyDown(KeyCode.Alpha1))
                RequestRoleChange(PlayerRole.Catcher);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                RequestRoleChange(PlayerRole.Runner);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) RequestRoleChange(PlayerRole.Hostage);

            // Sprinting Input
            if (canSprint)
            {
                /// Drain sprint time and apply continuous stamina penalty
                currentSprintTime -= Time.deltaTime * sprintDrainRate.Value;
                currentStamina -= Time.deltaTime * staminaPenaltyRate.Value;

                // Ensure sprint time and stamina don't go below zero
                currentSprintTime = Mathf.Max(currentSprintTime, 0);
                currentStamina = Mathf.Max(currentStamina, 0);
            }
        }

        if (!canSprint)
        {
            // Refill sprint time and stamina when not sprinting
            if (!isTryingToSprint && currentSprintTime < currentStamina)
                currentSprintTime += Time.deltaTime * sprintRefillRate.Value;

            if (currentStamina < maxStamina) currentStamina += Time.deltaTime * staminaRegenerationRate.Value;
        }

        // Cap sprint time and stamina at their max values
        currentSprintTime = Mathf.Min(currentSprintTime, currentStamina);
        currentStamina = Mathf.Min(currentStamina, maxStamina);

        sprintTimeSlider.value = currentSprintTime;
        staminaSlider.value = currentStamina;
    }

    private void TogglePlayerControl(bool shouldEnable)
    {
        if (shouldEnable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canControl = true; // Enable player control
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canControl = false; // Disable player control
        }
    }

    // This method is called when a player collides with another player.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only the local player can tag other players.
        if (!IsOwner) return;

        var otherPlayer = hit.gameObject.GetComponent<Player>();
        if (otherPlayer != null)
        {
            // Determine roles and perform tagging if conditions are met.
            if (playerRole.Value == PlayerRole.Catcher && otherPlayer.playerRole.Value == PlayerRole.Runner)
            {
                TagPlayerServerRpc(otherPlayer.OwnerClientId);
            }
            else if (playerRole.Value == PlayerRole.Runner && otherPlayer.playerRole.Value == PlayerRole.Catcher)
            {
                // Handle being tagged by a catcher.
                TagPlayerServerRpc(OwnerClientId);
            }
            else if ((playerRole.Value == PlayerRole.Runner && otherPlayer.playerRole.Value == PlayerRole.Hostage) ||
                     (playerRole.Value == PlayerRole.Hostage && otherPlayer.playerRole.Value == PlayerRole.Runner))
            {
                // Free the hostage.
                FreeHostageServerRpc(otherPlayer.OwnerClientId);
            }
        }
    }

// ServerRpc to tag a player.
    [ServerRpc]
    void TagPlayerServerRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var networkClient))
        {
            var player = networkClient.PlayerObject.GetComponent<Player>();
            if (player != null && player.playerRole.Value == PlayerRole.Runner)
            {
                player.playerRole.Value = PlayerRole.Hostage;

                player.TeleportToHostageAreaClientRpc();
            }
        }
    } 
    
    
    [ClientRpc]
    public void TeleportToHostageAreaClientRpc()
    {
        characterController.enabled = false;
        transform.position = hostageAreaTransform.position;
        characterController.enabled = true;
    }

// ServerRpc to free a hostage.
    [ServerRpc]
    void FreeHostageServerRpc(ulong hostageId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(hostageId, out var networkClient))
        {
            var hostage = networkClient.PlayerObject.GetComponent<Player>();
            if (hostage != null && hostage.playerRole.Value == PlayerRole.Hostage)
            {
                hostage.playerRole.Value = PlayerRole.Runner;
                // Additional logic for freeing the hostage.
            }
        }
    }

    
}

/*
//using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

//Below is the code for the player movement and camera movement
/*
[RequireComponent(typeof(CharacterController))]
*/
/*
public class Player : NetworkBehaviour
{
    
    /// <summary>
    /// The Sessions ID for the current server.
    /// </summary>
    [SyncVar]
    public string sessionId = "";

    /// <summary>
    /// Player name.
    /// </summary>
    public string username;

    public string ip;

    /// <summary>
    /// Platform the user is on.
    /// </summary>
    public string platform;

    /// <summary>
    /// Shifts the players position in space based on the inputs received.
    /// </summary>
    ///
    ///
    public static List<Player> AllPlayers = new List<Player>();

   

    [Header("Movement")]
    [SyncVar(hook = nameof(OnWalkingSpeedChanged))]
    public float walkingSpeed = 7.5f;
    [SyncVar(hook = nameof(OnRunningSpeedChanged))]
    public float runningSpeed = 11.5f;
    [SyncVar(hook = nameof(OnJumpSpeedChanged))]
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public AudioListener audioListener;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;
    
    [HideInInspector]
    public bool canMove = true;
    private bool canControl = true;
    
    
    
    
    [Header("Player Role and Color")]
    public Color catcherColor;
    public Color runnerColor;
    public Color hostageColor;
    public enum PlayerRole { None, Catcher, Runner, Hostage }
    [SyncVar(hook = nameof(OnRoleChanged))]
    public PlayerRole playerRole = PlayerRole.None;
    
    
    [Header("Hostage System")]
    private Transform hostageAreaTransform; // Assign this in the Unity Editor

    [Header("Sprint System")]
    // Sprint and Stamina variables
    //public float maxSprintTime = 5f; // Maximum time player can sprint
    [SyncVar(hook = nameof(OnMaxStaminaChanged))]
    public float maxStamina;
    private float currentSprintTime; // Current available sprint time
    private float currentStamina; // Current total stamina capacity   
    
    // Drain and Regeneration Rates
    [SyncVar(hook = nameof(OnSprintDrainRateChanged))]
    public float sprintDrainRate;
    [SyncVar(hook = nameof(OnSprintRefillRateChanged))]
    public float sprintRefillRate;
    [SyncVar(hook = nameof(OnStaminaPenaltyRateChanged))]
    public float staminaPenaltyRate;
    [SyncVar(hook = nameof(OnStaminaRegenerationRateChanged))]
    public float staminaRegenerationRate;
    
    public Slider sprintTimeSlider;
    public Slider staminaSlider;



    // SyncVar hooks for updating variables on clients
    private void OnWalkingSpeedChanged(float oldSpeed, float newSpeed)
    {
        walkingSpeed = newSpeed;
    }
    
    private void OnRunningSpeedChanged(float oldSpeed, float newSpeed)
    {
        runningSpeed = newSpeed;
    }
    
    private void OnJumpSpeedChanged(float oldSpeed, float newSpeed)
    {
        jumpSpeed = newSpeed;
    }
    
    private void OnMaxStaminaChanged(float oldMaxStamina, float newMaxStamina)
    {
        maxStamina = newMaxStamina;
    }
    
    private void OnSprintDrainRateChanged(float oldDrainRate, float newDrainRate)
    {
        sprintDrainRate = newDrainRate;
    }
    
    private void OnSprintRefillRateChanged(float oldRefillRate, float newRefillRate)
    {
        sprintRefillRate = newRefillRate;
    }
    
    private void OnStaminaPenaltyRateChanged(float oldPenaltyRate, float newPenaltyRate)
    {
        staminaPenaltyRate = newPenaltyRate;
    }
    
    private void OnStaminaRegenerationRateChanged(float oldRegenerationRate, float newRegenerationRate)
    {
        staminaRegenerationRate = newRegenerationRate;
    }

    [Command]
    public void CmdAssignWalkingSpeed(float newSpeed)
    {
        if (isServer)
        {
            walkingSpeed = newSpeed;
            UpdateWalkingSpeedForAll();
        }
    }
    
    [Server]
    public void UpdateWalkingSpeedForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.walkingSpeed = walkingSpeed;
        }
    }
    
    [Command]
    public void CmdAssignRunningSpeed(float newSpeed)
    {
        if (isServer)
        {
            runningSpeed = newSpeed;
            UpdateRunningSpeedForAll();
        }
    }
    
    [Server]
    public void UpdateRunningSpeedForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.runningSpeed = runningSpeed;
        }
    }
    
    [Command]
    public void CmdAssignJumpSpeed(float newSpeed)
    {
        if (isServer)
        {
            Debug.Log("Jump speed changed to " + newSpeed);
            jumpSpeed = newSpeed;
            UpdateJumpingSpeedForAll();
        }
        
    }
    
    [Server]
    public void UpdateJumpingSpeedForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.jumpSpeed = jumpSpeed;
        }
    }
    
    [Command]
    public void CmdAssignMaxStamina(float newMaxStamina)
    {
        if (isServer)
        {
            maxStamina = newMaxStamina;
            UpdateMaxStaminaForAll();
        }
    }
    
    [Server]
    public void UpdateMaxStaminaForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.maxStamina = maxStamina;
        }
    }
    
    [Command]
    public void CmdAssignSprintDrainRate(float newDrainRate)
    {
        if (isServer)
        {
            sprintDrainRate = newDrainRate;
            UpdateSprintDrainRateForAll();
        }
    }
    
    [Server]
    public void UpdateSprintDrainRateForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.sprintDrainRate = sprintDrainRate;
        }
    }
    
    [Command]
    public void CmdAssignSprintRefillRate(float newRefillRate)
    {
        if (isServer)
        {
            sprintRefillRate = newRefillRate;
            UpdateSprintRefillRateForAll();
        }
    }
    
    [Server]
    public void UpdateSprintRefillRateForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.sprintRefillRate = sprintRefillRate;
        }
    }
    
    [Command]
    public void CmdAssignStaminaPenaltyRate(float newPenaltyRate)
    {
        if (isServer)
        {
            staminaPenaltyRate = newPenaltyRate;
            UpdateStaminaPenaltyRateForAll();
        }
    }
    
    [Server]
    public void UpdateStaminaPenaltyRateForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.staminaPenaltyRate = staminaPenaltyRate;
        }
    }
    
    [Command]
    public void CmdAssignStaminaRegenerationRate(float newRegenerationRate)
    {
        if (isServer)
        {
            staminaRegenerationRate = newRegenerationRate;
            UpdateStaminaRegenerationRateForAll();
        }
    }
    
    [Server]
    public void UpdateStaminaRegenerationRateForAll()
    {
        foreach (Player player in AllPlayers)
        {
            player.staminaRegenerationRate = staminaRegenerationRate;
        }
    }
    
 

 



    public override void OnStartLocalPlayer() {
        
        Camera playerCamera = GetComponentInChildren<Camera>();
        AudioListener audioListener = GetComponentInChildren<AudioListener>();
        if (playerCamera != null) {
            playerCamera.enabled = true; // Enable camera for local player
        }
        
        
        

        Debug.Log("Player has been spawned on the client!");
        // Find and assign the hostage area from the scene
        GameObject hostageAreaGameObject = GameObject.FindGameObjectWithTag("HostageArea");
        if (hostageAreaGameObject != null)
        {
            hostageAreaTransform = hostageAreaGameObject.transform;
            Debug.Log("Hostage area found!");
        }
        else
        {
            Debug.LogError("Hostage area not found!");
        }
        
        // Initialize sprint and stamina variables
        currentSprintTime = maxStamina;
        currentStamina = maxStamina;
        
        sprintTimeSlider.maxValue = maxStamina;
        sprintTimeSlider.value = currentSprintTime;
        
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;

    }


 


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        TogglePlayerControl(true);
        
        
        if (playerCamera != null && !isLocalPlayer) {
            playerCamera.enabled = false; // Disable camera for non-local players
            audioListener.enabled = false;
            
        }
        
    }



    void Update()
    {
        if (!isLocalPlayer)
            return;

            // Handling cursor locking and visibility toggle
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePlayerControl(false);
            }
            
            // Handling relocking the cursor with a mouse click
            if (!canControl && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) { // Mouse click to relock
                TogglePlayerControl(true);
            }

            if (canControl)
            {

                // We are grounded, so recalculate move direction based on axes
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                Vector3 right = transform.TransformDirection(Vector3.right);
                // Press Left Shift to run
                // Determine if the player is trying to sprint and has the resources to do so
                bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift);
                bool canSprint = isTryingToSprint && currentSprintTime > 0 && currentStamina > 0;

                // Determine the target speed based on whether the player is sprinting or walking
                float targetSpeed = canSprint ? runningSpeed : walkingSpeed;

                // Apply the target speed to movement calculations
                float curSpeedX = canMove ? targetSpeed * Input.GetAxis("Vertical") : 0;
                float curSpeedY = canMove ? targetSpeed * Input.GetAxis("Horizontal") : 0;
                
                float movementDirectionY = moveDirection.y;
                moveDirection = (forward * curSpeedX) + (right * curSpeedY);

                if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
                {
                    moveDirection.y = jumpSpeed;
                }
                else
                {
                    moveDirection.y = movementDirectionY;
                }

                // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
                // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
                // as an acceleration (ms^-2)
                if (!characterController.isGrounded)
                {
                    moveDirection.y -= gravity * Time.deltaTime;
                }

                // Move the controller
                characterController.Move(moveDirection * Time.deltaTime);

                // Player and Camera rotation
                if (canMove)
                {
                    rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                    rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                    playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                    transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
                }

                // Listen for role assignment
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    CmdAssignRole(PlayerRole.Catcher);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    CmdAssignRole(PlayerRole.Runner);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    CmdAssignRole(PlayerRole.Hostage);
                }
                
                
                // Sprinting Input
                if (canSprint)
                {
                    /// Drain sprint time and apply continuous stamina penalty
                    currentSprintTime -= Time.deltaTime * sprintDrainRate;
                    currentStamina -= Time.deltaTime * staminaPenaltyRate;

                    // Ensure sprint time and stamina don't go below zero
                    currentSprintTime = Mathf.Max(currentSprintTime, 0);
                    currentStamina = Mathf.Max(currentStamina, 0);
                }
                else
                {
                    // Refill sprint time and stamina when not sprinting
                    if (!isTryingToSprint && currentSprintTime < currentStamina)
                    {
                        currentSprintTime += Time.deltaTime * sprintRefillRate;
                    }

                    if (currentStamina < maxStamina)
                    {
                        currentStamina += Time.deltaTime * staminaRegenerationRate;
                    }

                }

                // Cap sprint time and stamina at their max values
                currentSprintTime = Mathf.Min(currentSprintTime, currentStamina);
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                
                
                sprintTimeSlider.value = currentSprintTime;
                staminaSlider.value = currentStamina;
            

        }
    }
    
    void TogglePlayerControl(bool shouldEnable) {
        if (shouldEnable) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canControl = true; // Enable player control
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canControl = false; // Disable player control
        }
    }
    
    
    
    [Command]
    public void CmdAssignRole(PlayerRole newRole)
    {
        playerRole = newRole; // Assign the new role, triggering OnRoleChanged
        
    }
     
    
    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Color roleColor = GetColorForRole(newRole); // Determine color based on new role
        GetComponent<Renderer>().material.color = roleColor; // Update color
        
        if (playerRole == PlayerRole.Hostage)
        {
            gameObject.layer = LayerMask.NameToLayer("Hostage");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("NonHostage");
        }
    }
    
    private Color GetColorForRole(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Catcher:
                return catcherColor;
            case PlayerRole.Runner:
                return runnerColor;
            case PlayerRole.Hostage:
                return hostageColor;
            default:
                return Color.white; // Default color if role isn't set or is None
        }
    }
    
    




    /// <summary>
    /// Called after player has spawned in the scene.
    /// </summary>
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
        base.OnStartServer();
        AllPlayers.Add(this);
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        AllPlayers.Remove(this);
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        
        //only the local player can tag other players
        if (!isLocalPlayer)
        {
            return;
        }
        // Get the player component of the object that was collided with
        Player otherPlayer = hit.gameObject.GetComponent<Player>();
    
        if (otherPlayer != null)
        {
            // If this player is a catcher and the other is a runner
            if (playerRole == PlayerRole.Catcher && otherPlayer.playerRole == PlayerRole.Runner)
            {
                // The catcher (this player) tags the runner (other player)
                CmdTagPlayer(otherPlayer.netId);
            }
            // If this player is a runner and accidentally touches a catcher
            else if (playerRole == PlayerRole.Runner && otherPlayer.playerRole == PlayerRole.Catcher)
            {
                
                // The runner (this player) becomes the hostage
                CmdTagPlayer(netId); 
            } else if ((playerRole == PlayerRole.Runner && otherPlayer.playerRole == PlayerRole.Hostage) ||
                       (playerRole == PlayerRole.Hostage && otherPlayer.playerRole == PlayerRole.Runner))
            {
                // The Hostage becomes a Runner again
                Player hostage = (playerRole == PlayerRole.Hostage) ? this : otherPlayer;
                CmdFreeHostage(hostage.netId);
            }
        }
    }

    [Command]
    void CmdTagPlayer(uint playerId)
    {
        // Find the player based on NetworkIdentity and change their role
        NetworkIdentity.spawned.TryGetValue(playerId, out NetworkIdentity playerIdentity);
        Player player = playerIdentity.GetComponent<Player>();
        if (player != null && player.playerRole == PlayerRole.Runner) // Confirming it's indeed a runner
        {
            player.playerRole = PlayerRole.Hostage; // Update role to Hostage
            player.RpcTeleportToHostageArea();
        }
    }
    
    [Command]
    void CmdFreeHostage(uint hostageId)
    {
        // Find the player based on NetworkIdentity and change their role
        NetworkIdentity.spawned.TryGetValue(hostageId, out NetworkIdentity hostageIdentity);
        Player hostage = hostageIdentity.GetComponent<Player>();
        if (hostage != null && hostage.playerRole == PlayerRole.Hostage)
        {
            hostage.playerRole = PlayerRole.Runner;  // Update role to Runner
            // Other logic for freeing the hostage, like notifying players, can go here
        }
    }

    [ClientRpc]
    public void RpcTeleportToHostageArea()
    {
        // Teleport the player to the hostage area
        if (hostageAreaTransform != null)
        {
            // Disable the CharacterController to "ghost" the player
            characterController.enabled = false;

            // Teleport the player to the target location
            transform.position = hostageAreaTransform.position;

            // Optionally wait for a frame to let physics and other systems update
            // yield return null;

            // Re-enable the CharacterController to solidify the player again
            characterController.enabled = true;
        }
    }

    
    
    
    
    
}
*/