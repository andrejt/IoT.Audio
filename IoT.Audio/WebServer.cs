using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IoT.Audio
{
    internal class Webserver
    {
        private const uint BufferSize = 8192;
        private StreamSocketListener listener;
        private List<Type> controllers;

        public async Task StartAsync()
        {
            FindControllers();
            listener = new StreamSocketListener();

            listener.ConnectionReceived += async (sender, args) =>
            {
                byte[] responseBodyBytes = null;
                string contentType = null;
                string responseCode = null;

                try
                {
                    var request = new StringBuilder();
                    using (var input = args.Socket.InputStream)
                    {
                        var data = new byte[BufferSize];
                        var buffer = data.AsBuffer();
                        var dataRead = BufferSize;

                        while (dataRead == BufferSize)
                        {
                            await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                            dataRead = buffer.Length;
                            request.Append(Encoding.UTF8.GetString(data, 0, (int) dataRead));
                        }
                    }

                    Debug.WriteLine("Request received:\r\n{request}\r\n");
                    var query = await ProcessRequestAsync(request);

                    var response1 = query.Replace("\r\n", "<br/>");
                    if (query.StartsWith("{"))
                    {
                        contentType = "Content-Type: application/json; charset=utf-8\r\n";
                        responseBodyBytes = Encoding.UTF8.GetBytes(query);
                    }
                    else
                    {
                        contentType = "Content-Type: text/html; charset=utf-8\r\n";
                        responseBodyBytes = Encoding.UTF8.GetBytes($"<html><head><title>My Home on Raspberry Pi IoT Core</title></head><body>{response1}</body></html>");
                    }
                    responseCode = "200 OK";
                }
                catch (HttpRequestException e)
                {
                    contentType = "Content-Type: application/json; charset=utf-8\r\n";
                    responseBodyBytes = Encoding.UTF8.GetBytes(String.Empty);
                    responseCode = e.Message;
                }
                catch (Exception e)
                {
                    var errorBody = $"{{\"Error\": \"{e.Message}\"}}";
                    contentType = "Content-Type: application/json; charset=utf-8\r\n";
                    responseBodyBytes = Encoding.UTF8.GetBytes(errorBody);
                    responseCode = "500 Internal Server Error";
                }
                finally
                {
                    using (var output = args.Socket.OutputStream)
                    {
                        using (var response = output.AsStreamForWrite())
                        {
                            using (var bodyStream = new MemoryStream(responseBodyBytes))
                            {
                                var header = $"HTTP/1.1 {responseCode}\r\n{contentType}Content-Length: {bodyStream.Length}\r\nConnection: close\r\n\r\n";
                                var headerArray = Encoding.UTF8.GetBytes(header);
                                await response.WriteAsync(headerArray, 0, headerArray.Length);
                                await bodyStream.CopyToAsync(response);
                                await response.FlushAsync();
                            }
                        }
                    }
                }
            };
            await listener.BindServiceNameAsync("8085");
        }

        private void FindControllers()
        {
            var assembly = GetType().GetTypeInfo().Assembly;
            var derivedType = typeof(Controller);
            controllers = assembly
                .GetTypes()
                .Where(t =>
                    t != derivedType &&
                    derivedType.IsAssignableFrom(t)
                ).ToList();
        }

        private async Task<string> ProcessRequestAsync(StringBuilder request)
        {
            var requestLines = request.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var verb = requestLines[0];

            var url = requestLines.Length > 1 ? requestLines[1] : String.Empty;
            var uri = new Uri("http://localhost" + url);
            var pathParts = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split(new[] { '/' });
            var controllerName = pathParts[0];
            var action = pathParts.Length > 1 ? pathParts[1] : String.Empty;

            var controllerType = controllers.SingleOrDefault(i => String.Compare(i.Name, $"{controllerName}Controller", StringComparison.OrdinalIgnoreCase) == 0);
            if (controllerType == null)
            {
                throw new HttpRequestException("404 Not Found");
            }
            var invokeMethodName = $"{verb}{action}";
            var methodInfo = controllerType.GetMethod(invokeMethodName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo == null)
            {
                throw new HttpRequestException("404 Not Found");
            }

            var controllerInstance = ActivatorUtilities.CreateInstance(StartupTask.Services, controllerType);
            if (controllerInstance == null)
            {
                throw new HttpRequestException("404 Not Found");
            }

            var invokeParameters = ResolveParameters(uri.Query, methodInfo);

            var response = await (dynamic) methodInfo.Invoke(controllerInstance, invokeParameters);

            return JsonConvert.SerializeObject(response,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }

        private static object[] ResolveParameters(string query, MethodBase methodInfo)
        {
            var queryParameters = String.IsNullOrEmpty(query) ? null : new WwwFormUrlDecoder(query);
            object[] invokeParameters;
            if (queryParameters != null)
            {
                var parameters = methodInfo.GetParameters();
                invokeParameters = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    invokeParameters[i] = queryParameters.GetFirstValueByName(parameters[i].Name);
                }
            }
            else
            {
                invokeParameters = new object[0];
            }
            return invokeParameters;
        }
    }

    internal class WebServerResponse
    {
        public static WebServerResponse1<T> CreateOk<T>(T result)
        {
            return new WebServerResponse1<T> { Result = result };
        }

        public static WebServerResponse1<T> CreateError<T>(string errorMessage)
        {
            return new WebServerResponse1<T> { Error = errorMessage };
        }
    }

    internal class WebServerResponse1<T> : WebServerResponse
    {
        public T Result { get; set; }
        public string Error { get; set; }
    }
}
