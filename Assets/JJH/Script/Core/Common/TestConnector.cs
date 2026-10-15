using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TestConnector : MonoBehaviourPunCallbacks
{
    [Header("자동 매칭 세팅")]
    [SerializeField] private string gameVersion = "1";
    [SerializeField] private byte maxPlayers = 4; 

    void Start()
    {
        // 1. 이미 포톤에 연결되어 있는 게 아니라면 마스터 서버에 접속 시작
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings(); // 마스터 서버 연결 요청
        }
    }

    // 마스터 서버 접속 성공 시 호출되는 포톤 콜백
    public override void OnConnectedToMaster()
    {
        RoomOptions roomOptions = new() { MaxPlayers = maxPlayers };
        PhotonNetwork.JoinRandomOrCreateRoom(null, 0, MatchmakingMode.FillRoom, null, null, null, roomOptions, null);
    }

    // 방에 성공적으로 들어갔을 때 호출되는 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log($"[AutoConnect] 룸 입장 완료! 현재 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}명");

        PhotonNetwork.Instantiate("Player",Vector3.zero, Quaternion.identity);
    }

    // 방 생성/입장에 실패했을 때의 예외 처리
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[AutoConnect] 랜덤 방 진입 실패 ({message}). 새 방을 강제 생성합니다.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayers });
    }
}
