using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public ScrollRect _scrollRect;
    [SerializeField] public RectTransform _content;

    [Tooltip("Optional. If empty, uses the ScrollRect's horizontal scrollbar.")]
    [SerializeField] private Scrollbar _horizontalScrollbar;
    [Tooltip("Fixed handle size; the scrollbar represents item centers, not content bounds.")]
    [SerializeField, Range(0.01f, 1f)] private float _scrollbarHandleSize = 0.01f;

    [SerializeField] private int _childCount;
    [SerializeField] private float _scrollWidth;
    [SerializeField] private float _contentDeltaWidth;
    [SerializeField] private float _contentWidth;
    [SerializeField] private float _spacing;
    [SerializeField] private float _duration = 0.5f;

    // Kept for compatibility with existing serialized scenes. No longer needed:
    // indices now refer only to children that have ScrollSnapItem.
    [SerializeField] public int _childOffset = 0;

    [SerializeField] private int _currentIndex;
    [SerializeField] private int _currentDragIndex;

    public int StartIndex;
    public int EndIndex;
    public float MinVelocity;
    public Action<int> SnapEvent;

    [SerializeField] private int _radius;
    [SerializeField] private float _scale;
    [SerializeField] private bool _disabled;

    // Keeps the viewport-width behavior of your original Start() method.
    // Turn this off in the Inspector if you want the full-width viewport instead.
    [SerializeField] private bool _resizeViewportToItemWidth = true;

    private readonly List<ScrollSnapItem> _items = new List<ScrollSnapItem>();
    private readonly Vector3[] _corners = new Vector3[4];
    private ScrollSnapItem _selectedItem;
    private Vector2 _lastDelta;
    private Tweener _mainTweener;
    private InputAction _leftAction;
    private InputAction _rightAction;
    private bool _viewportResized;
    private Scrollbar _boundScrollbar;
    private Scrollbar _originalScrollRectScrollbar;
    private ScrollRect _boundScrollRect;
    private EventTrigger _scrollbarTrigger;
    private readonly List<EventTrigger.Entry> _scrollbarEntries =
        new List<EventTrigger.Entry>();
    private int _originalScrollbarSteps;
    private float _originalScrollbarSize;
    private bool _scrollbarPressed;
    private bool _scrollbarSnapPending;
    private bool _syncingScrollbar;
    private bool _visualsDirty = true;
    private bool _scrollbarDirty = true;
    private bool _hasGeometry;
    private RectTransform _lastViewport;
    private Rect _lastContentRect;
    private Rect _lastViewportRect;
    private Matrix4x4 _lastContentMatrix;
    private Matrix4x4 _lastViewportMatrix;
    private int _lastStartIndex;
    private int _lastEndIndex;
    private int _lastRadius;
    private float _lastScale;
    private float _lastScrollbarHandleSize;

    public Transform CurrentObject => CurrentItem != null ? CurrentItem.transform : null;

    public ScrollSnapItem CurrentItem =>
        _currentIndex >= 0 && _currentIndex < _items.Count
            ? _items[_currentIndex]
            : null;

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            SetCurrentIndex(value);
            _visualsDirty = true;
        }
    }

    private void OnEnable()
    {
        InvalidateGeometry();
        foreach (ScrollSnapItem item in _items)
            if (item != null)
                item.LayoutOwner = this;
        BindScrollbar();

        // Cache the exact action instances so OnDisable removes the same subscriptions.
        var actions = InputController.instance.InputActions;
        _leftAction = actions.UI.Left;
        _rightAction = actions.UI.Right;
        _leftAction.performed += ScrollToLeft;
        _rightAction.performed += ScrollToRight;
    }

    private void OnDisable()
    {
        foreach (ScrollSnapItem item in _items)
            if (item != null && item.LayoutOwner == this)
                item.LayoutOwner = null;
        UnbindScrollbar();
        if (_leftAction != null)
            _leftAction.performed -= ScrollToLeft;
        if (_rightAction != null)
            _rightAction.performed -= ScrollToRight;

        _leftAction = null;
        _rightAction = null;
        _mainTweener?.Kill();
        _mainTweener = null;
    }

    private void Start()
    {
        Recalculate();
    }

    [ContextMenu("Recalculate")]
    public void Recalculate()
    {
        InvalidateGeometry();
        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null)
            return;

        _content = _scrollRect.content;
        if (_content == null)
            return;

        if (isActiveAndEnabled)
            BindScrollbar();

        // Rebuild once when items are created/removed, not on every Update.
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        foreach (ScrollSnapItem item in _items)
            if (item != null && item.LayoutOwner == this)
                item.LayoutOwner = null;
        _items.Clear();
        foreach (Transform child in _content)
        {
            ScrollSnapItem item;
            if (child.gameObject.activeInHierarchy &&
                child.TryGetComponent(out item))
            {
                _items.Add(item);
                item.LayoutOwner = this;
            }
        }

        _childCount = _items.Count;

        if (_childCount == 0)
        {
            _selectedItem?.Deselect();
            _selectedItem = null;
            _currentIndex = 0;
            StartIndex = 0;
            EndIndex = -1;
            _mainTweener?.Kill();
            _mainTweener = null;
            _scrollbarPressed = false;
            _scrollbarSnapPending = false;
            SyncScrollbar();
            return;
        }

        // Indices now refer exclusively to valid, active ScrollSnapItem children.
        StartIndex = 0;
        EndIndex = _childCount - 1;

        var viewport = GetViewport();
        _scrollWidth = viewport.rect.width;
        _contentDeltaWidth = _content.sizeDelta.x;
        _contentWidth = ((RectTransform)_items[0].transform).rect.width;

        var layout = _content.GetComponent<HorizontalLayoutGroup>();
        _spacing = layout != null ? Mathf.Abs(layout.spacing) : 0f;

        // Preserve the original narrow/centered viewport setup, but apply it
        // only once (reapplying your original offsets shrinks it incorrectly).
        if (_resizeViewportToItemWidth && !_viewportResized &&
            _scrollRect.viewport != null && _contentWidth > 0f)
        {
            float inset = (viewport.rect.width - _contentWidth) * 0.5f;
            viewport.offsetMin = new Vector2(inset, viewport.offsetMin.y);
            viewport.offsetMax = new Vector2(-inset, viewport.offsetMax.y);
            _viewportResized = true;
        }

        // Keep selection when possible after a content refresh.
        int previousIndex = _selectedItem != null
            ? _items.IndexOf(_selectedItem)
            : -1;
        SetCurrentIndex(previousIndex >= 0 ? previousIndex :
            Mathf.Clamp(_currentIndex, StartIndex, EndIndex));

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        InvalidateGeometry();
        SyncScrollbar();
    }

    internal void InvalidateGeometry()
    {
        _visualsDirty = true;
        _scrollbarDirty = true;
    }

    private void CheckForGeometryChanges()
    {
        if (_scrollRect == null || _content == null)
            return;

        RectTransform viewport = GetViewport();
        Rect contentRect = _content.rect;
        Rect viewportRect = viewport.rect;
        Matrix4x4 contentMatrix = _content.localToWorldMatrix;
        Matrix4x4 viewportMatrix = viewport.localToWorldMatrix;
        // Constant work while idle, independent of the number of items. Matrices
        // also catch parent transforms and externally scripted position changes.
        if (!_hasGeometry || _lastViewport != viewport ||
            !_lastContentRect.Equals(contentRect) ||
            !_lastViewportRect.Equals(viewportRect) ||
            !_lastContentMatrix.Equals(contentMatrix) ||
            !_lastViewportMatrix.Equals(viewportMatrix) ||
            _lastStartIndex != StartIndex || _lastEndIndex != EndIndex)
        {
            _hasGeometry = true;
            _lastViewport = viewport;
            _lastContentRect = contentRect;
            _lastViewportRect = viewportRect;
            _lastContentMatrix = contentMatrix;
            _lastViewportMatrix = viewportMatrix;
            _lastStartIndex = StartIndex;
            _lastEndIndex = EndIndex;
            InvalidateGeometry();
        }

        if (_lastRadius != _radius || _lastScale != _scale)
        {
            _lastRadius = _radius;
            _lastScale = _scale;
            _visualsDirty = true;
        }
        if (_lastScrollbarHandleSize != _scrollbarHandleSize)
        {
            _lastScrollbarHandleSize = _scrollbarHandleSize;
            _scrollbarDirty = true;
        }
    }

    // Unity's native binding includes spacers and drives handle size. Disconnect
    // it while this component owns the scrollbar, and restore it on disable.
    private void BindScrollbar()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null)
            return;

        if (_horizontalScrollbar == null)
            _horizontalScrollbar = _scrollRect.horizontalScrollbar;
        if (_boundScrollbar == _horizontalScrollbar && _boundScrollRect == _scrollRect)
            return;

        UnbindScrollbar();
        if (_horizontalScrollbar == null)
            return;

        _boundScrollRect = _scrollRect;
        _boundScrollbar = _horizontalScrollbar;
        _scrollbarDirty = true;
        _originalScrollRectScrollbar = _scrollRect.horizontalScrollbar;
        _originalScrollbarSteps = _boundScrollbar.numberOfSteps;
        _originalScrollbarSize = _boundScrollbar.size;
        _scrollRect.horizontalScrollbar = null;
        _boundScrollbar.numberOfSteps = 0; // Continuous, not one step per item.
        _boundScrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        _scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

        _scrollbarTrigger = _boundScrollbar.GetComponent<EventTrigger>();
        if (_scrollbarTrigger == null)
            _scrollbarTrigger = _boundScrollbar.gameObject.AddComponent<EventTrigger>();
        AddScrollbarTrigger(EventTriggerType.PointerDown, BeginScrollbarInteraction);
        AddScrollbarTrigger(EventTriggerType.BeginDrag, BeginScrollbarInteraction);
        AddScrollbarTrigger(EventTriggerType.PointerUp, EndScrollbarInteraction);
        AddScrollbarTrigger(EventTriggerType.EndDrag, EndScrollbarInteraction);
        AddScrollbarTrigger(EventTriggerType.Cancel, EndScrollbarInteraction);
        SyncScrollbar();
    }

    private void AddScrollbarTrigger(EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        _scrollbarTrigger.triggers.Add(entry);
        _scrollbarEntries.Add(entry);
    }

    private void UnbindScrollbar()
    {
        _scrollbarPressed = false;
        _scrollbarSnapPending = false;
        if (_scrollbarTrigger != null)
        {
            foreach (var entry in _scrollbarEntries)
                _scrollbarTrigger.triggers.Remove(entry);
        }
        _scrollbarEntries.Clear();
        _scrollbarTrigger = null;

        if (_boundScrollbar != null)
        {
            _boundScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
            _boundScrollbar.numberOfSteps = _originalScrollbarSteps;
            _boundScrollbar.size = _originalScrollbarSize;
        }
        if (_boundScrollRect != null)
        {
            _boundScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
            if (_boundScrollRect.horizontalScrollbar == null)
                _boundScrollRect.horizontalScrollbar = _originalScrollRectScrollbar;
        }
        _boundScrollbar = null;
        _boundScrollRect = null;
        _originalScrollRectScrollbar = null;
    }

    private void BeginScrollbarInteraction(BaseEventData data)
    {
        var pointer = data as PointerEventData;
        if (pointer != null && pointer.button != PointerEventData.InputButton.Left)
            return;
        if (_disabled || _items.Count == 0 || _boundScrollbar == null ||
            !_boundScrollbar.IsInteractable())
            return;

        _scrollbarPressed = true;
        _scrollbarSnapPending = false;
        _mainTweener?.Kill();
        _mainTweener = null;
        _scrollRect.StopMovement();
    }

    private void EndScrollbarInteraction(BaseEventData data)
    {
        var pointer = data as PointerEventData;
        if (pointer != null && pointer.button != PointerEventData.InputButton.Left)
            return;
        if (!_scrollbarPressed)
            return;

        _scrollbarPressed = false;
        // Wait until PointerUp and EndDrag finish. Either may arrive first;
        // together they must request only one snap.
        _scrollbarSnapPending = true;
    }

    private void OnScrollbarValueChanged(float value)
    {
        if (_syncingScrollbar || _scrollRect == null || _content == null)
            return;
        // The user changed the handle, even if the content doesn't move (for
        // example when disabled or when there is only one item).
        _scrollbarDirty = true;
        if (_disabled || _items.Count == 0)
        {
            SyncScrollbar();
            return;
        }

        _mainTweener?.Kill();
        _mainTweener = null;
        _scrollRect.StopMovement();
        RectTransform viewport = GetViewport();
        float width = GetScrollableWidth();
        float position = _scrollRect.horizontalNormalizedPosition;
        float first = GetItemScrollPosition(StartIndex, viewport, width, position);
        float last = GetItemScrollPosition(EndIndex, viewport, width, position);
        // Track continuously without lag or stacked tweens. Animate only the snap.
        SetScrollPosition(Mathf.Clamp01(
            Mathf.Lerp(first, last, Mathf.Clamp01(value))));
        SetCurrentIndex(GetCurrentIndex());

        // Keyboard/controller and scripted value changes also settle on an item.
        // PointerDown may run after this callback; Begin clears the pending flag.
        if (!_scrollbarPressed)
            _scrollbarSnapPending = true;
    }

    private void OnScrollRectValueChanged(Vector2 position)
    {
        SyncScrollbar();
    }

    private void SetScrollPosition(float position)
    {
        // A ScrollRect position write can force a canvas rebuild. In the Editor,
        // Scrollbar.Rebuild can invoke onValueChanged even for an unchanged value.
        bool wasSyncing = _syncingScrollbar;
        _syncingScrollbar = true;
        try
        {
            _scrollRect.horizontalNormalizedPosition = position;
        }
        finally
        {
            _syncingScrollbar = wasSyncing;
        }
        SyncScrollbar();
    }

    private void LateUpdate()
    {
        if (_scrollbarPressed)
        {
            // ScrollRect can measure velocity during its own LateUpdate.
            _scrollRect.StopMovement();
            if (_boundScrollbar == null || !_boundScrollbar.isActiveAndEnabled ||
                !_boundScrollbar.IsInteractable())
            {
                _scrollbarPressed = false;
                _scrollbarSnapPending = true;
            }
        }
        if (_scrollbarSnapPending && !_scrollbarPressed)
        {
            _scrollbarSnapPending = false;
            Snap(GetCurrentIndex(), _duration, false);
        }
        // Covers DOTween, inertia, mouse wheel, and external position changes.
        SyncScrollbar();
    }

    private void SyncScrollbar()
    {
        if (_boundScrollbar == null || _syncingScrollbar)
            return;

        CheckForGeometryChanges();
        if (!_scrollbarDirty)
            return;
        _scrollbarDirty = false;

        float value = 0f;
        if (_items.Count > 1 && _content != null)
        {
            RectTransform viewport = GetViewport();
            float width = GetScrollableWidth();
            float position = _scrollRect.horizontalNormalizedPosition;
            float first = GetItemScrollPosition(StartIndex, viewport, width, position);
            float last = GetItemScrollPosition(EndIndex, viewport, width, position);
            value = Mathf.InverseLerp(first, last, position);
        }
        _syncingScrollbar = true;
        try
        {
            _boundScrollbar.size = _items.Count > 1 ? _scrollbarHandleSize : 1f;
            _boundScrollbar.SetValueWithoutNotify(value);
        }
        finally
        {
            _syncingScrollbar = false;
        }
    }

    // Unclamped endpoints preserve the exact center-to-center mapping. As with
    // the original Snap, edge padding must allow those centers to be reached.
    private float GetItemScrollPosition(int index)
    {
        return GetItemScrollPosition(index, GetViewport(), GetScrollableWidth(),
            _scrollRect.horizontalNormalizedPosition);
    }

    private float GetItemScrollPosition(int index, RectTransform viewport,
        float width, float position)
    {
        if (width <= 0.0001f)
            return 0f;
        RectTransform item = (RectTransform)_items[index].transform;
        float center = viewport.InverseTransformPoint(
            item.TransformPoint(item.rect.center)).x;
        return position +
            (center - viewport.rect.center.x) / width;
    }

    private RectTransform GetViewport()
    {
        return _scrollRect.viewport != null
            ? _scrollRect.viewport
            : (RectTransform)_scrollRect.transform;
    }

    private void SetCurrentIndex(int index)
    {
        if (_items.Count == 0)
            return;

        index = Mathf.Clamp(index, StartIndex, EndIndex);
        ScrollSnapItem next = _items[index];
        if (_selectedItem == next)
        {
            _currentIndex = index;
            return;
        }

        _selectedItem?.Deselect();
        _currentIndex = index;
        _selectedItem = next;
        if (!_disabled)
            _selectedItem.Select();
        SnapEvent?.Invoke(index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _scrollbarPressed = false;
        _scrollbarSnapPending = false;
        _mainTweener?.Kill();
        _mainTweener = null;
        _lastDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData != null)
            _lastDelta = eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_disabled || _items.Count == 0)
            return;

        int index = GetCurrentIndex();
        float magnitude = _scrollRect.velocity.magnitude;

        if (MinVelocity <= magnitude && Mathf.Abs(_lastDelta.x) > 0.01f)
        {
            float steps = Mathf.Sqrt(magnitude / 1000f);
            int target = Mathf.Clamp(
                index - (int)(steps * Mathf.Sign(_lastDelta.x)),
                StartIndex, EndIndex);
            Snap(target, steps * 0.35f, false);
        }
        else
        {
            Snap(index, _duration, false);
        }
    }

    private void Update()
    {
        if (_disabled || _items.Count == 0)
            return;

        CheckForGeometryChanges();
        if (!_visualsDirty)
            return;
        // Clear before callbacks: a selection listener may invalidate the layout.
        _visualsDirty = false;

        int nearest = GetCurrentIndex();
        if (nearest != _currentIndex)
            SetCurrentIndex(nearest);

        Scale(_radius, _scale);
    }

    // Find the nearest *valid item* to the viewport's visible center.
    // No index/width formula: dummy objects and spacers are ignored.
    private int GetCurrentIndex()
    {
        if (_items.Count == 0)
            return 0;

        RectTransform viewport = GetViewport();
        float viewportCenterX = viewport.rect.center.x;
        int nearestIndex = StartIndex;
        float nearestDistance = float.MaxValue;

        for (int i = StartIndex; i <= EndIndex; i++)
        {
            var rect = (RectTransform)_items[i].transform;
            float itemCenterX = viewport.InverseTransformPoint(
                rect.TransformPoint(rect.rect.center)).x;
            float distance = Mathf.Abs(itemCenterX - viewportCenterX);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    public void ScrollToLeft(InputAction.CallbackContext ctx = default)
    {
        if (_disabled || _scrollbarPressed || _items.Count == 0)
            return;

        Snap(Mathf.Clamp(_currentIndex - 1, StartIndex, EndIndex),
            _duration, false);
    }

    public void ScrollToRight(InputAction.CallbackContext ctx = default)
    {
        if (_disabled || _scrollbarPressed || _items.Count == 0)
            return;

        Snap(Mathf.Clamp(_currentIndex + 1, StartIndex, EndIndex),
            _duration, false);
    }

    public void ScrollToRight()
    {
        ScrollToRight(default(InputAction.CallbackContext));
    }

    public void ScrollToLeft()
    {
        ScrollToLeft(default(InputAction.CallbackContext));
    }

    private void Scale(int radius, float scale)
    {
        float centerX = GetViewport().rect.center.x;
        RectTransform viewport = GetViewport();
        float range = (_contentWidth + _spacing) * radius;

        foreach (ScrollSnapItem item in _items)
        {
            if (item == null || item.Content == null)
                continue;

            if (range <= 0f)
            {
                if (!item.Content.localScale.Equals(Vector3.one))
                    item.Content.localScale = Vector3.one;
                continue;
            }

            var rect = (RectTransform)item.transform;
            float itemCenterX = viewport.InverseTransformPoint(
                rect.TransformPoint(rect.rect.center)).x;
            float factor = Mathf.Clamp01(
                (range - Mathf.Abs(itemCenterX - centerX)) / range);

            Vector3 targetScale = Vector3.one * (factor * scale + 1f);
            if (!item.Content.localScale.Equals(targetScale))
                item.Content.localScale = targetScale;
        }
    }

    public void Snap(int index, bool instant)
    {
        Snap(index, _duration, instant);
    }

    private float GetScrollableWidth()
    {
        RectTransform viewport = GetViewport();
        _content.GetWorldCorners(_corners);
        float contentLeft = viewport.InverseTransformPoint(_corners[0]).x;
        float contentRight = viewport.InverseTransformPoint(_corners[2]).x;
        return Mathf.Max(0f,
            contentRight - contentLeft - viewport.rect.width);
    }

    private void Snap(int index, float duration, bool instant)
    {
        if (_disabled || _scrollbarPressed || _items.Count == 0)
            return;

        index = Mathf.Clamp(index, StartIndex, EndIndex);
        _visualsDirty = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        _scrollbarSnapPending = false;
        float target = Mathf.Clamp01(GetItemScrollPosition(index));

        _mainTweener?.Kill();
        _scrollRect.StopMovement();

        if (instant || duration <= 0f)
        {
            SetScrollPosition(target);
            SetCurrentIndex(index);
            SyncScrollbar();
            return;
        }

        _mainTweener = DOTween.To(
                () => _scrollRect.horizontalNormalizedPosition,
                SetScrollPosition,
                target,
                duration)
            .SetEase(Ease.OutQuint);
    }

    [ContextMenu("Disable")]
    public void Disable()
    {
        _disabled = true;
        _scrollbarPressed = false;
        _scrollbarSnapPending = false;
        _mainTweener?.Kill();
        _scrollRect?.StopMovement();

        _selectedItem?.Deselect();
        if (_selectedItem != null && _selectedItem.Content != null)
            _selectedItem.Content.localScale = Vector3.one;
    }

    [ContextMenu("Enable")]
    public void Enable()
    {
        _disabled = false;
        InvalidateGeometry();
        _selectedItem?.Select();
        if (_items.Count > 0)
            Scale(_radius, _scale);
    }
}
