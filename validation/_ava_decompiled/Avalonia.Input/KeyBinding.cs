using System.Windows.Input;

namespace Avalonia.Input;

public class KeyBinding : AvaloniaObject
{
	public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<KeyBinding, ICommand>("Command");

	public static readonly StyledProperty<object> CommandParameterProperty = AvaloniaProperty.Register<KeyBinding, object>("CommandParameter");

	public static readonly StyledProperty<KeyGesture> GestureProperty = AvaloniaProperty.Register<KeyBinding, KeyGesture>("Gesture");

	public ICommand Command
	{
		get
		{
			return GetValue(CommandProperty);
		}
		set
		{
			SetValue(CommandProperty, value);
		}
	}

	public object CommandParameter
	{
		get
		{
			return GetValue(CommandParameterProperty);
		}
		set
		{
			SetValue(CommandParameterProperty, value);
		}
	}

	public KeyGesture Gesture
	{
		get
		{
			return GetValue(GestureProperty);
		}
		set
		{
			SetValue(GestureProperty, value);
		}
	}

	public void TryHandle(KeyEventArgs args)
	{
		KeyGesture gesture = Gesture;
		if ((object)gesture != null && gesture.Matches(args))
		{
			ICommand command = Command;
			if (command != null && command.CanExecute(CommandParameter))
			{
				args.Handled = true;
				Command.Execute(CommandParameter);
			}
		}
	}
}
