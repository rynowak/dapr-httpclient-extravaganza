using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dapr.Client
{
    public class ServiceInvocationHandler : DelegatingHandler
    {
        private readonly HashSet<string> _allowedAppIds;
        private int? port;

        public ServiceInvocationHandler()
            : base()
        {
            _allowedAppIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public ServiceInvocationHandler(HttpMessageHandler innerHandler) 
            : base(innerHandler)
        {
            _allowedAppIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public ISet<string> AllowedAppIds => _allowedAppIds;

        private int Port 
        {
            get
            {
                if (port.HasValue)
                {
                    return port.Value;
                }

                var text = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
                if (!string.IsNullOrEmpty(text) && int.TryParse(text, out var p))
                {
                    port = p;
                    return port.Value;
                }

                port = 3500;
                return port.Value;
            }
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!TryRewiteUrl(request, out var original))
            {
                return base.Send(request, cancellationToken);
            }

            try
            {
                return base.Send(request, cancellationToken);
            }
            finally
            {
                request.RequestUri = original;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!TryRewiteUrl(request, out var original))
            {
                // TODO: can avoid extra state machine on this path if desired.
                return await base.SendAsync(request, cancellationToken);
            }

            try
            {
                Console.WriteLine("URL is: " + request.RequestUri);
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                request.RequestUri = original;
            }
        }

        private bool TryRewiteUrl(HttpRequestMessage request, [NotNullWhen(true)] out Uri? original)
        {
            if (request.RequestUri is null)
            {
                // do nothing
                original = null;
                return false;
            }
            
            if (_allowedAppIds.Count > 0 && 
                !_allowedAppIds.Contains(request.RequestUri.Host))
            {
                // do nothing
                original = null;
                return false; 
            }

            original = request.RequestUri;

            var builder = new UriBuilder(original)
            {
                Host = "localhost",
                Port = this.Port,
                Path = $"/v1.0/invoke/{original.Host}/method" + original.AbsolutePath,
            };
            request.RequestUri = builder.Uri;
            return true;
        }
    }
}
