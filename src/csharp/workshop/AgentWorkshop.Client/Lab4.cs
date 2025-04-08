using Azure.AI.Projects;

namespace AgentWorkshop.Client;

public class Lab4(AIProjectClient client, string modelName) : Lab(client, modelName)
{
    protected override string InstructionsFileName => "code_interpreter_multilingual.txt";

    protected override Task InitialiseLabAsync(AgentsClient agentClient) =>
        agentClient.UploadFileAsync(
            filePath: Path.Combine(SharedPath, "fonts", "fonts.zip"),
            purpose: AgentFilePurpose.Agents
        );
}
