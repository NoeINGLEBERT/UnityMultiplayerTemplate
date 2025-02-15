using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;

public class PlayerListUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject playerListPanel; // Parent panel for player list
    [SerializeField] private GameObject playerItemPrefab; // Prefab for each player entry

    private Dictionary<int, GameObject> playerItems = new Dictionary<int, GameObject>();

    private void Start()
    {
        // Ensure the panel is disabled initially
        playerListPanel.SetActive(false);

        Debug.Log("PlayerListUI Start called");

        // Populate the list with all current players in the room
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            AddPlayer(player);
        }
    }

    private void Update()
    {
        // Toggle player list visibility with Tab
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            playerListPanel.SetActive(true);
        if (Keyboard.current.tabKey.wasReleasedThisFrame)
            playerListPanel.SetActive(false);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log("New player entered room: " + newPlayer.NickName);

        // Fetch and store the avatar URL in the Photon custom properties
        //GetAvatarUrlFromPlayFab(newPlayer);
        AddPlayer(newPlayer);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log("Player left the room: " + otherPlayer.NickName);
        RemovePlayer(otherPlayer);
    }

    private void AddPlayer(Photon.Realtime.Player player)
    {
        if (playerItems.ContainsKey(player.ActorNumber)) return;

        Debug.Log("Adding player: " + player.NickName);

        // Temporarily enable the panel if it's inactive
        bool wasActive = playerListPanel.activeSelf;
        if (!wasActive)
            playerListPanel.SetActive(true);

        // Instantiate player item
        GameObject playerItem = Instantiate(playerItemPrefab, playerListPanel.transform.Find("PlayerListPanel"));
        TMP_Text usernameText = playerItem.transform.Find("UsernameText").GetComponent<TMP_Text>();

        if (usernameText != null)
        {
            usernameText.text = string.IsNullOrEmpty(player.NickName) ? "Unknown" : player.NickName;
        }
        else
        {
            Debug.LogError("UsernameText not found in player item prefab!");
        }

        // Get the avatar URL from the Photon custom properties
        if (player.CustomProperties.ContainsKey("AvatarUrl"))
        {
            string avatarUrl = player.CustomProperties["AvatarUrl"] as string;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                Image avatarImage = playerItem.transform.Find("AvatarImage").GetComponent<Image>();
                if (avatarImage != null)
                {
                    StartCoroutine(LoadAvatarImage(avatarUrl, avatarImage));
                }
                else
                {
                    Debug.LogError("AvatarImage not found in player item prefab!");
                }
            }
        }

        playerItems[player.ActorNumber] = playerItem;

        // Restore panel's active state
        if (!wasActive)
            playerListPanel.SetActive(false);
    }


    private void RemovePlayer(Photon.Realtime.Player player)
    {
        if (playerItems.ContainsKey(player.ActorNumber))
        {
            Debug.Log("Removing player: " + player.NickName);
            Destroy(playerItems[player.ActorNumber]);
            playerItems.Remove(player.ActorNumber);
        }
    }

    private void GetAvatarUrlFromPlayFab(Photon.Realtime.Player player)
    {
        // Call PlayFab API to get the player's profile (assuming their PlayFabId is available)
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest
        {
            PlayFabId = player.UserId // You must ensure the player has a PlayFabUserId
        },
        result =>
        {
            // Extract the avatar URL from PlayFab account info
            string avatarUrl = result.AccountInfo.TitleInfo.AvatarUrl;

            // Now set the avatar URL on Photon player's custom properties
            SetAvatarUrlInPhoton(player, avatarUrl);

        },
        error =>
        {
            Debug.LogError("Error fetching PlayFab account info: " + error.GenerateErrorReport());
        });
    }

    private void SetAvatarUrlInPhoton(Photon.Realtime.Player player, string avatarUrl)
    {
        // Set the Avatar URL in the Photon custom properties
        ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable();
        playerProperties["AvatarUrl"] = avatarUrl;

        // Apply custom properties to the player
        player.SetCustomProperties(playerProperties);

        Debug.Log("Avatar URL set for player: " + player.NickName);

        // After setting the custom properties, you can also update the UI if needed
        if (!string.IsNullOrEmpty(avatarUrl))
        {
            Image avatarImage = playerItems[player.ActorNumber].transform.Find("AvatarImage").GetComponent<Image>();
            if (avatarImage != null)
            {
                StartCoroutine(LoadAvatarImage(avatarUrl, avatarImage));
            }
            else
            {
                Debug.LogError("AvatarImage not found in player item prefab!");
            }
        }
    }

    private IEnumerator LoadAvatarImage(string url, Image avatarImage)
    {
        // Temporarily enable the panel if it's inactive
        bool wasActive = playerListPanel.activeSelf;
        if (!wasActive)
            playerListPanel.SetActive(true);

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            else
            {
                Debug.LogError($"Failed to load avatar image: {webRequest.error}");
            }
        }

        // Restore panel's active state
        if (!wasActive)
            playerListPanel.SetActive(false);
    }
}
