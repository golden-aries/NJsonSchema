using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NJsonSchema;

var hostBuilder = new HostBuilder();
hostBuilder.ConfigureCustomHostBuilder(args).UseConsoleLifetime();
var host = hostBuilder.Build();
var env = host.Services.GetRequiredService<IHostEnvironment>();

var fp = env.ContentRootFileProvider;
var fileName = "Schemas/empty.schema.jsonc";
//var fileName = "Schemas/mbfc_sdk.schema.jsonc";
//var fileName = "Schemas/draft-07.schema.jsonc";
//var fileName = "Schemas/draft-04.schema.jsonc";

var fi = fp.GetFileInfo(fileName);
using var reader = new StreamReader(fi.CreateReadStream());
var srcJson = await reader.ReadToEndAsync();

var schema = await JsonSchema.FromJsonAsync(fi.CreateReadStream());
var ver = schema.SchemaVersion;
var df = schema.Default;
schema.SchemaVersion = "http://json-schema.org/draft-07/schema#";
var dstJson = schema.ToJson();
var equal = string.Equals(srcJson, dstJson);
Console.WriteLine("Equal: " + equal);