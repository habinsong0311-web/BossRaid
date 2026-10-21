using Cysharp.Threading.Tasks;
using DG.Tweening;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constant_JJH;

using SF = UnityEngine.SerializeField;
public class BattleEndController : MonoBehaviour
{
    private GameManager GameManager => GameManager.m_Instance;


    #region 직렬화 필드
    [Header("스크립트")]
    [SF] private TestPortal portal;
    // 테스트용 강제 직렬화
    [SF] private DragonBoss dragon;
    [SF] SOSceneData homeScene;

    [Header("패널")]
    [SF] private CanvasGroup bttleEndPanel;

    [Header("이미지")]
    [SF] private Image winImage;
    [SF] private Image defeatImage;

    [Header("텍스트")]
    [SF] private TextMeshProUGUI timerText;

    [Header("GUI 직렬화")]
    [SF] private HorizontalLayoutGroup checkList;
    [SF] private List<Toggle> toggles;


    [Header("타이머 수치")]
    [SF] private int BattleTimerNormal = 60;
    [SF] private int BattleTimerSkip = 5;
    private int insideCount = 0;
    #endregion

    private CancellationTokenSource endTimerLoopToken;
    private List<(Toggle toggle, Player player)> toggleGroup;

    private void Awake()
    {
        bttleEndPanel.alpha = 0;
        bttleEndPanel.gameObject.SetActive(false);
        toggleGroup = new();
    }


    private void Start()
    {
        ForceConnectDragonScript(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 테스트용 드래곤 강제 연결 함수
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask ForceConnectDragonScript(CancellationToken token)
    {
        while (true)
        {
            DragonBoss bossData = FindAnyObjectByType<DragonBoss>();
            if (bossData != null)
            {
                dragon = bossData;
                break;
            }

            await UniTask.Delay(MILLISECOND_BASE, DelayType.DeltaTime,
                PlayerLoopTiming.Update, token);
        }

        
    }

    private void OnEnable()
    {
        portal.OnPlayerInPortal += PlayerReadyCheck;
        GameManager.OnAnnihilationPlayer += PanelOpenEvent;
    }

    private void OnDisable()
    {
        portal.OnPlayerInPortal -= PlayerReadyCheck;
        GameManager.OnAnnihilationPlayer -= PanelOpenEvent;
    }

    public void PanelOpenEvent()
    {
        PanelOpen().Forget();
    }

    private async UniTaskVoid PanelOpen()
    {
        bttleEndPanel.alpha = 0;
        bttleEndPanel.gameObject.SetActive(true);
        
        //상태 초기화
        toggleGroup.Clear();
        foreach (Toggle toggle in toggles)
        {
            toggle.isOn = false;
            toggle.gameObject.SetActive(false);
        }
            
        if(GameManager.GetAlivePlayerCount() > 0)
        {
            winImage.gameObject.SetActive(true);
            defeatImage.gameObject.SetActive(false);
        }
        else
        {
            winImage.gameObject.SetActive(false);
            defeatImage.gameObject.SetActive(true);
        }

        await bttleEndPanel.DOFade(1, UI_FADE_TIME).ToUniTask();

        if (GameManager.m_Instance.GetAlivePlayerCount() <= 0)
        {
            TimerStart(BattleTimerSkip);

            return;
        }

        for(int i = 0; i < GameManager.GetAlivePlayerCount(); i++)
        {
            toggles[i].gameObject.SetActive(true);
            toggleGroup.Add((toggles[i], GameManager.Players[i]));
        }
    }

    private void PlayerReadyCheck(Player player)
    {
        if (player == null) return;
        
        toggleGroup.ForEach(toggle =>
        {
            if (toggle.player == player)
            {
                if (toggle.toggle.isOn)
                {
                    toggle.toggle.isOn = false;
                    insideCount--;
                }else
                {
                    toggle.toggle.isOn = true;
                    insideCount++;
                }
            }
        });

        int requiredCount = Mathf.CeilToInt(GameManager.GetAlivePlayerCount() / 2f);
        int result = 0;
        // 모두 포탈에 들어온 경우 && 아직 포탈의 제한시간이 5초 이상인 경우
        if (insideCount == GameManager.GetAlivePlayerCount() && 
            (GameManager.GetAlivePlayerCount() == 1 || (int.TryParse(timerText.text, out result) &&
            result > BattleTimerSkip))) 
            TimerStart(BattleTimerSkip);

        // 절반 이상이 포탈에 들어온 경우
        else if (insideCount >= requiredCount)
            TimerStart(BattleTimerNormal);

        // 포탈의 남은 인원수가 절반 미만이 된 경우
        else
            TimerStop();
    }

    private void TimerStart(int time)
    {
        StopEndTimerSyncLoop();
        endTimerLoopToken = new CancellationTokenSource();
        double endTime = PhotonNetwork.Time + time;

        BattleEndTimer(endTime, endTimerLoopToken.Token).Forget();
    }

    private void TimerStop()
    {
        StopEndTimerSyncLoop();
    }

    private async UniTaskVoid BattleEndTimer(double time, CancellationToken cancellationToken)
    {
        if (timerText == null) return;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int remainingSeconds = 
                    Mathf.Max(0, Mathf.CeilToInt((float)(time - PhotonNetwork.Time)));

                timerText.SetText(remainingSeconds.ToString());

                if (remainingSeconds <= 0) break;

                await UniTask.Delay(MILLISECOND_BASE, DelayType.DeltaTime, 
                    PlayerLoopTiming.Update, cancellationToken);
            }

            GameSceneManager.m_Instance.MoveScene(homeScene);
            // 씬 이동 작동
        }
        catch (OperationCanceledException)
        {
            Debug.Log("타이머 종료");
        }
        catch (Exception ex) 
        {
            Debug.LogError($"예기치 못한 오류가 발생했습니다. : {ex.Message}");
        }
    }

    public void StopEndTimerSyncLoop()
    {
        if (endTimerLoopToken == null) return;
        endTimerLoopToken.Cancel();
        endTimerLoopToken.Dispose();
        endTimerLoopToken = null;
    }

    private void OnDestroy()
    {
        StopEndTimerSyncLoop();
    }

}
