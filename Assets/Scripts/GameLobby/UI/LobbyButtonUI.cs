using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls an entry in the join menu's list of lobbies, acting as a clickable button as well as displaying info about the lobby.
    /// </summary>

    //TODO WHAT WAS THIS OBSERVING??!?
    public class LobbyButtonUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text lobbyNameText;
        [SerializeField]
        TMP_Text lobbyCountText;

        /// <summary>
        /// Subscribed to on instantiation to pass our lobby data back
        /// </summary>
        public UnityEvent<LocalLobby> onLobbyPressed;
        LocalLobby m_Lobby;

        /// <summary>
        /// UI CallBack
        /// </summary>
        public void OnLobbyClicked()
        {
           onLobbyPressed.Invoke(m_Lobby);
        }

        public void SetLobby(LocalLobby lobby)
        {
            m_Lobby = lobby;
            SetLobbyname(m_Lobby.LobbyName.Value);
            SetLobbyCount(m_Lobby.LocalPlayers);
            m_Lobby.LobbyName.onChanged += SetLobbyname;
            m_Lobby.onUserListChanged += SetLobbyCount;

        }

        void SetLobbyname(string lobbyName)
        {
            lobbyNameText.SetText(m_Lobby.LobbyName.Value);
        }

        void SetLobbyCount(Dictionary<string, LocalPlayer> userList)
        {
            lobbyCountText.SetText($"{userList.Count}/{m_Lobby.MaxPlayerCount}");
        }
    }
}
