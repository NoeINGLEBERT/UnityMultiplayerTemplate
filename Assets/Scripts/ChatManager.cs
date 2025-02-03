using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class ChatManager : MonoBehaviourPun
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;          // Panel containing the chat UI
    [SerializeField] private TMP_InputField chatInput;      // Text input field for typing messages
    [SerializeField] private Transform messageContainer;    // Parent object (with Vertical Layout Group) for messages
    [SerializeField] private GameObject chatMessagePrefab;  // Prefab for chat messages

    public bool isChatOpen = false;                        // Is the chat currently open?

    private void Start()
    {
        CloseChat(); // Ensure the chat starts closed
    }

    private void Update()
    {
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
        photonView.RPC("RPC_AddMessage", RpcTarget.All, "<b>" + PhotonNetwork.NickName + ":</b> " + message);
    }

    [PunRPC]
    private void RPC_AddMessage(string message)
    {
        AddMessageToUI(message);
    }

    private void AddMessageToUI(string message)
    {
        // Instantiate a new message from the prefab
        GameObject newMessage = Instantiate(chatMessagePrefab, messageContainer);
        newMessage.GetComponent<TMP_Text>().text = message;
    }
}
