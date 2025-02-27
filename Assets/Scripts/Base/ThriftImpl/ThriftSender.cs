using System;
using thrift.gen_csharp;
using thrift.services;
using UnityEngine;

public class ThriftSender : MonoBehaviour
{
    private int cpt;
    private Message message;
    private Sender sender;

    // Use this for initialization
    private void Start()
    {
        Debug.Log("Start");
        message = new Message();
        Debug.Log("new message created");
        cpt = 0;
        sender = new Sender("localhost", 9095);
        Debug.Log("new sender created");
        sender.startConnection();
        Debug.Log("sender connection started");
    }

    // Update is called once per frame
    private void Update()
    {
        if (sender.isConnected())
        {
            message.Type = "trou de balle";
            message.Time = 2;
            message.Id = Convert.ToString(cpt);
            sender.send(message);
            cpt++;
        }
    }
}