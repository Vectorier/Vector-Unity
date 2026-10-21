using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.InputControllers;

namespace UI.Dialogs
{
    /// <summary>Owns modal ordering and restores the prior state of scene UI input.</summary>
    public static class DialogCoordinator
    {
        public static readonly DialogsQueue Dialogs = new DialogsQueue(), Notifications = new DialogsQueue();

        private sealed class Entry
        {
            public BaseDialog Dialog;
            public bool Modal, AddedCanvas, AddedCaster, OverrideSorting, CanvasEnabled;
            public Canvas Canvas;
            public GraphicRaycaster Caster;
            public int Order, Layer;
            public DialogsQueue Queue;
        }

        private static readonly List<Entry> open = new List<Entry>();
        private static readonly Dictionary<BaseDialog, Entry> overlays = new Dictionary<BaseDialog, Entry>();
        private static readonly Dictionary<Behaviour, bool> input = new Dictionary<Behaviour, bool>();
        private static bool blocked;
        private static GameObject blocker;
        public static int OpenDialogCount => open.Count;

        public static void Show(BaseDialog dialog, bool modal)
        {
            if (dialog == null || open.Exists(e => e.Dialog == dialog)) return;
            var entry = new Entry { Dialog = dialog, Modal = modal, Queue = DialogsQueue.ShowingQueue };
            dialog.gameObject.SetActive(true);
            if (modal)
            {
                CaptureInput(dialog);
                if (dialog.GetComponentInParent<Canvas>() != null)
                {
                    overlays.TryGetValue(dialog, out var retained);
                    entry.Canvas = dialog.GetComponent<Canvas>();
                    entry.AddedCanvas = entry.Canvas == null ||
                                        (retained != null && retained.AddedCanvas && retained.Canvas == entry.Canvas);
                    if (entry.Canvas == null) entry.Canvas = dialog.gameObject.AddComponent<Canvas>();
                    entry.OverrideSorting = entry.Canvas.overrideSorting;
                    entry.Order = entry.Canvas.sortingOrder;
                    entry.Layer = entry.Canvas.sortingLayerID;
                    entry.CanvasEnabled = entry.Canvas.enabled;
                    entry.Canvas.enabled = true;
                    entry.Caster = dialog.GetComponent<GraphicRaycaster>();
                    entry.AddedCaster = entry.Caster == null ||
                                        (retained != null && retained.AddedCaster && retained.Caster == entry.Caster);
                    if (entry.Caster == null) entry.Caster = dialog.gameObject.AddComponent<GraphicRaycaster>();
                    if (entry.AddedCanvas || entry.AddedCaster) overlays[dialog] = entry;
                }
            }

            open.Add(entry);
            if (modal)
            {
                if (!blocked)
                {
                    blocked = true;
                    UIBindings.InputBlockingChanged?.Invoke(true);
                    UIBindings.BackgroundEffectChanged?.Invoke(true);
                }

                UpdateModalOrder();
            }
        }

        private static void CaptureInput(BaseDialog dialog)
        {
            InputController.instance.Disable();
            foreach (var keyboard in Object.FindObjectsOfType<KeyboardController>(true))
                if (keyboard.gameObject.scene == dialog.gameObject.scene)
                    SaveAndDisable(keyboard);
            foreach (var caster in Object.FindObjectsOfType<GraphicRaycaster>(true))
                if (caster.gameObject.scene == dialog.gameObject.scene &&
                    (blocker == null || caster.gameObject != blocker) && !IsOwnedCaster(caster))
                    SaveAndDisable(caster);
        }

        private static bool IsOwnedCaster(GraphicRaycaster caster)
        {
            foreach (var entry in overlays.Values)
                if (entry.AddedCaster && entry.Caster == caster)
                    return true;
            return false;
        }

        private static void SaveAndDisable(Behaviour component)
        {
            if (!input.ContainsKey(component)) input.Add(component, component.enabled);
            component.enabled = false;
        }

        private static void UpdateModalOrder()
        {
            Entry top = null;
            foreach (var entry in open)
                if (entry.Modal && entry.Dialog != null)
                    top = entry;
            if (top == null)
            {
                RestoreInput();
                return;
            }

            foreach (var saved in input)
                if (saved.Key != null)
                    saved.Key.enabled = false;
            foreach (var entry in open)
                if (entry.Modal && entry.Caster != null)
                    entry.Caster.enabled = false;
            if (top.Canvas == null) return;
            int order = 0,
                layer = top.Canvas.rootCanvas.sortingLayerID,
                layerValue = top.Canvas.rootCanvas.cachedSortingLayerValue;
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas.gameObject.scene != top.Dialog.gameObject.scene ||
                    (blocker != null && canvas.gameObject == blocker) || open.Exists(e => e.Canvas == canvas)) continue;
                order = Mathf.Max(order, canvas.sortingOrder);
                if (canvas.cachedSortingLayerValue > layerValue)
                {
                    layer = canvas.sortingLayerID;
                    layerValue = canvas.cachedSortingLayerValue;
                }
            }

            foreach (var entry in open)
                if (entry.Modal && entry.Canvas != null)
                {
                    entry.Canvas.overrideSorting = true;
                    entry.Canvas.sortingLayerID = layer;
                    entry.Canvas.sortingOrder = ++order;
                }

            if (blocker == null)
                blocker = new GameObject("UI Modal Blocker", typeof(RectTransform), typeof(Canvas), typeof(Image),
                    typeof(GraphicRaycaster));
            blocker.transform.SetParent(top.Canvas.rootCanvas.transform, false);
            var rect = (RectTransform)blocker.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = Color.clear;
            var blockerCanvas = blocker.GetComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerID = layer;
            blockerCanvas.sortingOrder = ++order;
            top.Canvas.sortingOrder = ++order;
            top.Caster.enabled = true;
        }

        public static void Dismiss(BaseDialog dialog)
        {
            int index = open.FindIndex(e => ReferenceEquals(e.Dialog, dialog));
            if (index < 0) return;
            var entry = open[index];
            open.RemoveAt(index);
            RestoreEntry(entry);
            UpdateModalOrder();
            entry.Queue?.CompleteCurrent();
        }

        private static void RestoreEntry(Entry entry)
        {
            if (entry.Canvas != null)
            {
                entry.Canvas.overrideSorting = entry.OverrideSorting;
                entry.Canvas.sortingOrder = entry.Order;
                entry.Canvas.sortingLayerID = entry.Layer;
                entry.Canvas.enabled = entry.CanvasEnabled;
            }

            if (entry.AddedCaster && entry.Caster != null) entry.Caster.enabled = false;
        }

        private static void DestroyTemporary(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }

        private static void RestoreInput()
        {
            foreach (var pair in input)
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value;
            input.Clear();
            if (blocker != null)
            {
                blocker.SetActive(false);
                DestroyTemporary(blocker);
                blocker = null;
            }

            if (blocked)
            {
                blocked = false;
                UIBindings.InputBlockingChanged?.Invoke(false);
                UIBindings.BackgroundEffectChanged?.Invoke(false);
            }
            
            InputController.instance.Enable();
            
        }

        public static void Reset()
        {
            foreach (var entry in open) RestoreEntry(entry);
            open.Clear();
            RestoreInput();
            Dialogs.Clear();
            Notifications.Clear();
        }

        internal static void Forget(BaseDialog dialog)
        {
            overlays.Remove(dialog);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession()
        {
            Reset();
            overlays.Clear();
        }
    }
}