using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;
using Photon.Chat;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;          // Chat UI Panel
    [SerializeField] private TMP_InputField chatInput;      // Chat input field
    [SerializeField] private Transform messageContainer;    // Message container
    [SerializeField] private GameObject chatMessagePrefab;  // Chat message prefab

    private ChatClient chatClient;
    public bool isChatOpen = false;
    private string userId;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Not connected to Photon! Chat will not work.");
            return;
        }

        userId = PhotonNetwork.NickName;
        chatClient = new ChatClient(this);
        chatClient.UseBackgroundWorkerForSending = true; // Helps with async communication

        Photon.Chat.AuthenticationValues authValues = new Photon.Chat.AuthenticationValues(userId);
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, "1.0", authValues);
    }

    private void Update()
    {
        if (chatClient != null)
        {
            chatClient.Service();
            Debug.Log("Photon Chat State: " + chatClient.State); // Monitor state changes
        }
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Return)) // Open chat or send message
        {
            if (isChatOpen)
            {
                if (!string.IsNullOrWhiteSpace(chatInput.text))
                {
                    SendMessageToChat(chatInput.text);
                    chatInput.text = ""; // Clear input field
                }
                CloseChat();
            }
            else
            {
                OpenChat();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isChatOpen) // Close chat without sending
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
        chatInput.text = ""; // Clear input field
    }

    private void SendMessageToChat(string message)
    {
        if (chatClient != null && chatClient.CanChat)
        {
            chatClient.PublishMessage("GlobalChat", message);
        }
    }

    public void OnConnected()
    {
        Debug.Log("Connected to Photon Chat");
        chatClient.Subscribe(new string[] { "GlobalChat" }); // Join a global chat channel
    }

    public void OnDisconnected()
    {
        Debug.LogWarning("Disconnected from Photon Chat. Attempting to reconnect...");

        StartCoroutine(TryReconnect()); // Start reconnection attempt
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        Debug.Log("Subscribed to " + string.Join(", ", channels));
        AddMessageToUI("<color=green>Connected to Global Chat</color>");
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            AddMessageToUI($"<b>{senders[i]}:</b> {messages[i]}");
        }
    }

    private void AddMessageToUI(string message)
    {
        GameObject newMessage = Instantiate(chatMessagePrefab, messageContainer);
        newMessage.GetComponent<TMP_Text>().text = message;
    }

    // New methods required by Photon Chat's interface
    public void OnUserSubscribed(string channel, string user)
    {
        Debug.Log($"{user} joined {channel}");
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        Debug.Log($"{user} left {channel}");
    }

    public void OnChatStateChange(ChatState state) { }
    public void OnPrivateMessage(string sender, object message, string channelName) { }
    public void OnUnsubscribed(string[] channels) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void DebugReturn(DebugLevel level, string message) { }

    private IEnumerator TryReconnect()
    {
        yield return new WaitForSeconds(5); // Wait before retrying

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon is not connected. Cannot reconnect to chat.");
            yield break;
        }

        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, "1.0", new Photon.Chat.AuthenticationValues(userId));
        Debug.Log("Reconnecting to Photon Chat...");
    }
}
