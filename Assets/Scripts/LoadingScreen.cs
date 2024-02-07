using System.Collections;
using System.Collections.Generic;
using LobbyRelaySample;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject PlayerCardPrefab;
    [SerializeField]
    private GameObject PlayerCardContainer;
    
    private int m_spawnedPlayerCount = 0;
    
    public void AddSpawnedPlayer(Player player)
    {
        Debug.Log("Player " + player.OwnerClientId + " has spawned");
        m_spawnedPlayerCount ++;
        
        //add a player card and change its name to to the player's name
        GameObject playerCard = Instantiate(PlayerCardPrefab, PlayerCardContainer.transform);
        playerCard.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = player.OwnerClientId.ToString();
        
        if (GameManager.Instance.m_LocalLobby.PlayerCount == m_spawnedPlayerCount)
        {
            Debug.Log("All players have spawned");
            DisableLoadingScreen();
        }
        
    }
    
    //5 second courotine to be sure that everything is okay after everyone has spawned
    private void DisableLoadingScreen()
    {
        //yield return new WaitForSeconds(5);
        
        //find all player prefabs with Player tag and disable them
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            player.gameObject.GetComponent<Player>().AllPlayersSpawned();
        }
        gameObject.SetActive(false);
    }

}
