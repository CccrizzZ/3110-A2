using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands{
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT,
        PLAYER_DISCONNECT,
        PLAYER_LIST,
        PLAYER_JOIN
    }


    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    


    // player update message
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer[] players;

        public PlayerUpdateMsg(List<NetworkObjects.NetworkPlayer> playerList){      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            players = new NetworkObjects.NetworkPlayer[playerList.Count];
        
            int i = 0;
            foreach(NetworkObjects.NetworkPlayer player in playerList)
            {
                players[i] = player;
                i++;
            }
        }
    };




    [System.Serializable]
    // player input message
    public class PlayerInputMsg:NetworkHeader{
        public Input myInput;
        public PlayerInputMsg(){
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();



        }
    }


    [System.Serializable]
    public class ClientListMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;
        public ClientListMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {
            cmd = Commands.PLAYER_LIST;
            players = new NetworkObjects.NetworkPlayer[playerList.Count];

            int i = 0;
            foreach(NetworkObjects.NetworkPlayer player in playerList)
            {
                players[i] = player;
                i++;
            }
        }
    }

    [System.Serializable]
    public class DiscPlayerMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer[] players;
        public DiscPlayerMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {
            cmd = Commands.PLAYER_DISCONNECT;
            players = new NetworkObjects.NetworkPlayer[playerList.Count];

            int i = 0;
            foreach(NetworkObjects.NetworkPlayer player in playerList)
            {
                players[i] = player;
                i++;
            }
        }
    }

    // server update message
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer players;
        public ServerUpdateMsg()
        {    
            cmd = Commands.SERVER_UPDATE;
            players = new NetworkObjects.NetworkPlayer();

        
        }
    }
} 






namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }


    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public Color cubeColor = new Color(
                Random.Range(0.0f, 1.0f), // R 
                Random.Range(0.0f, 1.0f), // G
                Random.Range(0.0f, 1.0f)  // B
            );



        public Vector3 cubPos;
        public bool isConnected = true;
        public NetworkPlayer(){
            cubPos = new Vector3(0,0,0);

            cubeColor = new Color(
                Random.Range(0.0f, 1.0f), // R 
                Random.Range(0.0f, 1.0f), // G
                Random.Range(0.0f, 1.0f)  // B
            );

        }
    }





}



