using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject localCube;



    public string ClientPlayerID;

    private List<PlayerController> ClientPlayerList = new List<PlayerController>();


    
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);




    }
    

    // send info to server
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    // on connection
    void OnConnect(){
        Debug.Log("We are now connected to the server");

        // Example to send a handshake message:
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = m_Connection.InternalId.ToString();
        SendToServer(JsonUtility.ToJson(m));



        // newPlayer = new NetworkObjects.NetworkPlayer();
        // newPlayer.id = m.player.id;

        // ClientPlayerList.Add(newPlayer);





        // // set client id
        // ClientPlayerID = m.player.id;

        
    }

    // on data received
    void OnData(DataStreamReader stream){

        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            // player handshake
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            SpawnPlayer(hsMsg.player);
            Debug.Log("Handshake message received!");
            break;
 
            // player update
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            ClientPlayerUpdate(puMsg.players);
            Debug.Log("Player update message received!");
            break;
            
            // server update
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            Debug.Log("Server update message received!");
            break;

            // player list update
            case Commands.PLAYER_LIST:
            ClientListMsg listMsg = JsonUtility.FromJson<ClientListMsg>(recMsg);
            for (int p = 0; p < listMsg.players.Length; p++)
            {
                SpawnPlayer(listMsg.players[p]);
            }
            Debug.Log("Player list message received!");
            break;
            
            // player disconnect
            case Commands.PLAYER_DISCONNECT:
            DiscPlayerMsg discMsg = JsonUtility.FromJson<DiscPlayerMsg>(recMsg);
            RemoveClientPlayer(discMsg.players);
            Debug.Log("Player disconnect message received!");
            break;

            // player connect
            case Commands.PLAYER_JOIN:
            HandshakeMsg nhsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            ClientPlayerID = nhsMsg.player.id;
            SpawnPlayer(nhsMsg.player);
            break;

            default:
            Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        
        for (int i = 0; i < ClientPlayerList.Count; i++)
        {
            if (ClientPlayerList[i].pid == ClientPlayerID)
            {
                ClientPlayerList[i].isConnected = false;
            }
        }

        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);

    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }


    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();
    
        // if no connection, stop
        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }
            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }



    // Spawn player
    private void SpawnPlayer(NetworkObjects.NetworkPlayer newPlayer)
    {
        // if passed id is self then skip
        foreach (PlayerController p in ClientPlayerList)
        {
            if (p.pid == newPlayer.id)
            {
                return;
            }
        }

        // instantiate network player on this client
        PlayerController localP = Instantiate(localCube, newPlayer.cubPos, Quaternion.identity).GetComponent<PlayerController>();
        
        localP.pid = newPlayer.id;
        localP.transform.position = newPlayer.cubPos;
        localP.GetComponent<Renderer>().material.color = newPlayer.cubeColor;

        // Add to local player list
        ClientPlayerList.Add(localP);
    }


    
    private void ClientPlayerUpdate(NetworkObjects.NetworkPlayer[] clientplayers)
    {
        // update position and color for all players
        for (int i = 0; i < clientplayers.Length; i++)
        {
            foreach (PlayerController p in ClientPlayerList)
            {
                if (p.pid == clientplayers[i].id)
                {
                    p.transform.position = clientplayers[i].cubPos;
                    p.isConnected = clientplayers[i].isConnected;
                    p.GetComponent<Renderer>().material.color = clientplayers[i].cubeColor;
                }
                Debug.Log("pid: " + p.pid + ", Position: " + p.transform.position);
            }
        }
    }


    public void UpdatePlayer(GameObject GO)
    {
        // create new player controller script
        PlayerController p = GO.GetComponent<PlayerController>();

        
        if(p != null)
        {
            ServerUpdateMsg serverUpdateMsg = new ServerUpdateMsg();
            serverUpdateMsg.players.id = p.pid;
            serverUpdateMsg.players.cubPos = p.transform.position;
            serverUpdateMsg.players.isConnected = p.isConnected;


            SendToServer(JsonUtility.ToJson(serverUpdateMsg));
        }
    }

    
    private void RemoveClientPlayer(NetworkObjects.NetworkPlayer[] clientplayers)
    {
        foreach (NetworkObjects.NetworkPlayer np in clientplayers)
        {
            for (int i = 0; i < ClientPlayerList.Count; i++)
            {
                if (np.id == ClientPlayerList[i].pid)
                {
                    Destroy(ClientPlayerList[i].gameObject);
                    ClientPlayerList.RemoveAt(i--);
                }
            }            
        }
    }


}