using ExitGames.Client.Photon.StructWrapping;
using Firebase.Auth;
using Firebase.Database;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;


public class LobbyUiManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject nicknamePanel;
    [SerializeField] private GameObject lobbyPanel;


    [Header("Nickname Setup")]
    [SerializeField] private TMP_InputField nicknameInputField;


    // 이벤트 구독 목적 : 클래스가 파이어베이스를 수시로 호출하는 것을 방지하기 위해.
    public override void OnEnable()
    {
        base.OnEnable();

        FirebaseAuthService.OnAuthChanged += HandleAuthChanged;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        FirebaseAuthService.OnAuthChanged -= HandleAuthChanged;
    }


    private async void HandleAuthChanged(FirebaseUser user)
    {
        if (user == null)
        {

            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[Lobby] 파이어베이스 로그아웃 감지 ➡️ 포톤 유령 세션 방지를 위해 연결을 해제(Disconnect)합니다.");
                
                PhotonNetwork.Disconnect();
            }


            loginPanel.SetActive(true);
            nicknamePanel.SetActive(false);
            lobbyPanel.SetActive(false);

            return;
        }


        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("Users").Child(user.UserId).Child("nickname").GetValueAsync();

        if (snapshot.Exists && !string.IsNullOrEmpty(snapshot.Value.ToString()))
        {
            string savedNickname = snapshot.Value.ToString();
            
            ConnectToPhoton(savedNickname);
        }
        else
        {
            loginPanel.SetActive(false);
            
            nicknamePanel.SetActive(true);
        }
    }


    public async void OnClickSaveNickname()
    {
        string inputName = nicknameInputField.text;

        if (string.IsNullOrEmpty(inputName)) return;

        string uid = FirebaseBootstrap.Auth.CurrentUser.UserId;

        // 파이어베이스 DB에 유저 UID 방을 파고 닉네임 저장
        await FirebaseDatabase.DefaultInstance
            .GetReference("Users").Child(uid).Child("nickname").SetValueAsync(inputName);

        nicknamePanel.SetActive(false);
        lobbyPanel.SetActive(true);

        ConnectToPhoton(inputName);
    }


    private void ConnectToPhoton(string nickName)
    {
        PhotonNetwork.NickName = nickName;
        PhotonNetwork.ConnectUsingSettings();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("포톤 마스터 서버 접속 성공");
        
        PhotonNetwork.JoinLobby();

        loginPanel.SetActive(false);
        nicknamePanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void OnClickRefreshRoomList()
    {
        Debug.Log("[Lobby] 수동 새로고침 작동. 로비 재진입을 시도합니다.");

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby(); // 로비 → 포톤 마스터 서버
        }
    }

    public override void OnLeftLobby() // 포톤 마스터 서버 → 로비
    {
        Debug.Log("마스터 서버에서 대기 중...방 목록을 불러오기 위해 대기실(Lobby) 재진입을 요청합니다.");
        
        PhotonNetwork.JoinLobby();
    }

    public void OnClickCreateRoom()
    {

        RoomOptions options = new RoomOptions();
        
        options.MaxPlayers = 4;
        options.IsOpen = true;
        options.IsVisible = true;

        string roomName = $"Room_{Random.Range(1000, 9999)}";

        Debug.Log($"[Lobby] 포톤 서버에 방 생성을 요청합니다. 방 이름: {roomName}");

        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[Lobby] 방 생성성공! 대기 공간(CampScene)으로 이동합니다.");
        
        PhotonNetwork.LoadLevel("GameScene");
    }


    [Header("실시간 방 목록 UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject roomNameTagPrefab;

    public override void OnRoomListUpdate(System.Collections.Generic.List<Photon.Realtime.RoomInfo> roomList)
    {
        // 포톤Pun2가 중복된 정보를 누적시키는 문제가 있어 필요 없는 데이터를 파괴하는데 꼭 필요함.
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 포톤 서버가 보내준 최신 데이터를 기반으로 룸 네임 태그 항목 생성.
        foreach (var room in roomList)
        {
            if (room.RemovedFromList || room.PlayerCount <= 0) continue;

            GameObject item = Instantiate(roomNameTagPrefab, contentParent);

            // 자꾸 네임태그 UI Z축이 4천 넘게 뜨는 현상 방지 코드.
            RectTransform rect = item.GetComponent<RectTransform>();
            
            if (rect != null)
            {
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            RoomNameTag tagScript = item.GetComponent<RoomNameTag>();
            
            if (tagScript != null)
            {
                tagScript.SetRoomInfo(room);
            }
        }

        Debug.Log($"[Lobby] 포톤 실시간 방 목록 동기화 완료! (현재 활성화된 방 개수: {contentParent.childCount}개)");
    }


    [Header("클래스 선택 (임시 보관함)")]

    private int currentSelectedClassIndex = -1; // 기본값

    public void SetTemporaryClassIndex(int index)
    {
        currentSelectedClassIndex = index;
        
        Debug.Log($"[Lobby] 임시 직업 선택 변경: {index}번 클래스 픽업 중...");
    }


    public void OnClickSaveClassType()
    {

        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ClassType", out object value);

        currentSelectedClassIndex = (int)value;
        // 만약 플레이어가 아무것도 안 고르고 Save를 눌렀을 때 예외 방어
        if (currentSelectedClassIndex == -1)
        {
            Debug.LogWarning("[Lobby] 직업을 먼저 선택한 후에 Save 버튼을 눌러주세요!");
            
            return;
        }

        // 포톤 서버 장부에 최종 확정 번호 등록 (Index)
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "ClassType", currentSelectedClassIndex }
        };

        //PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        OnClickCloseClassSelectPanel();

        Debug.Log($"[네트워크] 서버 장부 최종 등록 완료! 내 직업 번호: {currentSelectedClassIndex}");
    }


    [Header("클래스 선택 패널 셋업")]
    [SerializeField] private GameObject classSelectPanel;

    public void OnClickCloseClassSelectPanel()
    {
        if (classSelectPanel != null)
        {
            classSelectPanel.SetActive(false); // 클래스 선택창 끄기
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true); // 기존 로비 패널 다시 켜기
        }

        Debug.Log("[Lobby] 클래스 선택창을 닫고 로비 패널로 돌아갑니다.");
    }

    public void OnClickOpenClassSelectPanel()
    {
        if (classSelectPanel != null)
        {
            classSelectPanel.SetActive(true); // 기존 로비 패널 다시 켜기
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false); // 클래스 선택창 끄기
        }

    }
}
