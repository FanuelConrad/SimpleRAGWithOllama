using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Configuration;
using System.Collections.ObjectModel;

namespace SimpleRAGWithOllama
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var ollamaConfig = new OllamaConfig()
            {
                TextModel = new OllamaModelConfig("mistral:latest") { MaxTokenTotal = 125000, Seed = 42, TopK = 7 },
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
                    MaxTokensPerParagraph = 20,
                    OverlappingTokens = 10
                });

            var memory = memoryBuilder.Build();

            var index = "ragwithollama";

            var indexes = await memory.ListIndexesAsync();
            if (indexes.Any(i => String.Equals(i.Name, index, StringComparison.OrdinalIgnoreCase)))
            {
                await memory.DeleteIndexAsync(index);
            }

            var document = new Document().AddFiles(["Data/Persons.txt"]);

            var documentID = await memory.ImportDocumentAsync(document, index: index);

            var memoryFilter = MemoryFilters.ByDocument(documentID);

            var chatHistory = new ChatHistory();
            Console.WriteLine("You can exit the console by tapping 'Exit'.");
            Console.WriteLine("First Question: When was Ethan Carter born?");
            var userInput = "When was Ethan Carter born?";

            while (userInput != "Exit")
            {
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                var conversationContext = chatHistory.GetHistoryAsContext();
                var fullQuery = ComposeQuery(userInput, conversationContext);
                var answer = await memory.AskAsync(
                    fullQuery,
                    index,
                    memoryFilter,
                    minRelevance: .6f);

                chatHistory.AddUserMessage(userInput);
                chatHistory.AddAssistantMessage(answer.Result);
                Console.WriteLine(answer.Result);

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
