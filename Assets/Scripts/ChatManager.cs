using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Chat;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject chatMessagePrefab;

    [Header("Message Colors")]
    [SerializeField] private Color joinColor = Color.green; // Color for when someone joins a channel
    [SerializeField] private Color leaveColor = Color.red; // Color for when someone leaves a channel

    private ChatClient chatClient;
    private string userId;
    private string currentChannel = "Main Lobby";  // Default channel to the lobby
    public bool isChatOpen = false;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Not connected to Photon! Chat will not work.");
            return;
        }

        userId = PhotonNetwork.NickName;
        chatClient = new ChatClient(this);
        chatClient.UseBackgroundWorkerForSending = true;

        Photon.Chat.AuthenticationValues authValues = new Photon.Chat.AuthenticationValues(userId);
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, "1.0", authValues);
    }

    private void Update()
    {
        if (chatClient != null)
        {
            chatClient.Service();
            Debug.Log("Photon Chat State: " + chatClient.State);
        }
        HandleInput();
    }

    private void HandleInput()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isChatOpen)
            {
                if (!string.IsNullOrWhiteSpace(chatInput.text))
                {
                    SendMessageToChat(chatInput.text);
                    chatInput.text = "";
                }
                CloseChat();
            }
            else
            {
                OpenChat();
            }
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame && isChatOpen)
        {
            CloseChat();
        }
    }

    private void OpenChat()
    {
        isChatOpen = true;
        chatPanel.SetActive(true);
        chatInput.Select();
        chatInput.ActivateInputField();
    }

    private void CloseChat()
    {
        isChatOpen = false;
        chatPanel.SetActive(false);
        chatInput.DeactivateInputField();
        chatInput.text = "";
    }

    private void SendMessageToChat(string message)
    {
        if (chatClient != null && chatClient.CanChat)
        {
            chatClient.PublishMessage(currentChannel, message);
        }
    }

    public void OnConnected()
    {
        Debug.Log("Connected to Photon Chat");
        chatClient.Subscribe(new string[] { currentChannel });
    }

    public void OnDisconnected()
    {
        Debug.LogWarning("Disconnected from Photon Chat. Attempting to reconnect...");
        StartCoroutine(TryReconnect());
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        Debug.Log("Subscribed to " + string.Join(", ", channels));
        AddMessageToUI($"<color=#{ColorUtility.ToHtmlStringRGB(joinColor)}>Joined {currentChannel}</color>");
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        if (channelName == currentChannel)
        {
            for (int i = 0; i < senders.Length; i++)
            {
                AddMessageToUI($"<b>{senders[i]}:</b> {messages[i]}");
            }
        }
    }

    private void AddMessageToUI(string message)
    {
        GameObject newMessage = Instantiate(chatMessagePrefab, messageContainer);
        newMessage.GetComponent<TMP_Text>().text = message;
    }

    public void OnUserSubscribed(string channel, string user)
    {
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(joinColor)}>{user} joined {channel}</color>");
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(leaveColor)}>{user} left {channel}</color>");
    }

    public void OnChatStateChange(ChatState state) { }
    public void OnPrivateMessage(string sender, object message, string channelName) { }
    public void OnUnsubscribed(string[] channels) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void DebugReturn(DebugLevel level, string message) { }

    private IEnumerator TryReconnect()
    {
        yield return new WaitForSeconds(5);
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon is not connected. Cannot reconnect to chat.");
            yield break;
        }
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, "1.0", new Photon.Chat.AuthenticationValues(userId));
        Debug.Log("Reconnecting to Photon Chat...");
    }

    public void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        // Switch to the new room-specific channel
        currentChannel = PhotonNetwork.CurrentRoom.Name;

        // Subscribe to the new room's chat channel
        chatClient.Subscribe(new string[] { currentChannel });

        // Unsubscribe from the previous lobby chat
        chatClient.Unsubscribe(new string[] { "Main Lobby" });
    }

    public void OnLeftRoom()
    {
        Debug.Log("Left Room: " + PhotonNetwork.CurrentRoom.Name);

        // Switch back to the main lobby channel
        currentChannel = "Main Lobby";
        chatClient.Subscribe(new string[] { currentChannel });

        // Unsubscribe from the room's chat channel
        chatClient.Unsubscribe(new string[] { "Room_" + PhotonNetwork.CurrentRoom.Name });

        // Notify UI of the channel switch
        AddMessageToUI($"<color=#{ColorUtility.ToHtmlStringRGB(joinColor)}>Left Room, switched back to Lobby Chat</color>");
    }
}
