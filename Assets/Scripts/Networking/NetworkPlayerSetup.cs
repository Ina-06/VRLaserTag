using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VRLaserTag.Networking
{
    public sealed class NetworkPlayerSetup : NetworkBehaviour
    {
        [SerializeField] private Behaviour[] ownerOnly;
        [SerializeField] private Renderer[] remoteOnly;
        [SerializeField] private CharacterController character;
        [SerializeField] private Transform head;
        [SerializeField] private CapsuleCollider body;
        [SerializeField] private Transform bodyVisual;
        private LobbyRig lobby;
        private int spawnOffset;

        public override void OnNetworkSpawn()
        {
            GetComponent<PlayerIdentity>().SetPlayerId(OwnerClientId + 1);
            if (body != null)
                for (int layer = 0; layer < 32; layer++)
                    Physics.IgnoreLayerCollision(body.gameObject.layer, layer);

            if (IsOwner)
            {
                lobby = FindAnyObjectByType<LobbyRig>();
                if (lobby != null) lobby.gameObject.SetActive(false);
                MoveToSpawn();
            }

            foreach (Behaviour component in ownerOnly)
                if (component != null) component.enabled = IsOwner;
            if (character != null) character.enabled = IsOwner;
            if (IsOwner) BindTeleportation(transform);
            foreach (Renderer renderer in remoteOnly)
                if (renderer != null) renderer.enabled = !IsOwner;
        }

        public override void OnNetworkDespawn()
        {
            foreach (Behaviour component in ownerOnly)
                if (component != null) component.enabled = false;
            if (character != null) character.enabled = false;
            if (lobby != null)
            {
                lobby.gameObject.SetActive(true);
                BindTeleportation(lobby.transform);
            }
        }

        private static void BindTeleportation(Transform rig)
        {
            var provider = rig.GetComponentInChildren<TeleportationProvider>();
            if (provider == null) return;
            foreach (var area in FindObjectsByType<BaseTeleportationInteractable>())
                area.teleportationProvider = provider;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || body == null || head == null) return;
            Vector3 headPosition = transform.InverseTransformPoint(head.position);
            float height = Mathf.Clamp(headPosition.y - 0.20f, 0.65f, 1.85f);
            body.height = height;
            body.transform.localPosition = new Vector3(headPosition.x, height * 0.5f, headPosition.z);
            if (bodyVisual != null) bodyVisual.localScale = new Vector3(body.radius * 2, height * 0.5f, body.radius * 2);
        }

        internal void RespawnOwner()
        {
            if (IsSpawned && IsServer) RespawnRpc();
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void RespawnRpc()
        {
            spawnOffset++;
            MoveToSpawn();
        }

        private void MoveToSpawn()
        {
            var spawns = FindAnyObjectByType<ArenaSpawnManager>();
            if (spawns == null || !spawns.MovePlayerToSpawn(transform, (int)(OwnerClientId % 5) + spawnOffset)) return;
            var sync = GetComponent<NetworkTransform>();
            if (sync != null && sync.CanCommitToTransform)
                sync.Teleport(transform.position, transform.rotation, transform.localScale);
        }
    }
}
