using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Orders.Processor;

public class SQSProcessorWorker : IHostedService
{

    private IAmazonSQS _sqs = null;
    private SQSProcessorOptions _options = null;    
    private IConfiguration _configuration = null;
    private ILogger<SQSProcessorWorker> _logger = null;

    public SQSProcessorWorker(IAmazonSQS sqs, IConfiguration configuration, ILogger<SQSProcessorWorker> logger, IOptions<SQSProcessorOptions> options)
    {
        _sqs = sqs;
        _logger = logger;
        _options = options.Value;
        _configuration = configuration;
    }

    private async Task FetchFromQueue(CancellationToken cancellationToken)
    {
        ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
        receiveMessageRequest.QueueUrl = _configuration["COPILOT_QUEUE_URI"] ?? _options.QueueUrl;
        receiveMessageRequest.MaxNumberOfMessages = _options.MaxNumberOfMessages;
        receiveMessageRequest.WaitTimeSeconds = _options.MessageWaitTimeSeconds;
        ReceiveMessageResponse receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);

        foreach(var message in receiveMessageResponse.Messages)
        {
            _logger.LogDebug($"Message Received: {message.MessageId}");
            // Do stuff
            var order = JsonSerializer.Deserialize<Order>(message.Body);
            _logger.LogInformation(" [x] Order Received {0}", order.OrderId);
            await Task.Delay(1000);
            _logger.LogInformation(" [x] Order Processed {0}", order.OrderId);
            await DeleteMessageAsync(message.ReceiptHandle);
        }
    }

    private async Task DeleteMessageAsync(string recieptHandle)
    {
        DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest();
        deleteMessageRequest.QueueUrl =  _configuration["COPILOT_QUEUE_URI"] ?? _options.QueueUrl;
        deleteMessageRequest.ReceiptHandle = recieptHandle;
        DeleteMessageResponse response = await _sqs.DeleteMessageAsync(deleteMessageRequest);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_configuration.GetSection("SQS").Exists() || !string.IsNullOrEmpty(_configuration["COPILOT_QUEUE_URI"])) {
            return Task.Run(async () =>
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    await FetchFromQueue(cancellationToken);
                }
            });
        }
        else
        {
            _logger.LogInformation("SQS Configuration missing. Not listening to Amazon SQS");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
