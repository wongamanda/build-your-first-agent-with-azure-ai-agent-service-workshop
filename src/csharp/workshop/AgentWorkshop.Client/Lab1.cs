using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.Projects;

namespace AgentWorkshop.Client;

public class Lab1(AIProjectClient client, string modelName)
    : Lab(client, modelName)
{
    protected override string InstructionsFileName => "instructions_function_calling.txt";

    public override IEnumerable<FunctionToolDefinition> InitialiseFunctions()
    {
        return [
            new FunctionToolDefinition(
                name: "FetchSalesDataAsync",
                description: "This function is used to answer user questions about Contoso sales data by executing SQLite queries against the database.",
                parameters: BinaryData.FromObjectAsJson(new {
                    Type = "object",
                    Properties = new {
                        Query = new {
                            Type = "string",
                            Description = "The input should be a well-formed SQLite query to extract information based on the user's question. The query result will be returned as a JSON object."
                        }
                    },
                    Required = new [] { "query" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            )
        ];
    }

    protected override async Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate)
    {
        if (agentClient is null)
            return;

        if (requiredActionUpdate.FunctionName == nameof(AgentWorkshop.Client.SalesData.FetchSalesDataAsync))
        {
            var json = JsonSerializer.Deserialize<JsonObject>(requiredActionUpdate.FunctionArguments) ?? throw new InvalidOperationException("Failed to parse JSON object.");
            var query = (json["query"]?.ToString()) ?? throw new InvalidOperationException("Failed to parse query.");
            string result = await SalesData.FetchSalesDataAsync(query);
            AsyncCollectionResult<StreamingUpdate> toolOutputUpdate = agentClient.SubmitToolOutputsToStreamAsync(
                requiredActionUpdate.Value,
                new List<ToolOutput>([new ToolOutput(requiredActionUpdate.ToolCallId, result)])
            );
            await foreach (StreamingUpdate toolUpdate in toolOutputUpdate)
            {
                switch (toolUpdate.UpdateKind)
                {
                    case StreamingUpdateReason.MessageUpdated:
                        MessageContentUpdate messageContentUpdate = (MessageContentUpdate)toolUpdate;
                        Console.Write(messageContentUpdate.Text);
                        break;
                }
            }
        }
    }
}
