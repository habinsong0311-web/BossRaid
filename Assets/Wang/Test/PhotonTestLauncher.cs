using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Wang.Test
{
    public sealed class PhotonTestLauncher : MonoBehaviourPunCallbacks
    {
        public const string PrefabPath = "Player";

        [SerializeField] private string roomName = "Wang-Player-Sync-Test";
        [SerializeField] private string region = "kr";
        [SerializeField] private Camera waitingCamera;
        private string status = "Connecting...";

        private void Start()
        {
            Application.runInBackground = true;
            Connect();
        }

        private void Connect()
        {
            if (PhotonNetwork.PhotonServerSettings == null ||
                string.IsNullOrWhiteSpace(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
            {
                status = "Set the PUN App ID in PhotonServerSettings.";
                return;
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.NickName = "Player-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            PhotonNetwork.AuthValues = new AuthenticationValues(Guid.NewGuid().ToString("N"));
            AppSettings settings = PhotonNetwork.PhotonServerSettings.AppSettings.CopyTo(new AppSettings());
            settings.AppVersion = "wang-skills-test-v4";
            settings.FixedRegion = region;
            status = "Connecting...";
            if (!PhotonNetwork.ConnectUsingSettings(settings))
                status = "Connection could not start. See Console.";
        }

        public override void OnConnectedToMaster()
        {
            status = "Joining room...";
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            int slot = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % 4;
            Vector3 spawn = new Vector3((slot % 2) * 3f - 1.5f, 0.2f, (slot / 2) * 3f);
            PhotonNetwork.Instantiate(PrefabPath, spawn, Quaternion.identity);
            waitingCamera.gameObject.SetActive(false);
            status = "Connected";
            Debug.Log($"[Wang Photon Test] Joined {PhotonNetwork.CurrentRoom.Name}, actor {PhotonNetwork.LocalPlayer.ActorNumber}.");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            status = $"Join failed ({returnCode}): {message}";
            Debug.LogWarning("[Wang Photon Test] " + status);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            status = $"Create failed ({returnCode}): {message}";
            Debug.LogWarning("[Wang Photon Test] " + status);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            status = "Disconnected: " + cause;
            if (waitingCamera != null)
                waitingCamera.gameObject.SetActive(true);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 480, 180), GUI.skin.box);
            GUILayout.Label("Wang / Photon Multiplayer Test");
            GUILayout.Label(status + " / " + PhotonNetwork.NetworkClientState);
            if (PhotonNetwork.InRoom)
            {
                GUILayout.Label($"Room: {PhotonNetwork.CurrentRoom.Name} | Players: {PhotonNetwork.CurrentRoom.PlayerCount}/4");
                GUILayout.Label($"You: {PhotonNetwork.NickName} | Ping: {PhotonNetwork.GetPing()} ms");
            }
            GUILayout.Label("WASD: Move | Shift: Run | Mouse: Look");
            GUILayout.Label("1: Sword Wave | 2: Blade Dance | 3: Slash | Q: Lightning Bolt");
            GUILayout.Label("E: Aim Lightning | E again: Cast (blue circle only)");
            GUILayout.Label("Position / rotation + Animator parameter synchronization.");
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected && GUILayout.Button("Reconnect"))
                Connect();
            GUILayout.EndArea();
        }
    }
}
