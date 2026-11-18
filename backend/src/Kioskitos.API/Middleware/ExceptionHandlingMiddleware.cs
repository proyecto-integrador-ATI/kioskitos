namespace Kioskitos.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
            when (!context.Response.HasStarted
                  && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Unexpected error. TraceId: {TraceId}",
                context.TraceIdentifier);

            context.Response.Clear();

            await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                type: "about:blank",
                title: "Internal server error",
                detail: "An unexpected error occurred. Please try again later.",
                instance: context.Request.Path.Value,
                extensions: new Dictionary<string, object?>
                {
                    ["traceId"] = context.TraceIdentifier
                })
                .ExecuteAsync(context);
        }
    }
}