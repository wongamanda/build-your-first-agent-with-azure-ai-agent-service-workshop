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
                name: nameof(AgentWorkshop.Client.SalesData.FetchSalesDataAsync),
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

        if (requiredActionUpdate.FunctionName != nameof(AgentWorkshop.Client.SalesData.FetchSalesDataAsync))
        {
            return;
        }

        FetchSalesDataArgs salesDataArgs = JsonSerializer.Deserialize<FetchSalesDataArgs>(requiredActionUpdate.FunctionArguments) ?? throw new InvalidOperationException("Failed to parse JSON object.");
        string result = await SalesData.FetchSalesDataAsync(salesDataArgs.Query);
        AsyncCollectionResult<StreamingUpdate> toolOutputUpdate = agentClient.SubmitToolOutputsToStreamAsync(
            requiredActionUpdate.Value,
            new List<ToolOutput>([new ToolOutput(requiredActionUpdate.ToolCallId, result)])
        );
        await foreach (StreamingUpdate toolUpdate in toolOutputUpdate)
        {
            if (toolUpdate.UpdateKind == StreamingUpdateReason.MessageUpdated)
            {
                // The tool has a response, so we can print it to the console.
                MessageContentUpdate messageContentUpdate = (MessageContentUpdate)toolUpdate;
                await Console.Out.WriteAsync(messageContentUpdate.Text);
                continue;
            }

            // Ignore the other update types
        }
    }

    record FetchSalesDataArgs(string Query);
}
