using System;
using thrift.gen_csharp;
using Thrift.Protocol;
using Thrift.Transport;
using UnityEngine;

namespace thrift.services
{
    public class Sender : Connector
    {
        private SimpleCom.Client client;
        private TTransport transport;

        public Sender() : this(DEFAULT_THRIFT_HOST, DEFAULT_THRIFT_PORT)
        {
        }

        public Sender(string host, int port) : base(host, port)
        {
        }

        ~Sender()
        {
            if (transport != null)
            {
                Debug.Log("Closing the simple client...");
                transport.Close();
            }
        }

        public override void startConnector()
        {
            try
            {
                lock (this)
                {
                    transport = new TSocket(getHost(), getPort());
                    TProtocol protocol = new TBinaryProtocol(transport);
                    client = new SimpleCom.Client(protocol);
                    transport.Open();
                    Debug.Log("Connected to " + getHost() + " - " + getPort());
                    Debug.Log("sender to String: " + client);
                    if (client != null) setConnected(true);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception " + ex.Message);
            }
        }

        public override void stopConnector()
        {
            if (isOnConnection() || isConnected())
            {
                //	Debug.Log ("stopConnector this.transport.IsOpen " + this.transport.IsOpen);
                if (transport.IsOpen) transport.Close();

                setConnected(false);
            }

            //	Debug.Log ("stopConnector this.transport.IsOpen " + this.transport.IsOpen);
        }

        public void send(Message m)
        {
            try
            {
                lock (client)
                {
                    client.send(m);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Can not send Message. " + ex.StackTrace);
                Debug.Log("Check that the right Receiver is connected on " + getHost() + " " + getPort());
                Debug.Log("Check that there is no remaining java.exe running");
                /*  if (isConnected()) {
                startConnection();
            }*/
            }
        }

        public new void setPort(int port)
        {
            if (getPort() != port)
            {
                base.setPort(port);
                stopConnector();
                //startConnection ();
            }
        }

        public new void setPort(string port)
        {
            setPort(Convert.ToInt16(port));
        }

        public new void setHost(string host)
        {
            if (!getHost().Equals(host))
            {
                base.setHost(host);
                stopConnector();
                //startConnection ();
            }
        }
    }
}