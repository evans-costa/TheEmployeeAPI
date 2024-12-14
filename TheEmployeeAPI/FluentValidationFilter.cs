﻿using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TheEmployeeAPI;

public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _serviceProvider = serviceProvider;
        _problemDetailsFactory = problemDetailsFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context,
        ActionExecutionDelegate
            next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
            if (context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue) &&
                argumentValue != null)
            {
                var argumentType = argumentValue.GetType();

                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

                var validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationResult = await validator.ValidateAsync(new
                        ValidationContext<object>(argumentValue));

                    if (!validationResult.IsValid)
                    {
                        validationResult.AddToModelState(context.ModelState);

                        var problemDetails = _problemDetailsFactory
                            .CreateValidationProblemDetails(context.HttpContext,
                                context.ModelState);
                        context.Result = new BadRequestObjectResult(problemDetails);

                        return;
                    }
                }
            }

        await next();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
