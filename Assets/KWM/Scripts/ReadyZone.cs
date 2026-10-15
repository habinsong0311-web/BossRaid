using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ReadyZone : MonoBehaviourPunCallbacks
{
    [Header("시작(출발) 버튼 UI 연결")]
    [SerializeField] private Button startButton; // 방장(Master)에게만 보일 스타트 버튼

    [Header("인게임/배틀 씬 이름")]
    [SerializeField] private string battleSceneName = "BattleScene"; // 전환할 다음 씬 이름

    private int currentReadyCount = 0; // 현재 구역 안에 들어온 인원수 (방장만 연산)

    void Start()
    {
        // 🌟 초기 세팅: 스타트 버튼은 오직 '방장(MasterClient)' 화면에서만 제어할 수 있게 초기화
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false); // 시작할 때는 꺼둠
            startButton.onClick.AddListener(OnClickStartGame);
        }
    }

    // 플레이어가 레디존(트리거 콜라이더)에 들어왔을 때 실행
    private void OnTriggerEnter(Collider other)
    {
        // 🌟 핵심 철학: 연산은 오직 마스터 클라이언트(방장) 컴퓨터에서만 수행합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        // 들어온 오브젝트가 네트워크 플레이어 캐릭터인지 검사 (Tag가 Player이거나 PhotonView가 있는 경우)
        if (other.CompareTag("Player") || other.GetComponent<PhotonView>() != null)
        {
            currentReadyCount++;

            // 포톤 서버에서 현재 방의 실제 인원수를 실시간으로 가져옴 (혼자면 1명, 3명이면 3명)
            int requiredPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            Debug.Log($"[레디존] 플레이어 진입! 현재 준비 인원: {currentReadyCount} / 필요 인원(현재 방 인원): {requiredPlayers}");

            CheckReadyConditions();
        }
    }

    // 플레이어가 레디존에서 나갔을 때 실행
    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Player") || other.GetComponent<PhotonView>() != null)
        {
            currentReadyCount--;

            // 음수 방어 코드 (혹시 모를 카운트 버그 방지)
            if (currentReadyCount < 0) currentReadyCount = 0;

            int requiredPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            Debug.Log($"[레디존] 플레이어 이탈... 현재 준비 인원: {currentReadyCount} / 필요 인원(현재 방 인원): {requiredPlayers}");

            CheckReadyConditions();
        }
    }

    // 인원이 다 찼는지 검사하고 방장 버튼을 제어하는 함수
    private void CheckReadyConditions()
    {
        // 포톤 서버가 실시간으로 인식하는 현재 방의 총 플레이어 수
        int requiredCount = PhotonNetwork.CurrentRoom.PlayerCount;

        // 현재 구역 안에 들어온 인원이 현재 방 인원과 같거나 많아지면 조건 충족
        if (currentReadyCount >= requiredCount)
        {
            Debug.Log($"[레디존] 현재 방 인원 {requiredCount}명 전원 진입 완료! 방장 출발 버튼을 활성화합니다.");
            startButton.gameObject.SetActive(true); // 방장 화면에 스타트 버튼 켜기
        }
        else
        {
            startButton.gameObject.SetActive(false); // 인원 미달 시 버튼 다시 끄기
        }
    }

    // [스타트 버튼용 함수] 방장이 버튼을 누르면 모든 유저를 강제로 다음 씬으로 끌고 갑니다.
    private void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[레디존] 방장이 출발을 눌렀습니다. {battleSceneName} 씬으로 방 인원 전원 강제 이동합니다.");

            // 🌟 포톤 자동 씬 동기화 전환: 방장이 이 함수로 씬을 넘기면, 
            // 같은 방에 연결되어 있는 모든 클라이언트가 수동 조작 없이 자동으로 같이 로딩됩니다.
            PhotonNetwork.LoadLevel(battleSceneName);
        }
    }
}
