using Avalonia.Controls;
using Luminalium.Attributes;
using Luminalium.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Luminalium.Extensions.Registry;

public static class SettingsPagesRegistryExtensions
{
    public static IServiceCollection AddSettingsPage<T>(this IServiceCollection services, string? name = null)
        where T : UserControl
    {
        return services.AddMainPageTo<T>(SettingsPagesRegistryService.Items, name);
    }

    public static IServiceCollection AddSettingsPageSeparator(this IServiceCollection services)
    {
        SettingsPagesRegistryService.Items.Add(new SettingsPageInfo(true));
        return services;
    }

    public static IServiceCollection AddSettingsPageFooter<T>(this IServiceCollection services, string? name = null)
        where T : UserControl
    {
        return services.AddMainPageTo<T>(SettingsPagesRegistryService.FooterItems, name);
    }

    public static IServiceCollection AddSettingsPageFooterSeparator(this IServiceCollection services)
    {
        SettingsPagesRegistryService.FooterItems.Add(new SettingsPageInfo(true));
        return services;
    }

    private static IServiceCollection AddMainPageTo<T>(this IServiceCollection services, IList<SettingsPageInfo> list,
        string? name = null) where T : UserControl
    {
        var type = typeof(T);
        if (type.GetCustomAttributes(false).FirstOrDefault(x => x is SettingsPageInfo) is not SettingsPageInfo info)
        {
            throw new ArgumentException($"无法注册设置页面 {type.FullName}，因为设置页面没有注册信息。");
        }

        if (list.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new ArgumentException($"此设置页面id {info.Id} 已经被占用。");
        }

        if (name != null)
        {
            info.Name = name;
        }


        services.AddKeyedTransient<UserControl, T>(info.Id);
        list.Add(info);
        return services;
    }
}
