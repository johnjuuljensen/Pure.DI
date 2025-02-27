﻿// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ContextualLoggerProblem
// ReSharper disable TemplateIsNotCompileTimeConstantProblem
namespace BlazorServerApp;

using System.Diagnostics.CodeAnalysis;
using Clock.Models;
using Microsoft.Extensions.Logging;

internal class AspNetLog<T> : ILog<T>
{
    private readonly ILogger<T> _logger;

    public AspNetLog(ILogger<T> logger) => _logger = logger;

    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    public void Info(string message) => _logger.Log(LogLevel.Information, message);
}