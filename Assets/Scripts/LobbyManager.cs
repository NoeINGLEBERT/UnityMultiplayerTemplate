using Photon.Pun;
using UnityEngine;
using TMPro;
using ExitGames.Client.Photon;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Player = Photon.Realtime.Player;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject leaveButton;
    [SerializeField] private TMP_Text readyStatusText;

    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ChatManager chatManager;

    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    public override void OnEnable()
    {
        if (PhotonNetwork.InRoom)
        {
            lobbyPanel.SetActive(true);

            UpdatePlayerCount();
            UpdateReadyStatus();
        }
    }

    private void UpdatePlayerCount()
    {
        playerCountText.text = $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCount();
        playerReadyStatus[newPlayer.ActorNumber] = false; // New player is not ready by default
        UpdateReadyStatus();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        UpdateReadyStatus();
    }

    public void SetReady()
    {
        // Toggle ready status
        bool isReady = !GetPlayerReadyStatus(PhotonNetwork.LocalPlayer);
        playerReadyStatus[PhotonNetwork.LocalPlayer.ActorNumber] = isReady;

        // Update properties in Photon
        Hashtable properties = new Hashtable { { "IsReady", isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        // Update UI
        UpdateReadyStatus();

        // Check if the game should start
        if (AllPlayersReady())
        {
            StartGame();
        }
    }

    private void UpdateReadyStatus()
    {
        int readyCount = 0;
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (GetPlayerReadyStatus(player))
                readyCount++;
        }

        readyStatusText.text = $"Ready: {readyCount}/{PhotonNetwork.CurrentRoom.PlayerCount}";

        // Update button text
        bool isReady = GetPlayerReadyStatus(PhotonNetwork.LocalPlayer);
        readyButton.GetComponentInChildren<TMP_Text>().text = isReady ? "Ready" : "Unready";
    }

    private bool GetPlayerReadyStatus(Player player)
    {
        return player.CustomProperties.ContainsKey("IsReady") && (bool)player.CustomProperties["IsReady"];
    }

    private bool AllPlayersReady()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
            return false;

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!GetPlayerReadyStatus(player))
                return false;
        }

        return true;
    }

    private void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Set game as started
            Hashtable roomProperties = new Hashtable { { "GameStarted", true } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            // Save game state to PlayFab
            if (!networkManager.previousRoomsList.Contains(PhotonNetwork.CurrentRoom.Name))
            {
                networkManager.previousRoomsList.Add(PhotonNetwork.CurrentRoom.Name);
                networkManager.SavePreviousRooms();
            }
        }

        lobbyPanel.SetActive(false);

        PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
    }

    public void LeaveRoom()
    {
        lobbyPanel.SetActive(false);
        chatManager.OnLeftRoom();
        PhotonNetwork.LeaveRoom();
        gameObject.SetActive(false);
    }
}