namespace Nyx.Orleans.Jobs;

[GenerateSerializer]
[Alias("Nyx.Orleans.Jobs.JobErrorInformationDetails")]
public record JobErrorInformationDetails(
    [property: Id(0)] string ExceptionTypeName,
    [property: Id(1)] string Message,
    [property: Id(2)] string Source,
    [property: Id(3)] string StackTrace);

[GenerateSerializer]
[Alias("Nyx.Orleans.Jobs.JobErrorInformation")]
public record JobErrorInformation(
    string ExceptionTypeName,
    string Message,
    string Source,
    string StackTrace,
    [property: Id(10)]  JobErrorInformationDetails? InnerException = null
    ) 
    : JobErrorInformationDetails(ExceptionTypeName, Message, Source, StackTrace)
{
    public static readonly JobErrorInformation Empty =
        new JobErrorInformation(string.Empty, string.Empty, string.Empty, string.Empty);
}