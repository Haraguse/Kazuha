using System;

namespace Avalonia.Utilities;

internal struct SpringSolver
{
	private readonly double m_w0;

	private readonly double m_zeta;

	private readonly double m_wd;

	private readonly double m_A;

	private readonly double m_B;

	/// <summary>
	///
	/// </summary>
	/// <param name="period">The time period.</param>
	/// <param name="zeta">The damping ratio.</param>
	/// <param name="initialVelocity"></param>
	public SpringSolver(TimeSpan period, double zeta, double initialVelocity)
		: this(Math.PI * 2.0 / period.TotalSeconds, zeta, initialVelocity)
	{
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="m">The mass of the oscillating body.</param>
	/// <param name="k">The stiffness of the oscillated body (spring constant).</param>
	/// <param name="c">The actual damping.</param>
	/// <param name="initialVelocity">The initial velocity.</param>
	public SpringSolver(double m, double k, double c, double initialVelocity)
		: this(Math.Sqrt(k / m), c / (2.0 * Math.Sqrt(k * m)), initialVelocity)
	{
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="ωn">The natural frequency of the system [rad/s].</param>
	/// <param name="zeta">The damping ratio.</param>
	/// <param name="initialVelocity"></param>
	public SpringSolver(double ωn, double zeta, double initialVelocity)
	{
		m_w0 = ωn;
		m_zeta = zeta;
		if (m_zeta < 1.0)
		{
			m_wd = m_w0 * Math.Sqrt(1.0 - m_zeta * m_zeta);
			m_A = 1.0;
			m_B = (m_zeta * m_w0 + (0.0 - initialVelocity)) / m_wd;
		}
		else
		{
			m_A = 1.0;
			m_B = 0.0 - initialVelocity + m_w0;
			m_wd = 0.0;
		}
	}

	public readonly double Solve(double t)
	{
		t = ((!(m_zeta < 1.0)) ? ((m_A + m_B * t) * Math.Exp((0.0 - t) * m_w0)) : (Math.Exp((0.0 - t) * m_zeta * m_w0) * (m_A * Math.Cos(m_wd * t) + m_B * Math.Sin(m_wd * t))));
		return 1.0 - t;
	}
}
