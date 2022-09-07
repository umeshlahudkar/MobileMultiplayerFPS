using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Login UI Panel")]
    [SerializeField] private GameObject LoginUIPanel;
    [SerializeField] private InputField nameInputField;

    [Header("Connection Status Text")]
    [SerializeField] private Text connectionStatusText;

    [Header("Game Option UI Panel")]
    [SerializeField] private GameObject gameOptionUIPanel;

    [Header("Create Room Panel")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private InputField roomNameInputField;
    [SerializeField] private InputField maxPlayerInputField;

    [Header("Inside Room Panel")]
    [SerializeField] private GameObject insideRoomPanel;
    [SerializeField] private Text roomInfo;
    [SerializeField] private GameObject playerListEntryPrefab;
    [SerializeField] private GameObject playerListParentObject;
    [SerializeField] private GameObject startButton;

    [Header("Room list Panel")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private GameObject roomListEntryPrefab;
    [SerializeField] private GameObject roomListParentObject;

    [Header("Random Panel")]
    [SerializeField] private GameObject joinRandomRoomPanel;

    private Dictionary<string, RoomInfo> catchedRooms = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListGameObjects = new Dictionary<string, GameObject>();
    private Dictionary<int, GameObject> playerLists = new Dictionary<int, GameObject>();

    #region Unity Methods

    private void Start()
    {
        LoginUIPanel.SetActive(true);
        gameOptionUIPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        joinRandomRoomPanel.SetActive(false);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Update()
    {
        // getting status using Nwtwork client state
        connectionStatusText.text = "Connection Status " + PhotonNetwork.NetworkClientState;
    }

    #endregion


    #region UI Callbacks
    public void OnLoginButtonClick()
    {
        string playerName = nameInputField.text;
        if(!string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("Player Name is Invalid");
        }
    }

    public void OnCreateRoomButtonClick()
    {
        LoginUIPanel.SetActive(false);
        gameOptionUIPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    public void OnRoomCreateButtonClick()
    {
        string roomName = roomNameInputField.text;

        if(string.IsNullOrEmpty(roomName))
        {
            roomName = "Room " + Random.Range(0, 1000);
        }

        RoomOptions roomOptions = new RoomOptions();
        if(!string.IsNullOrEmpty(maxPlayerInputField.text))
        {
            roomOptions.MaxPlayers = (byte)int.Parse(maxPlayerInputField.text);
        }

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void OnCancleButtonClick()
    {
        gameOptionUIPanel.SetActive(true);
        createRoomPanel.SetActive(false);
        LoginUIPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
    }

    public void OnShowRoomButtonClick()
    {
        if(!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        EnableRoomListPanel();
    }

    public void OnBackButtonClick()
    {
        if(PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        OnCancleButtonClick();
    }

    public void OnLeaveButtonClick()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnJoinRandomRoomButtonClick()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void OnStartGameButtonClick()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnected()
    {
        Debug.Log("Connected to the Internet");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " Connected to Photon Server");
        EnableOptionUIPanel();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " Room created");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " Joined " + PhotonNetwork.CurrentRoom.Name);
        EnableInsideRoomPanel();

        if(PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            startButton.SetActive(true);
        } 
        else
        {
            startButton.SetActive(false);
        }

        roomInfo.text = "Room Name : "+ PhotonNetwork.CurrentRoom.Name + " Player Count / Max Limit : " + PhotonNetwork.CurrentRoom.PlayerCount
                       + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;

        foreach(Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerListGameObject = Instantiate(playerListEntryPrefab, playerListParentObject.transform);

            playerListGameObject.transform.Find("PlayerNameText").GetComponent<Text>().text = player.NickName;
            if(player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                playerListGameObject.transform.Find("PlayerIndicator").gameObject.SetActive(true);
            }
            else
            {
                playerListGameObject.transform.Find("PlayerIndicator").gameObject.SetActive(false);
            }

            playerLists.Add(player.ActorNumber, playerListGameObject);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        roomInfo.text = "Room Name : " + PhotonNetwork.CurrentRoom.Name + " Player Count / Max Limit : " + PhotonNetwork.CurrentRoom.PlayerCount
                       + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;


        GameObject playerListGameObject = Instantiate(playerListEntryPrefab, playerListParentObject.transform);

        playerListGameObject.transform.Find("PlayerNameText").GetComponent<Text>().text = newPlayer.NickName;
        if (newPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            playerListGameObject.transform.Find("PlayerIndicator").gameObject.SetActive(true);
        }
        else
        {
            playerListGameObject.transform.Find("PlayerIndicator").gameObject.SetActive(false);
        }

        playerLists.Add(newPlayer.ActorNumber, playerListGameObject);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        roomInfo.text = "Room Name : " + PhotonNetwork.CurrentRoom.Name + " Player Count / Max Limit : " + PhotonNetwork.CurrentRoom.PlayerCount
                       + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;

        Destroy(playerLists[otherPlayer.ActorNumber].gameObject);
        playerLists.Remove(otherPlayer.ActorNumber);

        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            startButton.SetActive(true);
        }

    }

    public override void OnLeftRoom()
    {
        OnCancleButtonClick();

        foreach(GameObject player in playerLists.Values)
        {
            if(player != null)
                Destroy(player);
        }

        playerLists.Clear();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        foreach (RoomInfo room in roomList)
        {
            Debug.Log(room.Name);
            if(!room.IsOpen || !room.IsVisible || room.RemovedFromList)
            {
                if(catchedRooms.ContainsKey(room.Name))
                {
                    catchedRooms.Remove(room.Name);
                }
            } 
            else
            {
                if(catchedRooms.ContainsKey(room.Name))
                {
                    catchedRooms[room.Name] = room;
                }
                else
                {
                    catchedRooms.Add(room.Name, room);
                }
            }
        }

        foreach(RoomInfo room in catchedRooms.Values)
        {
            GameObject roomListEntryGameObject = Instantiate(roomListEntryPrefab, roomListParentObject.transform);
            roomListEntryGameObject.transform.localScale = Vector3.one;

            roomListEntryGameObject.transform.Find("RoomNameText").GetComponent<Text>().text = room.Name;
            roomListEntryGameObject.transform.Find("RoomPlayersText").GetComponent<Text>().text = room.PlayerCount + " / " + room.MaxPlayers;
            roomListEntryGameObject.transform.Find("JoinRoomButton").GetComponent<Button>().onClick.AddListener(() => OnJoinButtonClick(room.Name));

            roomListGameObjects.Add(room.Name, roomListEntryGameObject);
        }
    }

    public override void OnLeftLobby()
    {
        ClearRoomListView();
        catchedRooms.Clear();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room " + Random.Range(0, 1000);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 20; 

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    #endregion

    private void ClearRoomListView()
    {
        foreach (var roomListGameObject in roomListGameObjects.Values)
        {
            Destroy(roomListGameObject);
        }
        roomListGameObjects.Clear();
    }

    private void OnJoinButtonClick(string roomName)
    {
        if(PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(roomName);
    }
    private void EnableOptionUIPanel()
    {
        LoginUIPanel.SetActive(false);
        gameOptionUIPanel.SetActive(true);
    }

    private void EnableInsideRoomPanel()
    {
        insideRoomPanel.SetActive(true);
        LoginUIPanel.SetActive(false);
        gameOptionUIPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
    }

    private void EnableRoomListPanel()
    {
        roomListPanel.SetActive(true);
        LoginUIPanel.SetActive(false);
        gameOptionUIPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
    }

    private void EnableRandomRoomJoinPanel()
    {
        LoginUIPanel.SetActive(false);
        gameOptionUIPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        joinRandomRoomPanel.SetActive(true);
    }
}
