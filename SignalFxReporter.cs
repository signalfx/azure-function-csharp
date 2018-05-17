using System.Security;
using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Metrics.SignalFx.Helpers;
using com.signalfuse.metrics.protobuf;
using ProtoBuf;

namespace azurefunctionscsharp
{
    public class SignalFxReporter : ISignalFxReporter
    {
        private string apiToken;
        private IWebRequestorFactory _requestor;
		private ILogger log;

        public SignalFxReporter(string baseURI, string apiToken, int timeoutInMilliseconds, IWebRequestorFactory requestor = null)
        {
            if (requestor == null)
            {
				requestor = new WebRequestorFactory()
					.WithUri(baseURI + "/v2/datapoint")
					.WithMethod("POST")
					.WithContentType("application/x-protobuf")
					.WithHeader("X-SF-TOKEN", apiToken)
					.WithTimeout(timeoutInMilliseconds);
            }

            this._requestor = requestor;
			this.apiToken = apiToken;

        }

        public void Send(DataPointUploadMessage msg)
        {
            try
            {
                var request = _requestor.GetRequestor();
                using (var rs = request.GetWriteStream())
                {
					Serializer.Serialize(rs, msg);
                    // flush the message before disposing
                    rs.Flush();
                }
                try
                {
                    using (request.Send())
                    {
                    }
                }
                catch (SecurityException)
                {
                    log.LogInformation("API token for sending metrics to SignalFx is invalid");
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var webex = ex as WebException;
                    using (var exresp = webex.Response)
                    {
                        if (exresp != null)
                        {
                            var stream2 = exresp.GetResponseStream();
                            var reader2 = new StreamReader(stream2);
                            var errorStr = reader2.ReadToEnd();
                            log.LogInformation(errorStr);
                        }
                    }
                }
            }
        }
    }
}
