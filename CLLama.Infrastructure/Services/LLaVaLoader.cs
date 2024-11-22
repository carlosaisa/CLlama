﻿using LLama;
using LLama.Common;
using System.Text.RegularExpressions;

/*
 * - WIP -
 * Code extracted from LLamaSharp documentation:
 * https://scisharp.github.io/LLamaSharp/0.11.0/Examples/LLavaInteractiveModeExecute/
*/
namespace CLLama.Infrastructure.Services
{
    public class LLaVaLoader
    {
        public async Task Loader(string modelPath)
        {
            string multiModalProj = @"<Your multi-modal proj file path>";
            string modelImage = @"<Your image path>";
            const int maxTokens = 1024; // The max tokens that could be generated.

            var prompt = $"{{{modelImage}}}\nUSER:\nProvide a full description of the image.\nASSISTANT:\n";
            if (prompt == null)
                return;

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                //Seed = 1337,
            };
            using var model = LLamaWeights.LoadFromFile(parameters);
            using var context = model.CreateContext(parameters);

            // Llava Init
            using var clipModel = LLavaWeights.LoadFromFile(multiModalProj);

            var ex = new InteractiveExecutor(context, clipModel);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("The executor has been enabled. In this example, the prompt is printed, the maximum tokens is set to {0} and the context size is {1}.", maxTokens, parameters.ContextSize);
            Console.WriteLine("To send an image, enter its filename in curly braces, like this {c:/image.jpg}.");

            var inferenceParams = new InferenceParams() { 
                //Temperature = 0.1f, 
                AntiPrompts = new List<string> { "\nUSER:" }, 
                MaxTokens = maxTokens 
            };

            do {
                // Evaluate if we have images
                //
                var imageMatches = Regex.Matches(prompt, "{([^}]*)}").Select(m => m.Value);
                var imageCount = imageMatches.Count();
                var hasImages = imageCount > 0;
                byte[][]? imageBytes = null;

                if (hasImages)
                {
                    var imagePathsWithCurlyBraces = Regex.Matches(prompt, "{([^}]*)}").Select(m => m.Value);
                    var imagePaths = Regex.Matches(prompt, "{([^}]*)}").Select(m => m.Groups[1].Value);

                    try {
                        imageBytes = imagePaths.Select(File.ReadAllBytes).ToArray();
                    }
                    catch (IOException exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(
                            $"Could not load your {(imageCount == 1 ? "image" : "images")}:");
                        Console.Write($"{exception.Message}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Please try again.");
                        break;
                    }


                    int index = 0;
                    foreach (var path in imagePathsWithCurlyBraces)
                    {
                        // First image replace to tag <image, the rest of the images delete the tag
                        if (index++ == 0)
                            prompt = prompt.Replace(path, "<image>");
                        else
                            prompt = prompt.Replace(path, "");
                    }
                    Console.WriteLine();


                    // Initialize Images in executor
                    //ex.ImagePaths = imagePaths.ToList();
                }

                Console.ForegroundColor = ConsoleColor.White;
                await foreach (var text in ex.InferAsync(prompt, inferenceParams))
                {
                    Console.Write(text);
                }
                Console.Write(" ");
                Console.ForegroundColor = ConsoleColor.Green;
                prompt = Console.ReadLine();
                Console.WriteLine();

                // let the user finish with exit
                if (prompt!.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            while (true);
        }
    }
}
