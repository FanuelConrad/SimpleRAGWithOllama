# Simple RAG Console App with Ollama

## Overview
This project is a simple Retrieval-Augmented Generation (RAG) console application built using C# and the [Microsoft.KernelMemory](https://github.com/microsoft/kernel-memory) library. It utilizes the Ollama model for text generation and embedding, allowing users to query an indexed document interactively.

## Features
- Uses **Ollama** for text generation (`mistral:latest`) and text embedding (`nomic-embed-text:latest`).
- Implements **Kernel Memory** for indexing and querying documents.
- Supports **custom text partitioning** options to optimize search performance.
- Maintains **chat history** to provide conversational context.
- Provides **interactive console-based chat** with a document-based knowledge base.

## Prerequisites
- [.NET 9.0+](https://dotnet.microsoft.com/en-us/download/dotnet)
- [Ollama](https://ollama.com) installed and running locally
- Ollama server running on `http://localhost:11434/`
- A text file (`Data/Persons.txt`) to be indexed for retrieval

## Installation & Setup
### 1. Install Ollama Server
Follow the instructions on [Ollama's official website](https://ollama.com/) to install the server on your machine.

### 2. Pull Required Models
Once Ollama is installed, pull the required models using the following commands:
```sh
ollama pull mistral:latest
ollama pull nomic-embed-text:latest
```

### 3. Clone the Repository
```sh
git clone https://github.com/KsiProgramming/SimpleRAGWithOllama
cd SimpleRAGWithOllama
```

### 4. Install Dependencies
Ensure you have the necessary NuGet packages installed:
```sh
dotnet add package Microsoft.KernelMemory
```

### 5. Run the Application
```sh
dotnet run
```

## Usage
1. Start the application.
2. The program will index the `Data/Persons.txt` file.
3. You will be prompted to ask a question, e.g., `When was Ethan Carter born?`.
4. The app retrieves and displays relevant information from the indexed document.
5. Continue asking questions interactively or type `Exit` to quit.

## Code Structure
- `OllamaConfig` is used to configure text generation and embedding models.
- `KernelMemoryBuilder` initializes memory storage and search configurations.
- `ChatHistory` maintains conversation context for better responses.
- `ComposeQuery` structures user input with chat history for contextual queries.

## Configuration
Modify the `OllamaConfig` parameters to customize model behavior:
```csharp
var ollamaConfig = new OllamaConfig()
{
    TextModel = new OllamaModelConfig("mistral:latest") { MaxTokenTotal = 125000, Seed = 42, TopK = 7 },
    EmbeddingModel = new OllamaModelConfig("nomic-embed-text:latest") { MaxTokenTotal = 2048 },
    Endpoint = "http://localhost:11434/"
};
```

## Troubleshooting
### 1. Ollama Server Not Running
Ensure the Ollama server is running on port 11434:
```sh
ollama serve
```

### 2. Document Not Found
Make sure `Data/Persons.txt` exists before running the application.

### 3. Missing Dependencies
If you encounter missing package errors, restore dependencies:
```sh
dotnet restore
```

## License
This project is licensed under the MIT License.

## Acknowledgments
- [Microsoft Kernel Memory](https://github.com/microsoft/kernel-memory)
- [Ollama AI](https://ollama.com)

