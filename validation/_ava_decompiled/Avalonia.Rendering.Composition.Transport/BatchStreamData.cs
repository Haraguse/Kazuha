using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// The batch data is separated into 2 "streams":
/// - objects: CLR reference types that are references to either server-side or common objects
/// - structs: blittable types like int, Matrix, Color
/// Each "stream" consists of memory segments that are pooled 
/// </summary>
internal class BatchStreamData
{
	public Queue<BatchStreamSegment<object?[]>> Objects { get; } = new Queue<BatchStreamSegment<object[]>>();

	public Queue<BatchStreamSegment<nint>> Structs { get; } = new Queue<BatchStreamSegment<nint>>();
}
