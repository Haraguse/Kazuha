namespace Avalonia.PropertyStore;

internal enum FramePriority : sbyte
{
	Animation,
	AnimationTemplatedParentTheme,
	AnimationTheme,
	StyleTrigger,
	StyleTriggerTemplatedParentTheme,
	StyleTriggerTheme,
	Template,
	TemplateTemplatedParentTheme,
	TemplateTheme,
	Style,
	StyleTemplatedParentTheme,
	StyleTheme
}
