using System.Threading.Channels;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Hands admin email ids from the request that queued them to
    /// AdminEmailDispatcher, which sends them in the background. In memory
    /// only: an id still waiting here when the API stops is marked Interrupted
    /// on the next startup rather than resent.
    /// </summary>
    public class AdminEmailQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(int adminEmailId)
        {
            if (!_channel.Writer.TryWrite(adminEmailId))
            {
                throw new InvalidOperationException("The admin email queue is closed.");
            }
        }

        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
