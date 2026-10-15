using UnityEngine;
using Photon.Pun;

public class SimpleMoveTest : MonoBehaviourPun
{
    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        // 🌟 중요: 포톤 네트워크상에서 '내 캐릭터(분신)'일 때만 조작 가능하게 차단
        if (photonView.IsMine == false) return;

        // 키보드 W, A, S, D 또는 방향키 입력 받기
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new Vector3(h, 0, v).normalized;

        // 매 프레임마다 입력된 방향으로 큐브를 이동시킴
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }
}
