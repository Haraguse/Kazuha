namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="T:System.Double" /> value using a user-defined spring formula.
/// </summary>
public class SpringEasing : Easing
{
	private readonly Spring _internalSpring;

	/// <summary>
	/// The spring mass.
	/// </summary>
	public double Mass
	{
		get
		{
			return _internalSpring.Mass;
		}
		set
		{
			_internalSpring.Mass = value;
		}
	}

	/// <summary>
	/// The spring stiffness.
	/// </summary>
	public double Stiffness
	{
		get
		{
			return _internalSpring.Stiffness;
		}
		set
		{
			_internalSpring.Stiffness = value;
		}
	}

	/// <summary>
	/// The spring damping.
	/// </summary> 
	public double Damping
	{
		get
		{
			return _internalSpring.Damping;
		}
		set
		{
			_internalSpring.Damping = value;
		}
	}

	/// <summary>
	/// The spring initial velocity.
	/// </summary>
	public double InitialVelocity
	{
		get
		{
			return _internalSpring.InitialVelocity;
		}
		set
		{
			_internalSpring.InitialVelocity = value;
		}
	}

	public SpringEasing(double mass = 0.0, double stiffness = 0.0, double damping = 0.0, double initialVelocity = 0.0)
	{
		_internalSpring = new Spring();
		Mass = mass;
		Stiffness = stiffness;
		Damping = damping;
		InitialVelocity = initialVelocity;
	}

	public SpringEasing()
	{
		_internalSpring = new Spring();
	}

	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return _internalSpring.GetSpringProgress(progress);
	}
}
