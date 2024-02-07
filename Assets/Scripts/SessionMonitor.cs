using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionMonitor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void RegisterPlayer(Player player)
    {
        Debug.Log("Registering player: " + player);
        // Subscribe to player events
        player.OnPlayerRoleChanged += HandlePlayerRoleChanged;
    }
    
    public void HandlePlayerRoleChanged(Player player, Player.PlayerRole newRole)
    {
        Debug.Log("Session Monitor/ Player role changed: " + player.displayName.Value.ToString() + " to " + newRole);
    }
}
