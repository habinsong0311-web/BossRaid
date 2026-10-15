using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text stateText;

    private Button button;
    private string roomName;
    private Action<string> onJoin;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Setup(RoomInfo info,Action<string> joinCallback)
    {
        roomName = info.Name;
        onJoin = joinCallback;

        nameText.text = roomName;
        countText.text = $"{info.PlayerCount} / {info.MaxPlayers}";

        if (!info.IsOpen)
            stateText.text = "Closed";
        bool isFull = info.MaxPlayers <= info.PlayerCount;
        if (isFull)
            stateText.text = "Full";

        button.interactable = info.IsOpen && !isFull;
    }

    private void OnClick()
    {
        onJoin(roomName);
    }
}
