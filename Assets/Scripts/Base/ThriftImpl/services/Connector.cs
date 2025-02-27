using System;
using System.Threading;
using UnityEngine;

/**
 * @author: Ken
 *
 */

namespace thrift.services
{
    public abstract class Connector
    {
        public static string DEFAULT_THRIFT_HOST = "localhost";
        public static int DEFAULT_THRIFT_PORT = 9095;

        public static int SLEEP_TIME = 1000;

        /*
     * = isStarted for Servers
     */
        private bool connected;
        private ConnectionListener connectionListener;
        private string host;
        private bool onConnection;
        private int port;
        private Thread starterTh;

        public Connector() : this(DEFAULT_THRIFT_HOST, DEFAULT_THRIFT_PORT)
        {
        }

        public Connector(string host, int port)
        {
            this.host = host;
            this.port = port;
            connected = false;
            onConnection = false;
        }

        ~Connector()
        {
            stopConnector();
        }

        public string getHost()
        {
            return host;
        }

        public string getPortString()
        {
            return Convert.ToString(port);
        }

        public int getPort()
        {
            return port;
        }

        public void setHost(string host)
        {
            this.host = host;
            stopConnector();
            //startConnection ();
        }

        public void setPort(int port)
        {
            this.port = port;
            stopConnector();
            //startConnection ();
        }

        public void setPort(string port)
        {
            this.port = Convert.ToInt16(port);
            stopConnector();
            //startConnection ();
        }

        public void setURL(string host, string port)
        {
            setHost(host);
            setPort(port);
            stopConnector();
            //startConnection ();
        }

        public void setURL(string host, int port)
        {
            setHost(host);
            setPort(port);
            stopConnector();
            //startConnection ();
        }

        public void setConnectionLisnter(ConnectionListener connectionListener)
        {
            this.connectionListener = connectionListener;
        }

        public void setConnected(bool isConnected)
        {
            connected = isConnected;
            if (connectionListener != null)
            {
                if (connected)
                    connectionListener.onConnection();
                else
                    connectionListener.onDisconnection();
            }
        }

        public void startConnection()
        {
            //Debug.Log ("in StartConnection");
            if (isConnected())
            {
                setConnected(false);
                stopConnector();
            }

            onConnection = true;
            starterTh = new Thread(connectionStarting);
            starterTh.IsBackground = true;
            starterTh.Start();
        }

        public void connectionStarting()
        {
            var cpt = 1;
            while (!isConnected() && Thread.CurrentThread == starterTh)
            {
                //Debug.Log ("Try to start connection on " + this.getHost () + " - " + this.getPort () + " " + cpt);
                cpt++;
                startConnector();
                if (!isConnected())
                    try
                    {
                        Thread.Sleep(SLEEP_TIME);
                    }
                    catch (Exception ex1)
                    {
                        Debug.LogError(ex1);
                    }
                else
                    onConnection = false;
            }
        }

        public abstract void startConnector();

        public abstract void stopConnector();

        public bool isConnected()
        {
            return connected;
        }

        public bool isOnConnection()
        {
            return onConnection;
        }

        public void setConnectionListener(ConnectionListener connectionListener)
        {
            this.connectionListener = connectionListener;
        }
    }
}