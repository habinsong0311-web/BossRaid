using System;
using DG.Tweening;
using UnityEngine;

public class HalfCircleTelegraph : MonoBehaviour
{
    [SerializeField] private Renderer fillRenderer;
    [SerializeField] private Ease fillEase = Ease.Linear;

    private static readonly int FillAmountId =
        Shader.PropertyToID("_FillAmount");

    private Material fillMaterial;
    private Tween fillTween;

    private void Awake()
    {
        // 장판마다 독립적인 재질을 사용합니다.
        fillMaterial = fillRenderer.material;
    }

    public void Play(float duration, Action onComplete = null)
    {
        fillTween?.Kill();
        fillMaterial.SetFloat(FillAmountId, 0f);

        fillTween = DOVirtual.Float(
            0f,
            1f,
            duration,
            progress => fillMaterial.SetFloat(FillAmountId, progress)
        )
        .SetEase(fillEase)
        .SetLink(gameObject)
        .OnComplete(() => onComplete?.Invoke());
    }

    private void OnDestroy()
    {
        fillTween?.Kill();

        if (fillMaterial != null)
            Destroy(fillMaterial);
    }
}