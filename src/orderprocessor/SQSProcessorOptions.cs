namespace Orders.Processor;

public class SQSProcessorOptions
{
    public string QueueUrl { get; set; }
    public int MaxNumberOfMessages { get; set; } = 1;
    public int MessageWaitTimeSeconds { get; set; } = 10;
}