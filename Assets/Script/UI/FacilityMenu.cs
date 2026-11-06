using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class FacilityMenu : MonoBehaviour, IUserInterface
{
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.3f;

    private Tween _tween;

    void IUserInterface.Start()
    {
        _openButton.OnClickAsObservable().Subscribe(_ =>
        {
            OpenCanvas().Forget();
        }).AddTo(this);
        _closeButton.OnClickAsObservable().Subscribe(_ =>
        {
            CloseCanvas().Forget();
        }).AddTo(this);

        _canvasGroup.alpha = 0.0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    void IUserInterface.Update()
    {
        
    }

    private async UniTask OpenCanvas()
    {
        _tween.Kill();
        _tween = null;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        var sequence = DOTween.Sequence();
        _tween = sequence.Join(_canvasGroup.DOFade(1.0f, _fadeDuration));
        await sequence.ToUniTask();
        
        if (this != null && _tween == sequence)
        {
            _canvasGroup.interactable = true;
        }
    }

    private async UniTask CloseCanvas()
    {
        _tween.Kill();
        _tween = null;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var sequence = DOTween.Sequence();
        _tween = sequence.Join(_canvasGroup.DOFade(0.0f, _fadeDuration));
        await sequence.ToUniTask();

    }
}
