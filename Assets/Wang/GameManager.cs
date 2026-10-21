using System;
using System.Collections.Generic;
using Newtonsoft.Json.Bson;
using Photon.Pun;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public static GameManager m_Instance { get; private set; }
    public IReadOnlyList<Player> Players => m_listPlayer;

    [SerializeField] private SOSceneData m_refVillageSceneData;
    private readonly List<Player> m_listPlayer = new();

    public event Action OnAnnihilationPlayer;


    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        m_Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.SerializationRate = 20;
        PhotonNetwork.SendRate = 30;
    }


    public void OnEnterPlayer(Player _refPlayer)
    {
        if (_refPlayer == null || m_listPlayer.Contains(_refPlayer))
            return;

        m_listPlayer.Add(_refPlayer);
        _refPlayer.OnDeadEvent += OnPlayerDead;
    }

    public void OnExitPlayer(Player _refPlayer)
    {
        if (_refPlayer != null)
            _refPlayer.OnDeadEvent -= OnPlayerDead;
        m_listPlayer.Remove(_refPlayer);
    }

    private void OnPlayerDead()
    {
        if (GetAlivePlayerCount() != 0)
            return;

        OnAnnihilationPlayer?.Invoke();

        //GameSceneManager.m_Instance.MoveScene(m_refVillageSceneData);
    }

    public void RefreshPlayer(Vector3 _vSpawnPos)
    {
        foreach (Player player in m_listPlayer)
        {
            if (player != null)
                player.RefreshPlayer(_vSpawnPos);
        }
    }

    public void RefreshPlayerAll()
    {
        foreach (Player player in m_listPlayer)
        {
            if (player != null && !player.IsAlive)
                player.RefreshPlayer();
        }
    }
    
    public int GetAlivePlayerCount()
    {
        int iCount = 0;

        for(int i = 0; i<m_listPlayer.Count; ++i)
        {
            Player refPlayer = m_listPlayer[i];
            if (refPlayer != null && refPlayer.IsAlive)
                ++iCount;
        }

        return iCount;
    }

    public Player GetLocalPlayer()
    {
        foreach(Player refPlayer in m_listPlayer)
        {
            if (refPlayer.IsMine)
                return refPlayer;
        }

        return null;
    }


    private void OnDestroy()
    {
        if (m_Instance != this)
            return;

        foreach (Player player in m_listPlayer)
            if (player != null)
                player.OnDeadEvent -= OnPlayerDead;
        m_listPlayer.Clear();
        m_Instance = null;
    }

}
