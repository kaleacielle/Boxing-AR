using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    public static string latestPose = "";
    public static bool bodyDetected = false;

    public static Vector2 head;
    public static Vector2 leftShoulder;
    public static Vector2 rightShoulder;
    public static Vector2 leftElbow;
    public static Vector2 rightElbow;
    public static Vector2 leftWrist;
    public static Vector2 rightWrist;

    public int listenPort = 5052;
    public bool logVerbose = true;

    private static UDPReceiver instance;
    private UdpClient client;
    private bool isListening;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartListening();
    }

    public void StartListening()
    {
        if (isListening)
            return;

        try
        {
            client = new UdpClient(listenPort);
            client.Client.ReceiveTimeout = 2000;
            client.BeginReceive(ReceiveCallback, null);
            isListening = true;
            Debug.Log($"[UDPReceiver] Listening on UDP port {listenPort}.");
        }
        catch (SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.LogWarning($"[UDPReceiver] Port {listenPort} is already in use. Attempting to recover from a stale tracker instance.");
                if (TryRecoverPortConflict(listenPort))
                {
                    StartListening();
                    return;
                }
            }

            Debug.LogError($"[UDPReceiver] Could not bind UDP port {listenPort}: {socketException.Message}. This usually means another tracker instance is still running or a stale process is holding the port.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UDPReceiver] UDP listening failed: {exception.Message}");
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        if (client == null || !isListening)
            return;

        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);
            byte[] data = client.EndReceive(ar, ref ep);

            if (data == null || data.Length == 0)
            {
                latestPose = "";
                bodyDetected = false;
                client.BeginReceive(ReceiveCallback, null);
                return;
            }

            string msg = Encoding.UTF8.GetString(data);
            latestPose = msg;

            if (logVerbose)
                Debug.Log($"[UDPReceiver] Pose packet received: {msg}");

            string[] values = msg.Split(',');
            if (values.Length == 14)
            {
                float x1, y1, x2, y2, x3, y3, x4, y4, x5, y5, x6, y6, x7, y7;
                if (TryParse(values[0], out x1) &&
                    TryParse(values[1], out y1) &&
                    TryParse(values[2], out x2) &&
                    TryParse(values[3], out y2) &&
                    TryParse(values[4], out x3) &&
                    TryParse(values[5], out y3) &&
                    TryParse(values[6], out x4) &&
                    TryParse(values[7], out y4) &&
                    TryParse(values[8], out x5) &&
                    TryParse(values[9], out y5) &&
                    TryParse(values[10], out x6) &&
                    TryParse(values[11], out y6) &&
                    TryParse(values[12], out x7) &&
                    TryParse(values[13], out y7))
                {
                    head = new Vector2(x1, y1);
                    leftShoulder = new Vector2(x2, y2);
                    rightShoulder = new Vector2(x3, y3);
                    leftElbow = new Vector2(x4, y4);
                    rightElbow = new Vector2(x5, y5);
                    leftWrist = new Vector2(x6, y6);
                    rightWrist = new Vector2(x7, y7);

                    bodyDetected = true;
                    Debug.Log("[UDPReceiver] Body pose valid and parsed successfully.");
                }
                else
                {
                    bodyDetected = false;
                    Debug.LogWarning("[UDPReceiver] Pose packet was malformed and could not be parsed.");
                }
            }
            else
            {
                bodyDetected = false;
                Debug.LogWarning($"[UDPReceiver] Unexpected pose packet length: {values.Length}");
            }

            client.BeginReceive(ReceiveCallback, null);
        }
        catch (ObjectDisposedException)
        {
            Debug.LogWarning("[UDPReceiver] UDP socket closed while receiving.");
        }
        catch (SocketException socketException)
        {
            Debug.LogWarning($"[UDPReceiver] UDP receive timeout/socket issue: {socketException.Message}");
            if (client != null)
                client.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UDPReceiver] Receive callback failed: {exception.Message}");
        }
    }

    private bool TryRecoverPortConflict(int port)
    {
        try
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string command = $"netstat -ano | findstr :{port}";
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + command,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error))
                        return false;

                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (!line.Contains($":{port}"))
                            continue;

                        string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5)
                            continue;

                        string pidText = parts[parts.Length - 1];
                        if (int.TryParse(pidText, out int pid))
                        {
                            using (var kill = new System.Diagnostics.Process())
                            {
                                kill.StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "taskkill",
                                    Arguments = $"/PID {pid} /F",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true
                                };

                                kill.Start();
                                kill.WaitForExit();
                                Debug.Log($"[UDPReceiver] Killed stale process holding UDP port {port} (PID {pid}).");
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                string command = $"lsof -nP -iUDP:{port} || fuser -k {port}/udp";
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = "-lc \"" + command + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
                    {
                        Debug.Log($"[UDPReceiver] Cleared stale port holder on UDP {port}.");
                        return true;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[UDPReceiver] Recovery attempt failed: {exception.Message}");
        }

        return false;
    }

    private bool TryParse(string value, out float parsed)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed);
    }

    private void OnApplicationQuit()
    {
        StopListening();
    }

    private void OnDestroy()
    {
        StopListening();
    }

    private void StopListening()
    {
        isListening = false;
        if (client != null)
        {
            try
            {
                client.Close();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[UDPReceiver] Could not close UDP socket cleanly: {exception.Message}");
            }
            finally
            {
                client = null;
            }
        }
    }
}