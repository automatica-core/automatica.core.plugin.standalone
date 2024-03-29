﻿using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace Automatica.Core.Plugin.Standalone
{
    internal class ConsoleLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger(Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information"));
        }

        public void AddProvider(ILoggerProvider provider)
        {
            
        }
    }
    internal class ConsoleLogger : ILogger
    {
        private readonly LogLevel _logLevel;

        public ConsoleLogger(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var msg = formatter(state, exception);
            Console.WriteLine($"{DateTime.Now}: {msg}");   
        }
    }

    class Program
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        static async Task Main(string[] args)
        {

            // this calls are only that the we get all of our needed - i know it is a dirty workaround but necessary
            var dbName = EF.Helper.DatabaseConstants.DatabaseInitName;
            Console.WriteLine($"{dbName}");
            var bitValue = Driver.Utility.Utils.BitValue(1, 1);
            Console.WriteLine($"{bitValue}");


           


            var logger = new ConsoleLogger(Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information"));

            logger.LogInformation($"Starting...Version {ServerInfo.GetServerVersion()}, Datetime {ServerInfo.StartupTime}. Running .NET Core Version {GetNetCoreVersion()}");

            while (true)
            {
                try
                {
                    await Semaphore.WaitAsync();

                    logger.LogInformation($"Run plugin ({NetStandardUtils.Version.GetAssemblyVersion()})");

                    var execContext = new ExecutionContext(logger, args[0]);

                    if (!await execContext.Start())
                    {
                        logger.LogError("Error init connections...retry in 10sec");
                        Semaphore.Release(1);
                        await Task.Delay(10000);
                        
                        continue;
                    }

                    await execContext.Run();
                    Semaphore.Release(1);
                }
                catch (Exception e)
                {
                    Semaphore.Release(1);
                    Console.Error.WriteLine($"Error occured, retry in 10 sec\n{e}");
                    await Task.Delay(10000);
                }
            }
        }

        public static string GetNetCoreVersion()
        {
            var framework = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;
            return framework;
        }
    }
}
