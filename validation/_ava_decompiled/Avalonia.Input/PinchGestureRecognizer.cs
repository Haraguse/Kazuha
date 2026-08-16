using System;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input;

public class PinchGestureRecognizer : GestureRecognizer
{
	private float _initialDistance;

	private IPointer? _firstContact;

	private Point _firstPoint;

	private IPointer? _secondContact;

	private Point _secondPoint;

	private Point _origin;

	private double _previousAngle;

	protected override void PointerCaptureLost(IPointer pointer)
	{
		RemoveContact(pointer);
	}

	protected override void PointerMoved(PointerEventArgs e)
	{
		if (!(base.Target is Visual relativeTo))
		{
			return;
		}
		if (_firstContact == e.Pointer)
		{
			_firstPoint = e.GetPosition(relativeTo);
		}
		else
		{
			if (_secondContact != e.Pointer)
			{
				return;
			}
			_secondPoint = e.GetPosition(relativeTo);
		}
		if (_firstContact != null && _secondContact != null)
		{
			float num = GetDistance(_firstPoint, _secondPoint) / _initialDistance;
			double angleDegreeFromPoints = GetAngleDegreeFromPoints(_firstPoint, _secondPoint);
			PinchEventArgs e2 = new PinchEventArgs(num, _origin, angleDegreeFromPoints, _previousAngle - angleDegreeFromPoints);
			_previousAngle = angleDegreeFromPoints;
			base.Target?.RaiseEvent(e2);
			e.Handled = e2.Handled;
			e.PreventGestureRecognition();
		}
	}

	protected override void PointerPressed(PointerPressedEventArgs e)
	{
		if (!(base.Target is Visual relativeTo) || (e.Pointer.Type != PointerType.Touch && e.Pointer.Type != PointerType.Pen))
		{
			return;
		}
		if (_firstContact == null)
		{
			_firstContact = e.Pointer;
			_firstPoint = e.GetPosition(relativeTo);
		}
		else if (_secondContact == null && _firstContact != e.Pointer)
		{
			_secondContact = e.Pointer;
			_secondPoint = e.GetPosition(relativeTo);
			if (_firstContact != null && _secondContact != null)
			{
				_initialDistance = GetDistance(_firstPoint, _secondPoint);
				_origin = new Point((_firstPoint.X + _secondPoint.X) / 2.0, (_firstPoint.Y + _secondPoint.Y) / 2.0);
				_previousAngle = GetAngleDegreeFromPoints(_firstPoint, _secondPoint);
				Capture(_firstContact);
				Capture(_secondContact);
				e.PreventGestureRecognition();
			}
		}
	}

	protected override void PointerReleased(PointerReleasedEventArgs e)
	{
		if (RemoveContact(e.Pointer))
		{
			e.PreventGestureRecognition();
		}
	}

	private bool RemoveContact(IPointer pointer)
	{
		if (_firstContact == pointer || _secondContact == pointer)
		{
			if (_secondContact == pointer)
			{
				_secondContact = null;
			}
			if (_firstContact == pointer)
			{
				_firstContact = _secondContact;
				_secondContact = null;
			}
			base.Target?.RaiseEvent(new PinchEndedEventArgs());
			return true;
		}
		return false;
	}

	private static float GetDistance(Point a, Point b)
	{
		Point point = b - a;
		return (float)new Vector(point.X, point.Y).Length;
	}

	private static double GetAngleDegreeFromPoints(Point a, Point b)
	{
		double y = a.X - b.X;
		double x = 0.0 - (a.Y - b.Y);
		return Math.Atan2(y, x) * (180.0 / Math.PI) + 180.0;
	}
}
