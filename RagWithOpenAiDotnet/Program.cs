using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Build();

Console.WriteLine("（＾∀＾●）ﾉｼ");

string? openAiApiKey = configuration.GetSection("appConfig").GetSection("openAiApiKey").Value;
if (openAiApiKey == null)
{
    Console.WriteLine("Open Ai Api Key is not found in appsettings.json");
}
Environment.SetEnvironmentVariable("OPENAI_API_KEY", openAiApiKey);

OpenAIClient openAIClient = new(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
AssistantClient assistantClient = openAIClient.GetAssistantClient();
DocumentRepository documentRepository = new DocumentRepository();

OpenAIFile? file = null;
Assistant? assistant = null;


try
{
    using Stream document = documentRepository.GetTheDocument();

    file = await UploadFile(fileClient, document);

    assistant = await CreateAssistant(assistantClient, file.Id);

    ThreadRun threadRun = await CreateThreadAndRun(assistantClient, assistant.Id);

    threadRun = WaitForThreadCompletion(assistantClient, threadRun);

    DisplayThreadMessages(assistantClient, threadRun);
}
finally
{
    if (file != null)
    {
        await fileClient.DeleteFileAsync(file.Id);
    }
    if (assistant != null)
    {
        await openAIClient.GetVectorStoreClient().DeleteVectorStoreAsync(assistant.ToolResources.FileSearch.VectorStoreIds[0]);
        await assistantClient.DeleteAssistantAsync(assistant.Id);
    }
}

static async Task<OpenAIFile> UploadFile(OpenAIFileClient fileClient, Stream document)
{
    ClientResult<OpenAIFile> file = await fileClient.UploadFileAsync(
            document,
            "monthly_sales.json",
            FileUploadPurpose.Assistants);

    Console.WriteLine("File uploaded successfully");

    return file;
}

static async Task<Assistant> CreateAssistant(AssistantClient assistantClient, string fileId)
{
    AssistantCreationOptions assistantOptions = new()
    {
        Name = "Example: Contoso sales RAG",
        Instructions = "You are an assistant that looks up sales data and helps visualize the information based on user queries.",
        Tools =
        {
            new FileSearchToolDefinition()
        },
        ToolResources = new()
        {
            FileSearch = new()
            {
                NewVectorStores =
                {
                    new VectorStoreCreationHelper([fileId]),
                }
            }
        },
    };

    ClientResult<Assistant> assistant = await assistantClient.CreateAssistantAsync("gpt-4o", assistantOptions);
    Console.WriteLine("Assistant created successfully");
    return assistant;
}

static async Task<ThreadRun> CreateThreadAndRun(AssistantClient assistantClient, string assistantId)
{
    ThreadCreationOptions threadOptions = new()
    {
        InitialMessages = { "How well did product 113045 sell in February? Graph its trend over time." }
    };

    ThreadRun threadRun = await assistantClient.CreateThreadAndRunAsync(assistantId, threadOptions);
    Console.WriteLine("Thread created and started successfully");
    return threadRun;
}

static ThreadRun WaitForThreadCompletion(AssistantClient assistantClient, ThreadRun threadRun)
{
    do
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
        threadRun = assistantClient.GetRun(threadRun.ThreadId, threadRun.Id);
    } while (!threadRun.Status.IsTerminal);

    Console.WriteLine("Thread completed successfully");
    return threadRun;
}

static void DisplayThreadMessages(AssistantClient assistantClient, ThreadRun threadRun)
{
    CollectionResult<ThreadMessage> messages
            = assistantClient.GetMessages(threadRun.ThreadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Ascending });

    foreach (ThreadMessage message in messages)
    {
        Console.Write($"[{message.Role.ToString().ToUpper()}]: ");
        foreach (MessageContent contentItem in message.Content)
        {
            if (!string.IsNullOrEmpty(contentItem.Text))
            {
                Console.WriteLine($"{contentItem.Text}");

                if (contentItem.TextAnnotations.Count > 0)
                {
                    Console.WriteLine();
                }

                // Include annotations, if any.
                foreach (TextAnnotation annotation in contentItem.TextAnnotations)
                {
                    if (!string.IsNullOrEmpty(annotation.InputFileId))
                    {
                        Console.WriteLine($"* File citation, file ID: {annotation.InputFileId}");
                    }
                    if (!string.IsNullOrEmpty(annotation.OutputFileId))
                    {
                        Console.WriteLine($"* File output, new file ID: {annotation.OutputFileId}");
                    }
                }
            }
            // Image files would be included here
        }
        Console.WriteLine();
    }
}


#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.