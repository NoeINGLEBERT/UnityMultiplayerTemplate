using Photon.Pun;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI; // For Image component
using UnityEngine.Networking; // For UnityWebRequest
using System.Collections; // For IEnumerator

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Profile Panel References")]
    [SerializeField] private TMP_Text usernameText; // Reference to the username text in the profile panel
    [SerializeField] private TMP_Text mmrText; // Reference to the MMR text in the profile panel
    [SerializeField] private Image avatarImage; // Reference to the avatar image in the profile panel

    private string playerUsername = "Unknown"; // Default username
    private int playerMMR = 0; // Default MMR
    private string avatarUrl = ""; // Default avatar URL

    private void Start()
    {
        // Wait for PlayFabAuth to handle login and call Photon connection
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        loginPanel.SetActive(false);
        profilePanel.SetActive(true);
        roomPanel.SetActive(true);

        // Fetch player username, avatar URL, and MMR
        GetPlayerAccountInfo();
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
        PhotonNetwork.CreateRoom("TestRoom", new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No rooms available, creating a new one.");
        CreateRoom();
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

        PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);

        roomPanel.SetActive(false);

        gameObject.GetComponent<PlayerListUI>().enabled = true;
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
