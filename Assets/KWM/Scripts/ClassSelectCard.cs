using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ClassSelectCard : MonoBehaviour
{
    [Header("클래스(직업) 고유 데이터")]
    [SerializeField] private int classIndex;
    [SerializeField] private string className;
    [SerializeField] private int attackPower;
    [SerializeField] private int maxHp;
    [SerializeField] private int maxMp;

    [Header("UI 컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    private bool isSelected = false;

    void Start()
    {
        nameText.text = className;
        
        statsText.text = $"Power : {attackPower} | HP : {maxHp} / MP : {maxMp}";

        Hashtable props = new Hashtable { { "ClassType", -1 } };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateButtonUI();
    }


    public void OnClickSelect()
    {
        PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ClassType", out object value);
        if((int)value == -1 || (int)value == classIndex)
        {
            isSelected = !isSelected;

            if (isSelected)
            {
                Hashtable props = new Hashtable { { "ClassType", classIndex } };
            
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
                Debug.Log($"[로비] {className} 클래스 선택! 장부 저장 완료.");
            }
            else
            {
                Hashtable props = new Hashtable { { "ClassType", -1 } };
            
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
                Debug.Log($"[로비] {className} 클래스 선택 취소.");
            }

            UpdateButtonUI();
        }
    }

    private void UpdateButtonUI()
    {
        if (isSelected)
        {
            buttonText.text = "Selected";
            
            selectButton.image.color = Color.red;
        }
        else
        {
            buttonText.text = "Select";
            
            selectButton.image.color = Color.black;
        }
    }
}
