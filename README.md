# SignalFx C# Azure Function Wrapper

SignalFx C# Azure Function Wrapper.

## Usage

The SignalFx C# Azure Function Wrapper is a wrapper around an Azure Function, used to instrument execution of the function and send metrics to SignalFx.

### Install via NuGet
Add the following package reference to your `.csproj` or `function.proj`
```xml
  <PackageReference Include="signalfx-azure-functions" Version="1.0.0"/>
```


### Using the Metric Wrapper

Create a MetricWrapper with the ExecutionContext
Wrap your code in try-catch-finally, disposing of the wrapper finally.
```cs
using azurefunctioncsharp

...

    [FunctionName("HttpTrigger")]
		public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");
            MetricWrapper wrapper = new MetricWrapper(context);
            try { 
                ...
                // your code
                ...
                return ResponseObject
            } catch (Exception e) {
              wrapper.Error();
            } finally {
              wrapper.Dispose();
            }

        }
```

### Environment Variable
Set the Azure Function environment variables as follows:

1) Set authentication token:
```
 SIGNALFX_AUTH_TOKEN=signalfx token
```
2) Optional parameters available:
```
 SIGNALFX_API_HOSTNAME=[pops.signalfx.com]
 SIGNALFX_API_PORT=[443]
 SIGNALFX_API_SCHEME=[https]
 SIGNALFX_SEND_TIMEOUT=milliseconds for signalfx client timeout [2000]
```

### Metrics and dimensions sent by the wrapper

The Azure Function Wrapper sends the following metrics to SignalFx:

| Metric Name  | Type | Description |
| ------------- | ------------- | ---|
| azure.function.invocations  | Counter  | Count number of function invocations|
| azure.function.errors  | Counter  | Count number of errors from underlying function|
| azure.function.duration  | Gauge  | Milliseconds in execution time of underlying function|

The function wrapper adds the following dimensions to all data points sent to SignalFx:

| Dimension | Description |
| ------------- | ---|
| azure_region  | Azure Region where the function is executed  |
| azure_function_name  | Name of the function |
| azure_resource_name  | Name of the function app where the function is running |
| function_wrapper_version  | SignalFx Function Wrapper qualifier (e.g. signalfx-azurefunction-0.0.11) |
| is_Azure_Function  | Used to differentiate between Azure App Service and Azure Function metrics |
| metric_source | The literal value of 'azure_function_wrapper' |

### Sending a custom metric from the Azure Function
```cs
using com.signalfuse.metrics.protobuf;

// construct a data point
DataPoint dp = new DataPoint();

// use Datum to set the value
Datum datum = new Datum();
datum.intValue = 1;

// set the name, value, and metric type on the datapoint

dp.metric = "metric_name";
dp.metricType = MetricType.GAUGE;
dp.value = datum;

// add custom dimension
Dimension dim = new Dimension();
dim.key = "applicationName";
dim.value = "CoolApp";
dp.dimensions.Add(dim);

// send the metric
MetricSender.sendMetric(dp);
```

### Testing locally.
1) Follow the Azure instructions to run functions locally https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-csharp

2) Install as shown above by adding the dependency to `function.proj` or `.csproj` 


## License

Apache Software License v2. Copyright © 2014-2017 SignalFx
