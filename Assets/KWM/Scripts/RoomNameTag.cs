using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomNameTag : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]

    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text buttonText;

    private string targetRoomName;


    public void SetRoomInfo(RoomInfo info)
    {
        targetRoomName = info.Name;

        roomNameText.text = info.Name;

        if (info.PlayerCount <= 0)
        {
            // 0명인 방은 생성되지 않도록 설정.
            this.gameObject.SetActive(false); return;
        }

        playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";

        if (info.PlayerCount >= info.MaxPlayers || !info.IsOpen)
        {
            joinButton.interactable = false;
            
            buttonText.text = "Closed";
        }
        else
        {
            joinButton.interactable = true;
            
            buttonText.text = "Join";
        }
    }


    public void OnClickJoinRoom()
    {
        if (string.IsNullOrEmpty(targetRoomName)) return;

        Debug.Log($"[RoomNameTag] {targetRoomName} 번방으로 입장을 요청합니다.");

        PhotonNetwork.JoinRoom(targetRoomName);
    }
}
