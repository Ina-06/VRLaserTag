using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace VRLaserTag.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class LanSessionController : MonoBehaviour
    {
        [SerializeField] private string hostAddress = "192.168.1.100";
        [SerializeField] private ushort port = 7777;

        private NetworkManager manager;
        private UnityTransport transport;
        private bool starting;
        private bool requestedStop;
        private bool connectedToHost;
        private bool transportFailed;

        public string HostAddress { get => hostAddress; set => hostAddress = value ?? ""; }
        public ushort Port => port;
        public string Status { get; private set; } = "Offline. Choose Host or enter the host's IP and Join.";
        public string LocalAddresses { get; private set; } = "See this device's Wi-Fi settings.";
        public bool CanStart => manager != null && !starting && !manager.IsListening && !manager.ShutdownInProgress;
        public bool CanStop => manager != null && (starting || manager.IsListening) && !manager.ShutdownInProgress;
        public bool IsHost => manager != null && manager.IsHost;
        public int ConnectedCount => IsHost ? manager.ConnectedClientsIds.Count : 0;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            manager.OnConnectionEvent += OnConnectionEvent;
            manager.OnClientStopped += OnClientStopped;
            manager.OnTransportFailure += OnTransportFailure;
            if (manager.NetworkConfig.ConnectionApproval) manager.ConnectionApprovalCallback = ApproveConnection;
            Application.runInBackground = true;
            LocalAddresses = FindLocalAddresses();
        }

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Optional development-build launch controls for repeatable two-process tests.
            string[] args = Environment.GetCommandLineArgs();
            int portIndex = Array.IndexOf(args, "-lan-port");
            if (portIndex >= 0 && portIndex + 1 < args.Length &&
                ushort.TryParse(args[portIndex + 1], out ushort selectedPort) && selectedPort > 0)
                port = selectedPort;
            int joinIndex = Array.IndexOf(args, "-lan-join");
            if (Array.IndexOf(args, "-lan-host") >= 0) StartHost();
            else if (joinIndex >= 0 && joinIndex + 1 < args.Length)
            {
                HostAddress = args[joinIndex + 1];
                JoinHost();
            }
#endif
        }

        public void StartHost()
        {
            if (!PrepareStart()) return;
            // Loopback is the host's own client address; all interfaces accept LAN clients.
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            SetStatus("Starting host...");
            try
            {
                if (!manager.StartHost()) FailStart("Could not start host. Check that the port is free.");
                else
                {
                    starting = false;
                    SetStatus($"Hosting on UDP {port}. Share your LAN IP with the other player.");
                }
            }
            catch (Exception exception) { FailStart("Host start failed: " + exception.Message); }
        }

        public void JoinHost()
        {
            if (!CanStart) return;
            if (!TryParseAddress(hostAddress, out string address))
            {
                SetStatus("Enter a valid host IPv4 address, for example 192.168.1.20.");
                return;
            }
            if (!PrepareStart()) return;
            hostAddress = address;
            transport.SetConnectionData(address, port);
            SetStatus($"Connecting to {address}:{port}...");
            try
            {
                if (!manager.StartClient()) FailStart("Could not start client. Check the network settings.");
            }
            catch (Exception exception) { FailStart("Join failed: " + exception.Message); }
        }

        public void StopSession()
        {
            if (!CanStop) return;
            requestedStop = true;
            starting = false;
            SetStatus("Stopping...");
            manager.Shutdown();
        }

        private bool PrepareStart()
        {
            if (!CanStart) return false;
            if (port == 0)
            {
                SetStatus("Choose a port between 1 and 65535 in the Inspector.");
                return false;
            }
            requestedStop = false;
            connectedToHost = false;
            transportFailed = false;
            starting = true;
            return true;
        }

        private void FailStart(string message)
        {
            starting = false;
            transportFailed = true;
            SetStatus(message);
            if (manager.IsListening) manager.Shutdown();
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = request.ClientNetworkId == NetworkManager.ServerClientId ||
                manager.ConnectedClientsIds.Count < 5;
            response.CreatePlayerObject = response.Approved && manager.NetworkConfig.PlayerPrefab != null;
            if (!response.Approved) response.Reason = "This match already has five players.";
        }

        private void OnConnectionEvent(NetworkManager source, ConnectionEventData data)
        {
            Debug.Log($"[LAN] {data.EventType}: client {data.ClientId}");
            if (data.EventType != ConnectionEvent.ClientConnected || source.IsServer) return;
            connectedToHost = true;
            starting = false;
            SetStatus($"Connected to host {hostAddress}:{port}. Client ID: {data.ClientId}.");
        }

        private void OnClientStopped(bool wasHost)
        {
            starting = false;
            if (transportFailed) return;
            if (requestedStop) SetStatus("Offline. Session stopped.");
            else if (wasHost) SetStatus("Host stopped.");
            else if (connectedToHost) SetStatus("Host left or connection lost. Session ended.");
            else if (!string.IsNullOrEmpty(manager.DisconnectReason)) SetStatus(manager.DisconnectReason);
            else SetStatus("Could not connect. Check host IP, Wi-Fi, and firewall, then try again.");
            connectedToHost = false;
        }

        private void OnTransportFailure()
        {
            starting = false;
            transportFailed = true;
            SetStatus("Network transport failed. Check Wi-Fi and port availability, then try again.");
        }

        private void SetStatus(string message)
        {
            Status = message;
            Debug.Log("[LAN] " + message);
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnConnectionEvent -= OnConnectionEvent;
            manager.OnClientStopped -= OnClientStopped;
            manager.OnTransportFailure -= OnTransportFailure;
        }

        public static bool TryParseAddress(string value, out string address)
        {
            address = null;
            string candidate = value?.Trim();
            if (string.IsNullOrEmpty(candidate)) return false;
            string[] parts = candidate.Split('.');
            if (parts.Length != 4) return false;
            byte[] bytes = new byte[4];
            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index];
                if (part.Length == 0 || part.Length > 3) return false;
                foreach (char character in part) if (character < '0' || character > '9') return false;
                if (!byte.TryParse(part, out bytes[index])) return false;
            }
            if (bytes[0] == 0 || bytes[0] >= 224) return false;
            address = new IPAddress(bytes).ToString();
            return true;
        }

        private static string FindLocalAddresses()
        {
            try
            {
                var addresses = Array.FindAll(Dns.GetHostAddresses(Dns.GetHostName()),
                    address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address));
                if (addresses.Length > 0) return string.Join(", ", Array.ConvertAll(addresses, address => address.ToString()));
            }
            catch (SocketException) { }
            return "See this device's Wi-Fi settings.";
        }
    }
}
