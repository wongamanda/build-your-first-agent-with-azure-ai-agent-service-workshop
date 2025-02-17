using System;
using Azure.AI.Projects;

namespace AgentWorkshop.Client;

public static class Utils
{
    public static void LogGreen(string message) => Console.WriteLine($"\u001b[32m{message}\u001b[0m");
    public static void LogPurple(string message) => Console.WriteLine($"\u001b[35m{message}\u001b[0m");
    public static void LogBlue(string message) => Console.WriteLine($"\u001b[34m{message}\u001b[0m");

    public static async Task GetFile(AIProjectClient projectClient, string? fileId, string attachmentName)
    {
        LogGreen($"Getting file with ID: {fileId}");

        string[] parts = attachmentName.Split(':');
        string lastPart = parts[parts.Length - 1];
        string baseName = Path.GetFileName(lastPart);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
        string fileExtension = Path.GetExtension(baseName);

        string fileName = $"{fileNameWithoutExt}.{fileId}{fileExtension}";

        string env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "local";
        string folderPath = (env == "container" ? "src/workshop/" : "") + "files";

        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, fileName);

        var agentsClient = projectClient.GetAgentsClient();

        using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        BinaryData contentChunks = await agentsClient.GetFileContentAsync(fileId);

        // Convert BinaryData to a byte array
        byte[] fileBytes = contentChunks.ToArray();
        await fileStream.WriteAsync(fileBytes);

        LogGreen($"File saved to {filePath}");

        // Cleanup the remote file
        await agentsClient.DeleteFileAsync(fileId);
    }

    public static async Task GetFiles(AIProjectClient projectClient, ThreadMessage message)
    {
        foreach (MessageContent contentItem in message.ContentItems)
        {
            if (contentItem is MessageImageFileContent imageFileContent)
            {
                await GetFile(projectClient, imageFileContent.FileId, "unknown");
            }
            else if (contentItem is MessageTextContent textContent)
            {
                await GetFile(projectClient, fileId: null, textContent.Text);
            }
        }
    }

    public static async Task<VectorStore> CreateVectorStore(AIProjectClient projectClient, List<string> files, string vectorStoreName)
    {
        List<string> fileIds = new();
        string env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "local";
        string prefix = env == "container" ? "src/workshop/" : "";
        var agentsClient = projectClient.GetAgentsClient();

        foreach (var file in files)
        {
            string filePath = prefix + file;
            LogPurple($"Uploading file: {filePath}");
            // Adjust the upload API call if needed
            AgentFile fileInfo = await agentsClient.UploadFileAsync(filePath, AgentFilePurpose.Agents);
            fileIds.Add(fileInfo.Id);
        }

        LogPurple("Creating the vector store");
        var vectorStore = await agentsClient.CreateVectorStoreAsync(fileIds, vectorStoreName);
        LogPurple("Vector store created and files added.");
        return vectorStore;
    }
}
