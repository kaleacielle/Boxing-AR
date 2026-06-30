using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    // Keep this so old scripts still compile
    public static string latestPose = "";

    // Body detection
    public static bool bodyDetected = false;

    // Left & Right hands
    public static Vector2 leftWrist;
    public static Vector2 rightWrist;

    private UdpClient client;

    void Start()
    {
        client = new UdpClient(5052);

        Debug.Log("Listening on port 5052...");

        client.BeginReceive(ReceiveCallback, null);
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5052);

            byte[] data = client.EndReceive(ar, ref ep);

            string msg = Encoding.UTF8.GetString(data);

            // Keep old compatibility
            latestPose = msg;

            string[] values = msg.Split(',');

            // Left Wrist X,Y + Right Wrist X,Y
            if (values.Length == 4)
            {
                leftWrist = new Vector2(
                    float.Parse(values[0]),
                    float.Parse(values[1])
                );

                rightWrist = new Vector2(
                    float.Parse(values[2]),
                    float.Parse(values[3])
                );

                bodyDetected = true;
            }
            else
            {
                bodyDetected = false;
            }

            client.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void OnApplicationQuit()
    {
        client?.Close();
    }
}