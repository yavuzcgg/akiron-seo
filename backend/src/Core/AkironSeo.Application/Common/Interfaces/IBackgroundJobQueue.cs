namespace AkironSeo.Application.Common.Interfaces;

public interface IBackgroundJobQueue
{
    ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem);
    ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
