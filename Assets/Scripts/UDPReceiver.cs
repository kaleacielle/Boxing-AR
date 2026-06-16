using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    private UdpClient client;

    private void Start()
    {
        client = new UdpClient(5052);

        Debug.Log("Listening on port 5052...");

        client.BeginReceive(ReceiveCallback, null);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 5052);

        byte[] data = client.EndReceive(ar, ref endPoint);

        string message = Encoding.UTF8.GetString(data);

        Debug.Log("Received: " + message);

        client.BeginReceive(ReceiveCallback, null);
    }

    private void OnApplicationQuit()
    {
        client?.Close();
    }
}