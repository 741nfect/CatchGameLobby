using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using Mirror;
using TMPro;

public class HostControlPanel : MonoBehaviour
{
    /*
    
    public Player player; // Assign this in the inspector to the player script component

    // Assign these in the inspector to the sliders
    public GameObject hostControlPanel; // Assign in inspector
    public Slider walkingSpeedSlider; // Assign in inspector
    public TMP_Text walkingSpeedText; // Assign in inspector
    public Slider runningSpeedSlider; // Assign in inspector
    public TMP_Text runningSpeedText; // Assign in inspector
    public Slider jumpSpeedSlider; // Assign in inspector3
    public TMP_Text jumpSpeedText; // Assign in inspector
    public Slider maxStaminaSlider; // Assign in inspector
    public TMP_Text maxStaminaText; // Assign in inspector
    public Slider sprintDrainRateSlider; // Assign in inspector
    public TMP_Text sprintDrainRateText; // Assign in inspector
    public Slider sprintRefillRateSlider; // Assign in inspector
    public TMP_Text sprintRefillRateText; // Assign in inspector
    public Slider staminaPenaltyRateSlider; // Assign in inspector
    public TMP_Text staminaPenaltyRateText; // Assign in inspector
    public Slider staminaRegenerationRateSlider; // Assign in inspector
    public TMP_Text staminaRegenerationRateText; // Assign in inspector
    

    // ... add other sliders

    void Start()
    {
        // Initialize the control panel with current values from player
        if (player.isLocalPlayer && player.isServer) // Ensure it's the host player
        {
            hostControlPanel.SetActive(true);
            InitializeSliders();
        }
        else
        {
            hostControlPanel.SetActive(false);
        }
    }

    void InitializeSliders()
    {

        // Set initial values of sliders from the Player script and add listener for changes
        walkingSpeedSlider.value = player.walkingSpeed;
        walkingSpeedText.text = "Walking Speed: " + walkingSpeedSlider.value.ToString("F5");
        walkingSpeedSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignWalkingSpeed(walkingSpeedSlider.value);
            walkingSpeedText.text = "Walking Speed: " + walkingSpeedSlider.value.ToString("F5"); // Update the text
        });
        
        runningSpeedSlider.value = player.runningSpeed;
        runningSpeedText.text = "Running Speed: " + runningSpeedSlider.value.ToString("F5");
        runningSpeedSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignRunningSpeed(runningSpeedSlider.value);
            runningSpeedText.text = "Running Speed: " + runningSpeedSlider.value.ToString("F5"); // Update the text
        });
        
        jumpSpeedSlider.value = player.jumpSpeed;
        jumpSpeedText.text = "Jump Speed: " + jumpSpeedSlider.value.ToString("F5");
        jumpSpeedSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignJumpSpeed(jumpSpeedSlider.value);
            jumpSpeedText.text = "Jump Speed: " + jumpSpeedSlider.value.ToString("F5"); // Update the text
        });
        
        maxStaminaSlider.value = player.maxStamina;
        maxStaminaText.text = "Max Stamina: " + maxStaminaSlider.value.ToString("F5");
        maxStaminaSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignMaxStamina(maxStaminaSlider.value);
            maxStaminaText.text = "Max Stamina: " + maxStaminaSlider.value.ToString("F5"); // Update the text
        });
        
        sprintDrainRateSlider.value = player.sprintDrainRate;
        sprintDrainRateText.text = "Sprint Drain Rate: " + sprintDrainRateSlider.value.ToString("F5");
        sprintDrainRateSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignSprintDrainRate(sprintDrainRateSlider.value);
            sprintDrainRateText.text = "Sprint Drain Rate: " + sprintDrainRateSlider.value.ToString("F5"); // Update the text
        });
        
        sprintRefillRateSlider.value = player.sprintRefillRate;
        sprintRefillRateText.text = "Sprint Refill Rate: " + sprintRefillRateSlider.value.ToString("F5");
        sprintRefillRateSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignSprintRefillRate(sprintRefillRateSlider.value);
            sprintRefillRateText.text = "Sprint Refill Rate: " + sprintRefillRateSlider.value.ToString("F5"); // Update the text
        });
        
        staminaPenaltyRateSlider.value = player.staminaPenaltyRate;
        staminaPenaltyRateText.text = "Stamina Penalty Rate: " + staminaPenaltyRateSlider.value.ToString("F5");
        staminaPenaltyRateSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignStaminaPenaltyRate(staminaPenaltyRateSlider.value);
            staminaPenaltyRateText.text = "Stamina Penalty Rate: " + staminaPenaltyRateSlider.value.ToString("F5"); // Update the text
        });
        
        staminaRegenerationRateSlider.value = player.staminaRegenerationRate;
        staminaRegenerationRateText.text = "Stamina Regeneration Rate: " + staminaRegenerationRateSlider.value.ToString("F5");
        staminaRegenerationRateSlider.onValueChanged.AddListener(delegate
        {
            player.CmdAssignStaminaRegenerationRate(staminaRegenerationRateSlider.value);
            staminaRegenerationRateText.text = "Stamina Regeneration Rate: " + staminaRegenerationRateSlider.value.ToString("F5"); // Update the text
        });

    }
    */
}