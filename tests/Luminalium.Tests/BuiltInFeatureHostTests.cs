using Luminalium.App.Features;
using Luminalium.App.Services;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureHostTests
{
    [Fact]
    public async Task FreshFeatureCreatesOneInstancePerSerializedActivation()
    {
        var factory = new RecordingFactory();
        await using var host = Host(BuiltInFeatureId.Timer, factory);

        var results = await Task.WhenAll(
            host.ActivateAsync(BuiltInFeatureId.Timer),
            host.ActivateAsync(BuiltInFeatureId.Timer));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(2, factory.CreateCount);
        Assert.All(factory.Instances, instance => Assert.Equal(1, instance.ActivateCount));
    }

    [Fact]
    public async Task SharedFeatureCoalescesAndReactivatesOneInstance()
    {
        var factory = new RecordingFactory();
        await using var host = Host(BuiltInFeatureId.Settings, factory);

        var first = await host.ActivateAsync(BuiltInFeatureId.Settings);
        var second = await host.ActivateAsync(BuiltInFeatureId.Settings);

        Assert.True(first.IsSuccess);
        Assert.Equal(BuiltInFeatureActivationDisposition.Coalesced, second.Disposition);
        Assert.Equal(1, factory.CreateCount);
        Assert.Equal(2, Assert.Single(factory.Instances).ActivateCount);
    }

    [Fact]
    public async Task DispatcherOwnsCreationActivationAndClose()
    {
        var dispatcher = new RecordingDispatcher();
        var factory = new RecordingFactory();
        await using var host = Host(BuiltInFeatureId.Timer, factory, dispatcher);

        await host.ActivateAsync(BuiltInFeatureId.Timer);
        await host.BeginShutdownAsync();

        Assert.Equal(2, dispatcher.InvocationCount);
        Assert.Equal(1, Assert.Single(factory.Instances).CloseCount);
    }

    [Fact]
    public async Task ShutdownRejectsActivationWithoutInvokingFactory()
    {
        var factory = new RecordingFactory();
        await using var host = Host(BuiltInFeatureId.Timer, factory);
        await host.BeginShutdownAsync();

        var result = await host.ActivateAsync(BuiltInFeatureId.Timer);

        Assert.False(result.IsSuccess);
        Assert.Equal(BuiltInFeatureActivationErrorCode.ShutdownInProgress, result.Error!.Code);
        Assert.Equal(0, factory.CreateCount);
    }

    [Fact]
    public async Task ConstructionFailureReturnsStructuredFailureWithoutCachingPartialInstance()
    {
        var factory = new RecordingFactory(throwOnCreate: true);
        await using var host = Host(BuiltInFeatureId.Settings, factory);

        var first = await host.ActivateAsync(BuiltInFeatureId.Settings);
        var second = await host.ActivateAsync(BuiltInFeatureId.Settings);

        Assert.Equal(BuiltInFeatureActivationErrorCode.InitializationFailed, first.Error!.Code);
        Assert.Equal(BuiltInFeatureActivationErrorCode.InitializationFailed, second.Error!.Code);
        Assert.Equal(2, factory.CreateCount);
    }

    [Fact]
    public async Task ShutdownAndDisposeCloseEachInstanceExactlyOnce()
    {
        var factory = new RecordingFactory();
        var host = Host(BuiltInFeatureId.Timer, factory);
        await host.ActivateAsync(BuiltInFeatureId.Timer);
        await host.BeginShutdownAsync();
        await host.BeginShutdownAsync();
        await host.DisposeAsync();

        Assert.Equal(1, Assert.Single(factory.Instances).CloseCount);
    }

    private static BuiltInFeatureHost Host(BuiltInFeatureId id, RecordingFactory factory, RecordingDispatcher? dispatcher = null) =>
        new(BuiltInFeatureCatalog.Default, dispatcher ?? new RecordingDispatcher(), new Dictionary<BuiltInFeatureId, IBuiltInFeatureFactory> { [id] = factory });

    private sealed class RecordingDispatcher : IBuiltInFeatureDispatcher
    {
        public int InvocationCount { get; private set; }
        public async Task<T> InvokeAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return await operation();
        }
    }

    private sealed class RecordingFactory(bool throwOnCreate = false) : IBuiltInFeatureFactory
    {
        public int CreateCount { get; private set; }
        public List<RecordingInstance> Instances { get; } = [];
        public Task<IBuiltInFeatureInstance> CreateAsync(CancellationToken cancellationToken)
        {
            CreateCount++;
            if (throwOnCreate)
            {
                throw new InvalidOperationException("secret construction detail");
            }

            var instance = new RecordingInstance();
            Instances.Add(instance);
            return Task.FromResult<IBuiltInFeatureInstance>(instance);
        }
    }

    private sealed class RecordingInstance : IBuiltInFeatureInstance
    {
        public int ActivateCount { get; private set; }
        public int CloseCount { get; private set; }
        public Task ActivateAsync(CancellationToken cancellationToken)
        {
            ActivateCount++;
            return Task.CompletedTask;
        }
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            CloseCount++;
            return Task.CompletedTask;
        }
    }
}
