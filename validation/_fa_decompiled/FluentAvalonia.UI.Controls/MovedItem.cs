using Avalonia;

namespace FluentAvalonia.UI.Controls;

internal struct MovedItem
{
	public int sourceIndex;

	public int destinationIndex;

	public Rect sourceRect;

	public Rect destinationRect;

	public MovedItem(int src, int dst, Rect srcRc, Rect dstRc)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		sourceIndex = src;
		destinationIndex = dst;
		sourceRect = srcRc;
		destinationRect = dstRc;
	}
}
