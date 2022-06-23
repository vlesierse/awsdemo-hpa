using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging.SNS;

public class SNSTopic : ITopic
{

    private readonly IAmazonSimpleNotificationService _sns;
    private readonly SNSTopicOptions _options;

    public SNSTopic(IAmazonSimpleNotificationService sns, IOptions<SNSTopicOptions> options)
    {
        _sns = sns;
        _options = options.Value;
    }

    public async Task Publish<T>(T message)
    {
        var request = new PublishRequest {
            TopicArn = _options.TopicArn,
            Message = JsonSerializer.Serialize(message)
        };
        _ = await _sns.PublishAsync(request);
    }
}