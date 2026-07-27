using System.Net;
using System.Text;

namespace Digi.Recruitment.Module.Tests.TestDoubles
{
    /// <summary>
    /// A scripted <see cref="HttpMessageHandler"/> so the AI client can be tested
    /// against exact wire responses — including the awkward ones the real service
    /// only produces occasionally (413, a 422 with each error slug, a 200 with a
    /// bumped schema version) without needing a GPU or a 90-second wait.
    /// </summary>
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _script = new();
        private readonly List<HttpRequestMessage> _received = new();

        /// <summary>Every request that reached the handler, in order.</summary>
        public IReadOnlyList<HttpRequestMessage> Received => _received;

        /// <summary>Bodies of received requests, captured before disposal.</summary>
        public List<string> ReceivedBodies { get; } = new();

        public int CallCount => _received.Count;

        /// <summary>Queue one response. Calls consume the queue in order; the last entry repeats.</summary>
        public StubHttpMessageHandler Respond(HttpStatusCode status, string? json = null)
        {
            _script.Enqueue(_ => Build(status, json));
            return this;
        }

        /// <summary>Queue a transport-level failure (connection refused, DNS, reset).</summary>
        public StubHttpMessageHandler Throw(Exception exception)
        {
            _script.Enqueue(_ => throw exception);
            return this;
        }

        /// <summary>Queue a response built from the request, for assertions on what we sent.</summary>
        public StubHttpMessageHandler RespondWith(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _script.Enqueue(factory);
            return this;
        }

        private static HttpResponseMessage Build(HttpStatusCode status, string? json) => new(status)
        {
            Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json")
        };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _received.Add(request);

            ReceivedBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            if (_script.Count == 0)
            {
                throw new InvalidOperationException(
                    $"StubHttpMessageHandler received an unscripted call: {request.Method} {request.RequestUri}");
            }

            // Keep the final scripted response available for repeat calls (retries).
            var next = _script.Count == 1 ? _script.Peek() : _script.Dequeue();
            return next(request);
        }
    }
}
