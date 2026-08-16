using Avalonia.Reactive;

namespace Avalonia.Media;

/// <summary>
/// Transforms an <see cref="T:Avalonia.Visual" /> according to a <see cref="P:Avalonia.Media.MatrixTransform.Matrix" />.
/// </summary>
public sealed class MatrixTransform : Transform
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.MatrixTransform.Matrix" /> property.
	/// </summary>
	public static readonly StyledProperty<Matrix> MatrixProperty = AvaloniaProperty.Register<MatrixTransform, Matrix>("Matrix", Matrix.Identity);

	/// <summary>
	/// Gets or sets the matrix.
	/// </summary>
	public Matrix Matrix
	{
		get
		{
			return GetValue(MatrixProperty);
		}
		set
		{
			SetValue(MatrixProperty, value);
		}
	}

	/// <summary>
	/// Gets the matrix.
	/// </summary>
	public override Matrix Value => Matrix;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.MatrixTransform" /> class.
	/// </summary>
	public MatrixTransform()
	{
		this.GetObservable(MatrixProperty).Subscribe(delegate
		{
			RaiseChanged();
		});
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.MatrixTransform" /> class.
	/// </summary>
	/// <param name="matrix">The matrix.</param>
	public MatrixTransform(Matrix matrix)
		: this()
	{
		Matrix = matrix;
	}
}
