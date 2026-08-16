using System.ComponentModel;
using Luminalium.Models.Config;
using Microsoft.Extensions.Logging;

namespace Luminalium.Services.Config;

public abstract class ConfigHandlerBase<T> where T : ConfigBase
{
    protected ConfigHandlerBase(Func<T> fallbackFactory)
    {
        Logger = (ILogger)IAppHost.Host?.Services.GetService(typeof(ILogger<>).MakeGenericType(GetType()))!;
        ConfigService = IAppHost.GetService<ConfigServiceBase>();
        FallbackFactory = fallbackFactory;

        Logger.LogInformation("加载配置文件...");
        Data = ConfigService.LoadConfig(FallbackFactory());
        Data.PropertyChanged += Data_OnPropertyChanged;
    }

    protected ConfigHandlerBase(ILogger logger, ConfigServiceBase configService, Func<T> fallbackFactory)
    {
        Logger = logger;
        ConfigService = configService;
        FallbackFactory = fallbackFactory;

        Logger.LogInformation("加载配置文件...");
        Data = ConfigService.LoadConfig(FallbackFactory());
        Data.PropertyChanged += Data_OnPropertyChanged;
    }

    public T Data { get; private set; }
    public event EventHandler? Reloaded;

    private ILogger Logger { get; }
    private ConfigServiceBase ConfigService { get; }
    private Func<T> FallbackFactory { get; }

    public virtual void Reload()
    {
        Data.PropertyChanged -= Data_OnPropertyChanged;
        Logger.LogInformation("重载配置文件...");
        Data = ConfigService.LoadConfig(FallbackFactory());
        Data.PropertyChanged += Data_OnPropertyChanged;
        Reloaded?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Save()
    {
        Logger.LogInformation("保存配置文件...");
        ConfigService.SaveConfig(Data);
    }

    public virtual void Delete()
    {
        Logger.LogInformation("删除配置文件...");
        ConfigService.DeleteConfig(Data);
    }

    protected virtual void Data_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
    }
}
