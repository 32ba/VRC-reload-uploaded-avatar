using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic; // For List<byte>

namespace ReloadAvatarHook
{
    public static class OscSender
    {
        private const string OSC_ADDRESS = "/avatar/change";

        /// <summary>
        /// Sends an OSC message with the avatar blueprint ID.
        /// Reads IP and Port from ReloadHookSettings.
        /// </summary>
        /// <param name="blueprintId">The blueprint ID to send.</param>
        public static void SendOscAvatarChangeMessage(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId))
            {
                Debug.LogWarning("[VRC Reload Avatar Hook] Blueprint ID is empty, skipping OSC message.");
                return;
            }

            Debug.Log($"[VRC Reload Avatar Hook] Sending OSC message for Blueprint ID: {blueprintId}");

            try
            {
                // Get settings dynamically
                string targetIp = ReloadHookSettings.OscIpAddress;
                int targetPort = ReloadHookSettings.OscPort;

                using (var udpClient = new UdpClient())
                {
                    byte[] messageBytes = FormatOscMessage(OSC_ADDRESS, blueprintId);
                    udpClient.Send(messageBytes, messageBytes.Length, targetIp, targetPort);
                    Debug.Log($"[VRC Reload Avatar Hook] OSC message sent to {targetIp}:{targetPort} with ID: {blueprintId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VRC Reload Avatar Hook] Failed to send OSC message: {e.Message}");
            }
        }

        private static byte[] FormatOscMessage(string address, string argument)
        {
            List<byte> message = new List<byte>();

            // Address Pattern
            byte[] addressBytes = Encoding.ASCII.GetBytes(address);
            message.AddRange(addressBytes);
            message.Add(0); // Null terminator
            PadToMultipleOf4(message);

            // Type Tag String
            byte[] typeTagBytes = Encoding.ASCII.GetBytes(",s");
            message.AddRange(typeTagBytes);
            message.Add(0); // Null terminator
            PadToMultipleOf4(message);

            // String Argument
            byte[] argumentBytes = Encoding.UTF8.GetBytes(argument);
            message.AddRange(argumentBytes);
            message.Add(0); // Null terminator
            PadToMultipleOf4(message);

            return message.ToArray();
        }

        private static void PadToMultipleOf4(List<byte> data)
        {
            int remainder = data.Count % 4;
            if (remainder > 0)
            {
                int padding = 4 - remainder;
                for (int i = 0; i < padding; i++)
                {
                    data.Add(0);
                }
            }
        }
    }
}
