using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a button which will set the local player's emote state when pressed. This demonstrates a player updating their data within the room.
    /// </summary>
    public class RolePreferenceButtonUI : UIPanelBase
    {
        [SerializeField]
        RoleType m_roleType;

        public void SetPlayerRolePreference()
        {
            Debug.Log("SetPlayerRolePreference");
            Manager.SetLocalUserRolePreference(m_roleType);
        }
    }
}
