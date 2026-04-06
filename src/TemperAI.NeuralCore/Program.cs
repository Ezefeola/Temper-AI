using TemperAI.NeuralCore.Mcp;

// Test-ping mode for connectivity verification
if (args.Contains("--test-ping"))
{
    Console.Error.WriteLine("NeuralCore OK");
    return 0;
}

await McpServer.RunAsync();
return 0;
