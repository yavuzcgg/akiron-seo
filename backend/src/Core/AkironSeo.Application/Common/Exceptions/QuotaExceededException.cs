namespace AkironSeo.Application.Common.Exceptions;

public sealed class QuotaExceededException : Exception
{
    public QuotaExceededException(string message = "Monthly token allowance exceeded. Please upgrade your subscription plan.")
        : base(message)
    {
    }
}
