using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SunSkog.Api.Filters
{
    /// <summary>
    /// Minimal API filter: vezme DTO z argumentů, spustí FluentValidation
    /// a při chybách vrátí 400 ValidationProblem bez volání handleru.
    /// </summary>
    public sealed class Validate<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
        {
            var model = ctx.Arguments.FirstOrDefault(a => a is T) as T;
            if (model is null) return await next(ctx);

            var validator = ctx.HttpContext.RequestServices.GetService(typeof(IValidator<T>)) as IValidator<T>;
            if (validator is null) return await next(ctx);

            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            return await next(ctx);
        }
    }
}