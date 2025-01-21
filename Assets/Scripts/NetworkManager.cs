using Photon.Pun;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon server
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server!");
        // You can now join or create a room
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");
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
        PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
    }
}