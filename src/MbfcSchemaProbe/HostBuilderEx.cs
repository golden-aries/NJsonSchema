using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Hosting;
public static class HostBuilderEx
{
    /// <summary>
    /// Method is taken from Microsoft's HostBuilder.ConfigureDefaults
    /// source of which Licensed to the .NET Foundation under one or more agreements.
    /// The .NET Foundation licenses this file to you under the MIT license.
    /// and modified in a way that for the Host's AppConfiguraion
    /// Environment Variables Provider accepts only variables named with prefix: $"{hostingContext.HostingEnvironment.ApplicationName}_"
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting/src/HostBuilder.cs
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IHostBuilder ConfigureCustomHostBuilder(this IHostBuilder builder, string[] args)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Logging:LogLevel:Default", "Warning")
                });

            config.AddEnvironmentVariables(prefix: "DOTNET_");

            if (args is { Length: > 0 })
            {
                config.AddCommandLine(args);
            }
        });

        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            IHostEnvironment env = hostingContext.HostingEnvironment;

            bool reloadOnChange = GetReloadConfigOnChangeValue(hostingContext);

            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: reloadOnChange)
                       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);

            if (env.IsDevelopment() && env.ApplicationName is { Length: > 0 })
            {
                var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                if (appAssembly is not null)
                {
                    config.AddUserSecrets(appAssembly, optional: true);
                }
            }

            if (hostingContext.HostingEnvironment.IsProduction())
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".customHostBuilderDemo",
                    "appsettings.json");
                config.AddJsonFile(path, optional: true);
            }

            config.AddEnvironmentVariables(prefix: $"{hostingContext.HostingEnvironment.ApplicationName}_");

            if (args is { Length: > 0 })
            {
                config.AddCommandLine(args);
            }
        })
        .ConfigureLogging((hostingContext, logging) =>
        {
            bool isWindows =
#if NET6_0_OR_GREATER
                    OperatingSystem.IsWindows();
#else
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

            // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
            // the defaults be overridden by the configuration.
            if (isWindows)
            {
                // Default the EventLogLoggerProvider to warning or above
                logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
            }

            logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
#if NET6_0_OR_GREATER
            if (!OperatingSystem.IsBrowser())
#endif
            {
                logging.AddConsole();
            }
            logging.AddDebug();
            logging.AddEventSourceLogger();

            if (isWindows)
            {
                // Add the EventLogLoggerProvider on windows machines
                logging.AddEventLog();
            }

            logging.Configure(options =>
            {
                options.ActivityTrackingOptions =
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.ParentId;
            });

        })
        .UseDefaultServiceProvider((context, options) =>
        {
            bool isDevelopment = context.HostingEnvironment.IsDevelopment();
            options.ValidateScopes = isDevelopment;
            options.ValidateOnBuild = isDevelopment;
        });

        return builder;
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Calling IConfiguration.GetValue is safe when the T is bool.")]
        static bool GetReloadConfigOnChangeValue(HostBuilderContext hostingContext) => hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);
    }
}
