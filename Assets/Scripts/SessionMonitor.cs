using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SessionMonitor : MonoBehaviour
{
    [SerializeField]
    private GameObject sessionMonitorCardPrefab;
    [SerializeField]
    private Transform playerCardsContainer; 
    

    public void RegisterPlayer(Player _player)
    {
        Debug.Log("Registering player: " + _player);
        // Subscribe to player events
        _player.OnPlayerRoleChanged += HandlePlayerRoleChanged;
    }
    
    public void HandlePlayerRoleChanged(Player _player, Player.PlayerRole newRole)
    {
        Debug.Log("Session Monitor/ Player role changed: " + _player.displayName.Value.ToString() + " to " + newRole);
        
        // Attempt to find an existing player card by playerId
        SessionMonitorCard existingCard = playerCardsContainer.GetComponentsInChildren<SessionMonitorCard>()
            .FirstOrDefault(card => card.player.NetworkObjectId == _player.NetworkObjectId);

        if (existingCard != null) {
            // Update existing card
            existingCard.SetPlayerInfo(_player);
        } else {
            // Instantiate a new player card and set its information
            GameObject newCardObj = Instantiate(sessionMonitorCardPrefab, playerCardsContainer);
            SessionMonitorCard newCardScript = newCardObj.GetComponent<SessionMonitorCard>();
            newCardScript.SetPlayerInfo(_player);
            // If you implement a unique identifier, set it here as well
            // newCardScript.PlayerId = playerId;
        }
        
    }
}
