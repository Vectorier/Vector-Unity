using UnityEngine;
using UnityEngine.UI;

public class ScrollSnapItem : MonoBehaviour, ILayoutController
{
	// Layout can move a card while the content bounds stay unchanged. Notify the
	// scroller during that layout pass instead of polling every card while idle.
	internal ScrollSnap LayoutOwner;

	public void SetLayoutHorizontal() => NotifyLayoutChanged();
	public void SetLayoutVertical() => NotifyLayoutChanged();
	private void OnRectTransformDimensionsChange() => NotifyLayoutChanged();
	private void OnDidApplyAnimationProperties() => NotifyLayoutChanged();

	private void NotifyLayoutChanged()
	{
		if (LayoutOwner != null)
			LayoutOwner.InvalidateGeometry();
	}

	public RectTransform Content;

	public Text SelectionText;

	public Image SelectionImage;

	public Color SelectionColor;

	public Color RegularColor;

	public bool IsSelected;

	public void Select()
	{
        if (SelectionText != null)
        {
            SelectionText.color = SelectionColor;
        }
        if (SelectionImage != null)
        {
            SelectionImage.color = SelectionColor;
        }
        IsSelected = transform;
    }

	public void Deselect()
	{
        if (SelectionText != null)
        {
            SelectionText.color = RegularColor;
        }
		if (SelectionImage != null)
		{
			SelectionImage.color = RegularColor;
		}
		IsSelected = false;
    }
}
