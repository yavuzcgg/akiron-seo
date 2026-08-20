using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AkironSeo.API.Validation;

public static class ValidationEndpointExtensions
{
    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (request is null)
            {
                return await next(context);
            }

            var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();
            var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
            if (result.IsValid)
            {
                return await next(context);
            }

            var errors = result.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => char.ToLowerInvariant(group.Key[0]) + group.Key[1..],
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

            var problem = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more request fields are invalid.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["correlationId"] = context.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
            problem.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

            return Results.Json(
                problem,
                statusCode: StatusCodes.Status400BadRequest,
                contentType: "application/problem+json");
        });
    }
}
