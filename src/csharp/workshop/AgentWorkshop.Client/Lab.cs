using System.ClientModel;
using Azure.AI.Projects;

namespace AgentWorkshop.Client;

public abstract class Lab(AIProjectClient client, string modelName) : IAsyncDisposable
{
    protected readonly SalesData SalesData = new();

    protected AIProjectClient Client { get; } = client;
    protected string ModelName { get; } = modelName;
    protected AgentsClient? agentClient;
    protected Agent? agent;
    protected AgentThread? thread;

    protected abstract string InstructionsFileName { get; }

    private readonly List<ToolDefinition> tools = [];

    const int maxCompletionTokens = 4096;
    const int maxPromptTokens = 10240;
    const float temperature = 0.1f;
    const float topP = 0.1f;

    public virtual IEnumerable<FunctionToolDefinition> InitialiseFunctions() => [];

    public async Task RunAsync()
    {
        tools.AddRange(InitialiseFunctions());

        string instructionsFile = "../../../../../../shared/instructions/" + InstructionsFileName;

        if (!File.Exists(instructionsFile))
        {
            throw new FileNotFoundException("Instructions file not found.", instructionsFile);
        }

        string instructions = File.ReadAllText(instructionsFile);

        string databaseSchema = await SalesData.GetDatabaseInfoAsync();

        instructions = instructions.Replace("{database_schema_string}", databaseSchema);

        await Console.Out.WriteLineAsync("Creating agent...");
        agentClient = Client.GetAgentsClient();
        agent = await agentClient.CreateAgentAsync(
            model: ModelName,
            name: "Constoso Sales AI Agent",
            instructions: instructions,
            tools: tools,
            temperature: temperature
        );

        await Console.Out.WriteLineAsync($"Agent created with ID: {agent.Id}");

        await Console.Out.WriteLineAsync("Creating thread...");
        thread = await agentClient.CreateThreadAsync();
        await Console.Out.WriteLineAsync($"Thread created with ID: {thread.Id}");

        while (true)
        {
            await Console.Out.WriteLineAsync();
            Utils.LogGreen("Enter your query (type 'exit' to quit):");
            string? prompt = await Console.In.ReadLineAsync();

            if (prompt is null)
            {
                continue;
            }

            if (prompt.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                break;
            }

            _ = await agentClient.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: prompt
            );

            AsyncCollectionResult<StreamingUpdate> su = agentClient.CreateRunStreamingAsync(
                threadId: thread.Id,
                assistantId: agent.Id,
                maxCompletionTokens: maxCompletionTokens,
                maxPromptTokens: maxPromptTokens,
                temperature: temperature,
                topP: topP
            );

            await foreach (StreamingUpdate update in su)
            {
                switch (update.UpdateKind)
                {
                    case StreamingUpdateReason.RunRequiresAction:
                        RequiredActionUpdate requiredActionUpdate = (RequiredActionUpdate)update;
                        await HandleActionAsync(requiredActionUpdate);
                        break;
                }
            }
        }
    }

    protected virtual Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (agentClient is not null)
        {
            if (thread is not null)
            {
                await agentClient.DeleteThreadAsync(thread.Id);
            }

            if (agent is not null)
            {
                await agentClient.DeleteAgentAsync(agent.Id);
            }
        }

        SalesData.Dispose();
    }
}