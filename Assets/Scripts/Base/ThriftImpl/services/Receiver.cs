using System;
using System.Threading;
using thrift.gen_csharp;
using Thrift.Server;
using Thrift.Transport;
using UnityEngine;

/**
 *
 * @author Ken
 */


namespace thrift.services
{
    public abstract class Receiver : Connector
    {
        private SimpleComHandler handler;
        private Message message;
        private SimpleCom.Processor processor;
        private Thread receiverThread;
        private TServer server;
        private TServerTransport serverTransport;

        public Receiver() : this(DEFAULT_THRIFT_PORT)
        {
        }

        public Receiver(int port) : base(DEFAULT_THRIFT_HOST, port)
        {
        }

        public override void startConnector()
        {
            try
            {
                handler = new SimpleComHandler(this);
                processor = new SimpleCom.Processor(handler);
                receiverThread = new Thread(startingSimpleServer);
                receiverThread.IsBackground = true;
                receiverThread.Start();
            }
            catch (Exception x)
            {
                Debug.LogError(x);
            }
        }

        public override void stopConnector()
        {
            if (server != null)
            {
                Debug.Log("Stoping the Receiver...");
                server.Stop();
                serverTransport.Close();
                Debug.Log("Receiver stopped");
            }

            if (receiverThread != null && receiverThread.IsAlive) receiverThread.Abort();

            setConnected(false);
        }

        public void startingSimpleServer()
        {
            Debug.Log("Try to start Receiver SimpleServer on " + getHost() + " - " + getPort());
            try
            {
                serverTransport = new TServerSocket(getPort());
                server = new TSimpleServer(processor, serverTransport);

                // Use this for a multithreaded server
                // TServer server = new TThreadPoolServer(new TThreadPoolServer.Args(serverTransport).processor(processor));

                Debug.Log("Starting the simple server...");
                setConnected(true);
                server.Serve();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        public abstract void perform(Message m);
    }
}