using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    // network driver
    public NetworkDriver m_Driver;

    // server port
    public ushort serverPort;

    // connection list
    private NativeList<NetworkConnection> m_Connections;

    // server player list
    public List<NetworkObjects.NetworkPlayer> ServerPlayersList = new List<NetworkObjects.NetworkPlayer>();




    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        



        // repeat broadcast to all clients
        InvokeRepeating("SendToClient", 1, 1.0f/60.0f);
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }


    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }



    void OnConnect(NetworkConnection c){
        Debug.Log("Accepted a connection");

        // Example to send a handshake message:
        HandshakeMsg hsm = new HandshakeMsg();
        hsm.player.id = c.InternalId.ToString();


        // send all connection to client
        foreach (NetworkConnection con in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(hsm), con);
        }

        ClientListMsg Clist = new ClientListMsg(ServerPlayersList);
        SendToClient(JsonUtility.ToJson(Clist), c);

        // send new player info
        hsm.cmd = Commands.PLAYER_JOIN;
        SendToClient(JsonUtility.ToJson(hsm), c);

        // add to server player list and connection list
        ServerPlayersList.Add(hsm.player);
        m_Connections.Add(c);


        print("User ID: " + hsm.player.id + " Connected!");
        

    }



    void OnData(DataStreamReader stream, int i){

        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);



        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            ServerProcessMSG(suMsg);
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }






    void OnDisconnect(int i){
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                for (int p = 0; p < ServerPlayersList.Count; p++)
                {
                    m_Connections.RemoveAtSwapBack(i);
                }
                i-=1;
            }
        }


        // players disconnect
        List<NetworkObjects.NetworkPlayer> DisconnectedPlayers = new List<NetworkObjects.NetworkPlayer>();
        for (int i = 0; i < ServerPlayersList.Count; i++)
        {
            if (ServerPlayersList[i].isConnected == false)
            {
                NetworkObjects.NetworkPlayer disc = ServerPlayersList[i];
                ServerPlayersList.RemoveAt(i-1);
                DisconnectedPlayers.Add(disc);
                    
            }

        }

        // Inform clients about disconnected players
        if (DisconnectedPlayers.Count >= 1 )
        {
            DiscPlayerMsg discMsg = new DiscPlayerMsg(DisconnectedPlayers);
            for (int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(discMsg), m_Connections[i]);
            }
        }









        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            
            // on client sent and destroy
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
    
    void SendToClient()
    {

        PlayerUpdateMsg playerUpdateMsg = new PlayerUpdateMsg(ServerPlayersList);

        foreach (NetworkConnection c in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(playerUpdateMsg), c);
        }

        // Debug.Log("Broadcast to all clients...");

    }
    

    // replicate client into to server
    void ServerProcessMSG(ServerUpdateMsg sMsg){

        foreach (NetworkObjects.NetworkPlayer player in ServerPlayersList)
        {
            if (player.id == sMsg.players.id )
            {
                player.cubeColor = sMsg.players.cubeColor;
                player.cubPos = sMsg.players.cubPos;
                
                Debug.Log("---ID: " + player.id + " POS: " + player.cubPos + " Color: " + player.cubeColor);
                
            }
        }
    }










}
