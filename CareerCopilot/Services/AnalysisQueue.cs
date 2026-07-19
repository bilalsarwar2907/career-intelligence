using System.Threading.Channels;

namespace CareerCopilot.Services;

/// <summary>
/// Simple in-memory queue. The UI enqueues job IDs;
/// AnalysisWorker processes them in the background.
/// </summary>
public class AnalysisQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int jobId) => _channel.Writer.TryWrite(jobId);
    public ChannelReader<int> Reader => _channel.Reader;
    public int PendingCount => _channel.Reader.Count;
}
