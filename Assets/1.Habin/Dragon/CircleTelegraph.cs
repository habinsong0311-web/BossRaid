using System;
using DG.Tweening;
using UnityEngine;

public class CircleTelegraph : MonoBehaviour
{
    [SerializeField] private Transform fill;

    private Vector3 fullScale;
    private Tween fillTween;

    private void Awake()
    {
        fullScale = fill.localScale;
    }

    public void Play(float duration, Action onComplete)
    {
        fillTween?.Kill();

        fill.localScale = new Vector3(0f, fullScale.y, 0f);

        fillTween = fill.DOScale(fullScale, duration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }
}