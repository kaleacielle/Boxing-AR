using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    public static string latestPose = "";

    public static bool bodyDetected = false;

    // Head
    public static Vector2 head;

    // Upper Body
    public static Vector2 leftShoulder;
    public static Vector2 rightShoulder;

    public static Vector2 leftElbow;
    public static Vector2 rightElbow;

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

            latestPose = msg;

            string[] values = msg.Split(',');

            // Head + Shoulders + Elbows + Wrists
            if (values.Length == 14)
            {
                head = new Vector2(
                    float.Parse(values[0]),
                    float.Parse(values[1])
                );

                leftShoulder = new Vector2(
                    float.Parse(values[2]),
                    float.Parse(values[3])
                );

                rightShoulder = new Vector2(
                    float.Parse(values[4]),
                    float.Parse(values[5])
                );

                leftElbow = new Vector2(
                    float.Parse(values[6]),
                    float.Parse(values[7])
                );

                rightElbow = new Vector2(
                    float.Parse(values[8]),
                    float.Parse(values[9])
                );

                leftWrist = new Vector2(
                    float.Parse(values[10]),
                    float.Parse(values[11])
                );

                rightWrist = new Vector2(
                    float.Parse(values[12]),
                    float.Parse(values[13])
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