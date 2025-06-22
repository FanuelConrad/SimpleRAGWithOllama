using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Configuration;
using System.Collections.ObjectModel;

namespace SimpleRAGWithOllama
{
    internal class Program
    {
        private const int MAX_CONCURRENT_PROCESSING = 3; // Control parallel processing

        static async Task Main(string[] args)
        {
            var ollamaConfig = new OllamaConfig()
            {
                TextModel = new OllamaModelConfig("gemma:2b") { MaxTokenTotal = 8192, Seed = 42, TopK = 7 },
                EmbeddingModel = new OllamaModelConfig("nomic-embed-text:latest") { MaxTokenTotal = 2048 },
                Endpoint = "http://localhost:11434/"
            };

            var memoryBuilder = new KernelMemoryBuilder()
                .WithOllamaTextGeneration(ollamaConfig)
                .WithOllamaTextEmbeddingGeneration(ollamaConfig)
                .WithSearchClientConfig(new SearchClientConfig()
                {
                    AnswerTokens = 4096
                })
                .WithCustomTextPartitioningOptions(new TextPartitioningOptions()
                {
                    MaxTokensPerParagraph = 1024,    // Increased for better context
                    OverlappingTokens = 128           // Maintained for context continuity
                });

            var memory = memoryBuilder.Build();

            var index = "ragwithollama";

            var indexes = await memory.ListIndexesAsync();
            if (indexes.Any(i => String.Equals(i.Name, index, StringComparison.OrdinalIgnoreCase)))
            {
                await memory.DeleteIndexAsync(index);
            }

            var pdfFiles = Directory
                .GetFiles("Data", "*.pdf")
                .Where(f => Path.GetFileName(f).IndexOf("Callout", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            var documentIDs = new List<string>();
            var semaphore = new SemaphoreSlim(MAX_CONCURRENT_PROCESSING);

            // Process files in parallel with controlled concurrency
            var processingTasks = pdfFiles.Select(async file =>
            {
                try
                {
                    await semaphore.WaitAsync();
                    Console.WriteLine($"Processing file: {Path.GetFileName(file)}");
                    var document = new Document().AddFiles(new[] { file });
                    var result = await memory.ImportDocumentAsync(document, index: index);
                    documentIDs.Add(result);
                    Console.WriteLine($"Successfully processed: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(file)}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(processingTasks);

            var chatHistory = new ChatHistory();
            Console.WriteLine("You can exit the console by tapping 'Exit'.");
            Console.WriteLine("First Question: Summarize the reports?");
            var userInput = "Summarize the reports?";

            while (userInput != "Exit")
            {
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                try
                {
                    var conversationContext = chatHistory.GetHistoryAsContext();
                    var fullQuery = ComposeQuery(userInput, conversationContext);
                    var answer = await memory.AskAsync(
                        fullQuery,
                        index,
                        minRelevance: .3f);

                    chatHistory.AddUserMessage(userInput);
                    chatHistory.AddAssistantMessage(answer.Result);
                    Console.WriteLine(answer.Result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing query: {ex.Message}");
                }

                Console.WriteLine("Please Ask your question");
                userInput = Console.ReadLine();
            }
        }

        static string ComposeQuery(string userInput, string conversationContext)
        {
            return $"{conversationContext}\nUser: {userInput}";
        }

        public class ChatHistory
        {
            private readonly Collection<string> _messages = [];

            public void AddUserMessage(string message)
            {
                _messages.Add($"User: {message}");
            }

            public void AddAssistantMessage(string message)
            {
                _messages.Add($"Assistant: {message}");
            }

            public string GetHistoryAsContext(int maxMessages = 10)
            {
                var recentMessages = _messages.TakeLast(maxMessages);
                return string.Join("\n", recentMessages);
            }
        }
    }
}
