using System;
using DG.Tweening;
using Nekki.Vector.Core.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class TutorialKeyboard : MonoBehaviour
{
    [SerializeField] private RectTransform _key;

    [SerializeField] private RectTransform _arrow;

    [SerializeField] private Text _text;
    
    private Sequence _sequence;

    public void Show(Key key, string description)
    {
        _key.transform.localScale = Vector3.one;
        
        _arrow.transform.eulerAngles = key switch
        {
            Key.Up => Vector3.zero,
            Key.Down => new Vector3(0, 0, 180),
            Key.Left => new Vector3(0, 0, 90),
            Key.Right => new Vector3(0, 0, -90),
            _ => Vector3.zero
        };
        _text.text = description;
        
        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(0.2f);
        _sequence.Append(_key.DOScale(new Vector3(0.85f, 0.85f, 0.85f), 0.3f)).AppendInterval(0.3f).Append(_key.DOScale(Vector3.one, 0.6f)).SetLoops(-1);
    }

    private void OnDisable()
    {
        _sequence.Kill();
        _sequence = null;
    }

    private void OnDestroy()
    {
        _sequence.Kill();
        _sequence = null;
    }
}
