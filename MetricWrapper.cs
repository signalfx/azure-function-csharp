using System;
using System.Collections.Generic;
using com.signalfuse.metrics.protobuf;
using Microsoft.Azure.WebJobs;

namespace azurefunctionscsharp
{
	public class MetricWrapper : IDisposable
    {
		// signalfx env variables
		private static string AUTH_TOKEN = "SIGNALFX_AUTH_TOKEN";
		private static string TIMEOUT_MS = "SIGNALFX_SEND_TIMEOUT";

		// azure execution context variables
		private static string REGION_NAME = "REGION_NAME";
		private static string WEBSITE_SITE_NAME = "WEBSITE_SITE_NAME";
		private static string APP_POOL_ID = "APP_POOL_ID";

		//metric names
		protected static string METRIC_NAME_PREFIX = "azure.function.";
    protected static string METRIC_NAME_INVOCATIONS = "invocations";
    protected static string METRIC_NAME_ERRORS = "errors";
    protected static string METRIC_NAME_DURATION = "duration";

		//dimension names
		protected static string FUNCTION_NAME = "azure_function_name";
		protected static string METRIC_SOURCE = "azure_function_wrapper";
		protected static string RESOURCE_NAME = "azure_resource_name";
		protected static string REGION_DIMENSION = "azure_region";
		protected static string IS_AZURE_WRAPPER = "is_Azure_Function";
		protected static string WRAPPER_VERSION = "1.0.0";
        
		private readonly System.Diagnostics.Stopwatch watch;
		private readonly IDictionary<string, string> defaultDimensions;
		private readonly ISignalFxReporter reporter;

		public MetricWrapper(ExecutionContext context) : this(context, null)
        {
			
        }

		public MetricWrapper(ExecutionContext context, 
		                     List<Dimension> dimensions) : this(context, dimensions, GetEnvironmentVariable(AUTH_TOKEN))
		{
			
		}

        public MetricWrapper(ExecutionContext context,
		                     List<Dimension> dimensions,
		                     String authToken)
		{
			int timeoutMs = 300; //default timeout 300ms
			try {
				timeoutMs = Int32.Parse(GetEnvironmentVariable(TIMEOUT_MS));
			} catch (Exception e) {
				//ignore and use default timeout
			}

			// create endpoint
			SignalFxAzureFuncEndpoint signalFxEndpoint = new SignalFxAzureFuncEndpoint();


			// create reporter with endpoint
			reporter = new SignalFxReporter(signalFxEndpoint.ToString(), authToken, timeoutMs);

			// get default dimensions
			defaultDimensions = GetDefaultDimensions(context);
         
            // set wrapper singleton context
			MetricSender.setWrapper(this);

            
			watch = System.Diagnostics.Stopwatch.StartNew();
			sendMetricCounter(METRIC_NAME_INVOCATIONS, MetricType.COUNTER);

		}

		private void sendMetricCounter(string metricName, MetricType metricType)
		{
			Datum datum = new Datum();
			datum.intValue = 1;
            
			sendMetric(metricName, metricType, datum);
		}


		private void sendMetric(string metricName, MetricType metricType, 
		                       Datum datum)
		{
			DataPoint dp = new DataPoint();
			dp.metric = METRIC_NAME_PREFIX + metricName;
			dp.metricType = metricType;
			dp.value = datum;
            
			MetricSender.sendMetric(dp);
		}

        
		protected internal void sendMetric(DataPoint dp)
		{
			// send the metric
			AddDimensions(dp, defaultDimensions);
			DataPointUploadMessage msg = new DataPointUploadMessage();
			msg.datapoints.Add(dp);
			reporter.Send(msg);
		}
  
		public void Dispose()
		{
			//end stopwatch and send duration
			watch.Stop();
			var elapsedMs = watch.ElapsedMilliseconds;
			Datum timer = new Datum();
			timer.doubleValue = elapsedMs;
			sendMetric(METRIC_NAME_DURATION, MetricType.GAUGE, timer);
		}

        public void Error()
		{
			sendMetricCounter(METRIC_NAME_ERRORS, MetricType.COUNTER);
		}

        
        
		public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

		private static Dictionary<string, string> GetDefaultDimensions(ExecutionContext context) 
		{
			Dictionary<string, string> defaultDimensions = new Dictionary<string, string>();

			string functionName = context.FunctionName;
			string resourceName = GetEnvironmentVariable(WEBSITE_SITE_NAME);
			string resourceNameSecondary = GetEnvironmentVariable(APP_POOL_ID);
			string region = GetRegionName(GetEnvironmentVariable(REGION_NAME));
            
			functionName = string.IsNullOrEmpty(functionName) ? "undefined" : functionName;
            defaultDimensions.Add(FUNCTION_NAME, functionName);   
            
			if (!string.IsNullOrEmpty(resourceName))
			{
				defaultDimensions.Add(RESOURCE_NAME, resourceName);
			} 
			else if (!string.IsNullOrEmpty(resourceNameSecondary))
			{
				defaultDimensions.Add(RESOURCE_NAME, resourceNameSecondary);
			}
			else 
			{
				defaultDimensions.Add(RESOURCE_NAME, "undefined");
			}
            
			region = string.IsNullOrEmpty(region) ? "undefined" : region;
			defaultDimensions.Add(REGION_DIMENSION, region);

			defaultDimensions.Add("metric_source", METRIC_SOURCE);
			defaultDimensions.Add("function_wrapper_version", WRAPPER_VERSION);
			defaultDimensions.Add(IS_AZURE_WRAPPER, "true");
         
			return defaultDimensions;
		}

		protected virtual void AddDimensions(DataPoint dataPoint, IDictionary<string, string> dimensions)
        {
            foreach (KeyValuePair<string, string> entry in dimensions)
            {
                if (!string.IsNullOrEmpty(entry.Value))
                {
                    AddDimension(dataPoint, entry.Key, entry.Value);
                }
            }
        }

        protected virtual void AddDimension(DataPoint dataPoint, string key, string value)
        {
            Dimension dimension = new Dimension();
            dimension.key = key;
            dimension.value = value;
            dataPoint.dimensions.Add(dimension);
        }

		private static string GetRegionName(String region)
        {
            if (region != null)
            {
                switch (region)
                {
                    case "East US 2": return "eastus2";
                    case "West US 2": return "westus2";
                    case "South Central US": return "southcentralus";
                    case "West Central US": return "westcentralus";
                    case "East US": return "eastus";
                    case "North Central US": return "northcentralus";
                    case "North Europe": return "northeurope";
                    case "Canada East": return "canadaeast";
                    case "Central US": return "centralus";
                    case "West US": return "westus";
                    case "West Europe": return "westeurope";
                    case "Central India": return "centralindia";
                    case "Southeast Asia": return "southeastasia";
                    case "Canada Central": return "canadacentral";
                    case "Korea Central": return "koreacentral";
                    case "France Central": return "francecentral";
                    case "South India": return "southindia";
                    case "Australia East": return "australiaeast";
                    case "Australia Southeast": return "australiasoutheast";
                    case "Japan West": return "japanwest";
                    case "UK West": return "ukwest";
                    case "UK South": return "uksouth";
                    case "Japan East": return "japaneast";
                    case "East Asia": return "eastasia";
                    case "Brazil South": return "brazilsouth";
                    default: return null;
                }
            }
            return null;
        }
    }
}
