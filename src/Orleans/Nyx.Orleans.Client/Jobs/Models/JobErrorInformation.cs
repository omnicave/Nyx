namespace Nyx.Orleans.Jobs;

public record JobErrorInformationDetails(
    string ExceptionTypeName,
    string Message,
    string Source,
    string StackTrace);

public record JobErrorInformation(
    string ExceptionTypeName,
    string Message,
    string Source,
    string StackTrace,
    JobErrorInformationDetails? InnerException = null
    ) 
    : JobErrorInformationDetails(ExceptionTypeName, Message, Source, StackTrace)
{
    public static readonly JobErrorInformation Empty =
        new JobErrorInformation(string.Empty, string.Empty, string.Empty, string.Empty);
}