using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using extOSC;

public class MDNSManager : MonoBehaviour
{
    [System.Serializable]
    public class MDNSDevice
    {
        public string label;            // 識別用の名前（Brainなど）
        public string hostName;         // mDNS名（brain.localなど）
        public OSCTransmitter transmitter; // 割り当てるTransmitter
    }

    [Header("Device Settings")]
    [SerializeField] private List<MDNSDevice> devices = new List<MDNSDevice>();

    [Header("Retry Settings")]
    [SerializeField] private int maxRetries = 5;
    [SerializeField] private float retryInterval = 2.0f;

    void Start()
    {
        // 起動時に全デバイスのIP解決を開始
        foreach (var device in devices)
        {
            StartCoroutine(ResolveRoutine(device));
        }
    }

    private IEnumerator ResolveRoutine(MDNSDevice device)
    {
        if (device.transmitter == null) yield break;

        int attempts = 0;
        bool resolved = false;

        while (!resolved && attempts < maxRetries)
        {
            attempts++;
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(device.hostName);
                foreach (var address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        device.transmitter.RemoteHost = address.ToString();
                        Debug.Log($"<color=green>SUCCESS:</color> {device.label} ({device.hostName}) -> {address}");
                        resolved = true;
                        break;
                    }
                }
            }
            catch
            {
                Debug.LogWarning($"<color=yellow>RETRYING ({attempts}/{maxRetries}):</color> {device.hostName} not found yet...");
            }

            if (!resolved) yield return new WaitForSeconds(retryInterval);
        }

        if (!resolved)
        {
            Debug.LogError($"<color=red>FAILED:</color> Could not resolve {device.hostName} after {maxRetries} attempts.");
        }
    }
}
