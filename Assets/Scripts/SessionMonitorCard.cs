using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SessionMonitorCard : MonoBehaviour
{
    [HideInInspector]
    public Player player;
    [SerializeField]
    private TMPro.TextMeshProUGUI m_displayNameTextArea;
    [SerializeField]
    private Image m_playerImageArea;
    
    public void SetPlayerInfo(Player _player)
    {
        player = _player;
        m_displayNameTextArea.text = player.displayName.Value.ToString();
        //change the color of the player according to its role 
        switch (player.playerRole.Value)
        {
            case Player.PlayerRole.None:
                m_playerImageArea.color = Color.white;
                break;
            case Player.PlayerRole.Catcher:
                //below code changes the color in the inspector but not in the game
                m_playerImageArea.color = player.catcherColor;
                break;
            case Player.PlayerRole.Runner:
                m_playerImageArea.color = player.runnerColor;
                break;
            case Player.PlayerRole.Hostage:
                m_playerImageArea.color = player.hostageColor;
                break;
        }
    }
}
