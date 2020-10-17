using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public int speed = 15;
    public NetworkClient NWC;

    public string pid;
    // Start is called before the first frame update

    public bool isConnected = true;


    private void Awake() {
        NWC = FindObjectOfType<NetworkClient>();
    }

    void Start()
    {
        // repeat updateself to server
        InvokeRepeating("UpdateSelf", 1, 1.0f/60.0f);
    }

    // Update is called once per frame
    private void Update()
    {
        if (NWC.ClientPlayerID == pid)
        {

            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(Vector3.forward * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(-Vector3.forward * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(Vector3.left * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.D))
            {
                transform.Translate(-Vector3.left * Time.deltaTime * speed);
            }
            
        }
    }



    void UpdateSelf()
    {
        NWC.UpdatePlayer(gameObject);


    }


}

