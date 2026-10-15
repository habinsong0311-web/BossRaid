using UnityEngine;
using Photon.Pun;

public class NetworkPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("캐릭터 프리팹 리스트")]
    [SerializeField] private GameObject[] playerPrefabs;

    [Header("스폰 위치 배열")]
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        // 포톤 서버에 잘 연결되어 있고 방 안에 있다면 생성 시작
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            SpawnMyCharacter();
        }
    }


    private void SpawnMyCharacter()
    {
        int classIndex = 0;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ClassType", out object value))
        {
            classIndex = (int)value;
        }

        // 혹시 모를 에러 방지. 패킷 손실 등으로 배열 범위 초과 시 0번 캐릭터로 강제 설정.
        if (classIndex < 0 || classIndex >= playerPrefabs.Length)
        {
            Debug.LogError("[Network] 고른 캐릭터 번호가 프리팹 리스트 범위를 벗어났습니다! 0번으로 대체합니다.");

            classIndex = 0;
        }


        int myOrder = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
        
        Vector3 spawnPos = spawnPoints[myOrder].position;
        
        Quaternion spawnRot = spawnPoints[myOrder].rotation;

        
        string prefabName = playerPrefabs[classIndex].name;
        
        PhotonNetwork.Instantiate(prefabName, spawnPos, spawnRot);

        Debug.Log($"[Network] {prefabName} 캐릭터 동기화 생성 완료 (위치: {myOrder}번 스폰포인트)");
    }
}
