using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    // Used later for pose matching
    public static string latestPose = "";

    // Used now for wrist tracking
    public static Vector2 wristPosition;

    // Body detection
    public static bool bodyDetected = false;

    private UdpClient client;

    void Start()
    {
        client = new UdpClient(5052);

        Debug.Log("Listening on port 5052...");

        client.BeginReceive(ReceiveCallback, null);
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5052);

        byte[] data = client.EndReceive(ar, ref ep);

        string msg = Encoding.UTF8.GetString(data);

        // Keep the latest message
        latestPose = msg;

        // Expected format: x,y
        string[] values = msg.Split(',');

        if (values.Length == 2)
        {
            float x = float.Parse(values[0]);
            float y = float.Parse(values[1]);

            wristPosition = new Vector2(x, y);

            bodyDetected = true;
        }
        else
        {
            bodyDetected = false;
        }

        client.BeginReceive(ReceiveCallback, null);
    }

    void OnApplicationQuit()
    {
        client?.Close();
    }
}