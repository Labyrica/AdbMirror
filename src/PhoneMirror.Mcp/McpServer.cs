using System.Text;
using System.Text.Json;

namespace PhoneMirror.Mcp;

/// <summary>
/// MCP server that handles JSON-RPC messages over stdin/stdout.
/// Follows the Model Context Protocol specification (2024-11-05).
/// </summary>
public sealed class McpServer
{
    private readonly AdbToolHandler _toolHandler;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly object _writeLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public McpServer(AdbToolHandler toolHandler, TextReader? input = null, TextWriter? output = null)
    {
        _toolHandler = toolHandler;
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Ensure stdout is UTF-8 with no BOM
        if (_output == Console.Out)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);
        }

        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await _input.ReadLineAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line == null)
            {
                // stdin closed
                break;
            }

            line = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            JsonRpcRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOptions);
            }
            catch (JsonException)
            {
                SendError(null, -32700, "Parse error");
                continue;
            }

            if (request == null)
            {
                SendError(null, -32600, "Invalid request");
                continue;
            }

            await HandleRequestAsync(request, ct);
        }
    }

    private async Task HandleRequestAsync(JsonRpcRequest request, CancellationToken ct)
    {
        try
        {
            switch (request.Method)
            {
                case "initialize":
                    SendResult(request.Id, new InitializeResult());
                    break;

                case "notifications/initialized":
                    // Notification - no response needed
                    break;

                case "tools/list":
                    SendResult(request.Id, new ToolsListResult
                    {
                        Tools = _toolHandler.GetToolDefinitions()
                    });
                    break;

                case "tools/call":
                    await HandleToolCallAsync(request, ct);
                    break;

                case "ping":
                    SendResult(request.Id, new { });
                    break;

                default:
                    if (!request.IsNotification)
                    {
                        SendError(request.Id, -32601, $"Method not found: {request.Method}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            if (!request.IsNotification)
            {
                SendError(request.Id, -32603, $"Internal error: {ex.Message}");
            }
        }
    }

    private async Task HandleToolCallAsync(JsonRpcRequest request, CancellationToken ct)
    {
        ToolCallParams? toolParams = null;
        if (request.Params.HasValue)
        {
            toolParams = JsonSerializer.Deserialize<ToolCallParams>(
                request.Params.Value.GetRawText(), JsonOptions);
        }

        if (toolParams == null || string.IsNullOrEmpty(toolParams.Name))
        {
            SendError(request.Id, -32602, "Invalid params: missing tool name");
            return;
        }

        var result = await _toolHandler.ExecuteAsync(
            toolParams.Name,
            toolParams.Arguments,
            ct);

        SendResult(request.Id, result);
    }

    private void SendResult(JsonElement? id, object result)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Result = result
        };
        SendMessage(response);
    }

    private void SendError(JsonElement? id, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        SendMessage(response);
    }

    private void SendMessage(JsonRpcResponse response)
    {
        var json = JsonSerializer.Serialize(response, JsonOptions);
        lock (_writeLock)
        {
            _output.WriteLine(json);
            _output.Flush();
        }
    }
}
