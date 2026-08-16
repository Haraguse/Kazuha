namespace FluentAvalonia.UI.Controls;

internal struct LiveReorderIndices(int dragItemIndex, int dragOverIndex, int count)
{
	public int draggedItemIndex = dragItemIndex;

	public int draggedOverIndex = dragOverIndex;

	public int itemsCount = count;
}
