using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class OscSender
{
    public static void SendOscMessage(string ip, int port, string address, params object[] values)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                byte[] oscMessage = CreateOscMessage(address, values);
                udpClient.Send(oscMessage, oscMessage.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Reload Avatar Hook] OSC Send Error: {e.Message}");
            }
        }
    }

    private static byte[] CreateOscMessage(string address, object[] values)
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
            {
                WriteOscString(writer, address);

                // タイプタグ
                string typeTag = ",";
                foreach (var value in values)
                {
                    if (value is int) typeTag += "i";
                    else if (value is float) typeTag += "f";
                    else if (value is string) typeTag += "s";
                }
                WriteOscString(writer, typeTag);

                // データ部分
                foreach (var value in values)
                {
                    if (value is int intValue) writer.Write(IPAddress.HostToNetworkOrder(intValue));
                    else if (value is float floatValue)
                    {
                        byte[] bytes = BitConverter.GetBytes(floatValue);
                        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                        writer.Write(bytes);
                    }
                    else if (value is string stringValue) WriteOscString(writer, stringValue);
                }

                return stream.ToArray();
            }
        }
    }

    private static void WriteOscString(System.IO.BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes);
        writer.Write(new byte[4 - (bytes.Length % 4)]); // 4バイト境界調整
    }
}