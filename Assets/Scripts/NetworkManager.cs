using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private Transform previousRoomsContent;
    [SerializeField] private GameObject previousRoomItemPrefab;

    [Header("Profile Panel References")]
    [SerializeField] private TMP_Text usernameText; // Reference to the username text in the profile panel
    [SerializeField] private TMP_Text mmrText; // Reference to the MMR text in the profile panel
    [SerializeField] private Image avatarImage; // Reference to the avatar image in the profile panel

    [SerializeField] private GameObject chatManager;
    [SerializeField] private GameObject lobbyManager;

    public List<string> previousRoomsList = new List<string>();

    private string playerUsername = "Unknown"; // Default username
    private int playerMMR = 0; // Default MMR
    private string avatarUrl = ""; // Default avatar URL

    private string currentRoomName;

    [SerializeField] private int maxPlayer;

    private void Start()
    {
        // Wait for PlayFabAuth to handle login and call Photon connection
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");

        PhotonNetwork.JoinLobby();
        
        loginPanel.SetActive(false);
        profilePanel.SetActive(true);
        roomPanel.SetActive(true);
        chatManager.SetActive(true);

        // Fetch player username, avatar URL, and MMR
        GetPlayerAccountInfo();
        GetPreviousRooms();
    }

    private void GetPlayerAccountInfo()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), OnGetAccountInfoSuccess, OnGetAccountInfoFailure);
    }

    private void OnGetAccountInfoSuccess(GetAccountInfoResult result)
    {
        Debug.Log("Successfully retrieved account info from PlayFab.");

        // Get username from the DisplayName
        if (!string.IsNullOrEmpty(result.AccountInfo.TitleInfo.DisplayName))
        {
            playerUsername = result.AccountInfo.TitleInfo.DisplayName;
        }
        else
        {
            Debug.LogWarning("DisplayName is empty or null. Default username will be used.");
        }

        // Get avatar URL
        if (!string.IsNullOrEmpty(result.AccountInfo.TitleInfo.AvatarUrl))
        {
            avatarUrl = result.AccountInfo.TitleInfo.AvatarUrl;

            ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable();
            playerProperties["AvatarUrl"] = avatarUrl;

            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
        else
        {
            Debug.LogWarning("Avatar URL is empty or null. Default avatar will be used.");
        }

        // Fetch MMR from UserData
        GetPlayerMMR();
    }

    private void OnGetAccountInfoFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to retrieve account info from PlayFab: {error.ErrorMessage}");
    }

    private void GetPlayerMMR()
    {
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request, OnGetUserDataSuccess, OnGetUserDataFailure);
    }

    private void OnGetUserDataSuccess(GetUserDataResult result)
    {
        Debug.Log("Successfully retrieved player data from PlayFab.");

        if (result.Data != null && result.Data.ContainsKey("MMR"))
        {
            int.TryParse(result.Data["MMR"].Value, out playerMMR);
        }

        // Update the profile panel
        UpdateProfilePanel();
    }

    private void OnGetUserDataFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to retrieve player MMR data from PlayFab: {error.ErrorMessage}");
    }

    public void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayer,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", false } },
            CustomRoomPropertiesForLobby = new string[] { "GameStarted" }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Clear existing room items
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // Populate the room list
        foreach (RoomInfo room in roomList)
        {
            if (!room.RemovedFromList && room.CustomProperties.TryGetValue("GameStarted", out object started))
            {
                bool gameStarted = (bool)started;
                if (!gameStarted)
                {
                    GameObject roomItem = Instantiate(roomListItemPrefab, roomListContent);
                    roomItem.GetComponentInChildren<TMP_Text>().text = room.Name;
                    roomItem.GetComponent<Button>().onClick.AddListener(() => JoinRoom(room.Name));
                }
            }
            else
            {
                Debug.LogWarning($"Room {room.Name} missing 'GameStarted' property or is removed.");
            }
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room successfully!");

        // Set player properties
        ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable
    {
        { "AvatarUrl", PlayFabAuth.AvatarUrl },
        { "Username", PhotonNetwork.NickName }
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("GameStarted", out object gameStartedObj))
        {
            bool gameStarted = (bool)gameStartedObj;

            if (gameStarted)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    // Set game as started
                    ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", true } };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                    // Save game state to PlayFab
                    if (!previousRoomsList.Contains(PhotonNetwork.CurrentRoom.Name))
                    {
                        previousRoomsList.Add(PhotonNetwork.CurrentRoom.Name);
                        SavePreviousRooms();
                    }
                }

                PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
            }
            else
            {
                lobbyManager.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("GameStarted property not found. Defaulting to 'RoomLobby' scene...");
            PhotonNetwork.LoadLevel("RoomLobby");
        }

        roomPanel.SetActive(false);

        gameObject.GetComponent<PlayerListUI>().enabled = true;

        // Trigger a method in the ChatManager to handle the room-specific chat
        FindFirstObjectByType<ChatManager>().OnJoinedRoom();
    }

    private void GetPreviousRooms()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("PreviousRooms"))
            {
                previousRoomsList = new List<string>(result.Data["PreviousRooms"].Value.Split(','));
                UpdatePreviousRoomsUI();
            }
        }, error => Debug.LogError("Failed to fetch previous rooms: " + error.ErrorMessage));
    }

    private void UpdatePreviousRoomsUI()
    {
        foreach (Transform child in previousRoomsContent)
            Destroy(child.gameObject);

        foreach (string roomName in previousRoomsList)
        {
            GameObject roomItem = Instantiate(previousRoomItemPrefab, previousRoomsContent);
            roomItem.GetComponentInChildren<TMP_Text>().text = roomName;
            roomItem.GetComponent<Button>().onClick.AddListener(() => TryRejoinRoom(roomName));
        }
    }

    private void TryRejoinRoom(string roomName)
    {
        currentRoomName = roomName;
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"Failed to join room: {message}");
        Debug.Log($"Tried to join room: {currentRoomName}");

        // Check if the failure reason is "Game does not exist"
        if (message.Contains("Game does not exist"))
        {
            Debug.Log("Room doesn't exist, recreating room.");
            RecreateRoom(currentRoomName); // Pass the room name to recreate it
        }
        else
        {
            Debug.Log("Join room failed due to a different reason, not recreating.");
        }
    }

    public void RecreateRoom(string roomName)
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayer,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", true } },
            CustomRoomPropertiesForLobby = new string[] { "GameStarted" }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void SavePreviousRooms()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "PreviousRooms", string.Join(",", previousRoomsList) } }
        }, result => Debug.Log("Previous rooms saved."),
        error => Debug.LogError("Failed to save previous rooms: " + error.ErrorMessage));
    }

    public void CloseGame(string roomName)
    {
        if (previousRoomsList.Contains(roomName))
        {
            previousRoomsList.Remove(roomName);
            SavePreviousRooms();
        }
    }

    /// <summary>
    /// Updates the profile panel with player username, MMR, and avatar image.
    /// </summary>
    private void UpdateProfilePanel()
    {
        if (usernameText != null)
        {
            usernameText.text = $"{playerUsername}";
        }
        else
        {
            Debug.LogWarning("UsernameText is not assigned in the inspector.");
        }

        if (mmrText != null)
        {
            mmrText.text = $"MMR: {playerMMR}";
        }
        else
        {
            Debug.LogWarning("MMRText is not assigned in the inspector.");
        }

        if (avatarImage != null && !string.IsNullOrEmpty(avatarUrl))
        {
            StartCoroutine(LoadAvatarImage(avatarUrl));
        }
        else
        {
            Debug.LogWarning("AvatarImage is not assigned in the inspector or Avatar URL is empty.");
        }
    }

    /// <summary>
    /// Loads an image from a URL and sets it as the sprite for the avatar image.
    /// </summary>
    private IEnumerator LoadAvatarImage(string url)
    {
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
                Debug.LogError($"Failed to load avatar image from URL: {webRequest.error}");
            }
        }
    }
}
