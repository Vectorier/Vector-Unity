using System;
using DG.Tweening;
using Nekki.Vector.Core.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGamepad : MonoBehaviour
{
    [SerializeField] private RectTransform _root;

    [SerializeField] private RectTransform _analog;

    [SerializeField] private Image _arrow;

    [SerializeField] private Text _text;

    Sequence _sequence;
    
    private Vector2 _initialAnalogPosition;
    private Color _initialArrowColor;

    private void Awake()
    {
        _initialAnalogPosition = _analog.anchoredPosition;
        _initialArrowColor = _arrow.color;
    }

    public void Show(Key key, string description)
    {
        Vector3 angle = Vector3.zero;
        string text = "";

        switch (key)
        {
            case Key.Up:
                angle = new Vector3(0f, 0f, 90f);
                text = LocalizationManager.Instance.GetTranslation("help_up_constxt");
                break;
            case Key.Down:
                angle = new Vector3(0f, 0f, -90f);
                text = LocalizationManager.Instance.GetTranslation("help_down_constxt");
                break;
            case Key.Left:
                angle = new Vector3(0, 0f, -180f);
                text = LocalizationManager.Instance.GetTranslation("help_left_constxt");
                break;
            case Key.Right:
                text = LocalizationManager.Instance.GetTranslation("help_right_constxt");
                break;
        }
        
        _root.transform.eulerAngles = angle;
        _text.text = text + " " + description;
        
        _sequence = DOTween.Sequence();

        _sequence.AppendInterval(0.2f);

        _sequence.Append(
            _analog.DOAnchorPosX(40, 0.7f).SetEase(Ease.OutSine)
        );

        _sequence.Join(
            _arrow.DOFade(0.2f, 0.7f)
        );

        _sequence.AppendInterval(0.1f);

        _sequence.SetLoops(-1);
    }
    
    private void OnDisable()
    {
        _sequence.Kill();
        _sequence = null;
        
        _analog.anchoredPosition = _initialAnalogPosition;
        _arrow.color = _initialArrowColor;
    }
    
    private void OnDestroy()
    {
        _sequence.Kill();
        _sequence = null;
        
        _analog.anchoredPosition = _initialAnalogPosition;
        _arrow.color = _initialArrowColor;
    }
}