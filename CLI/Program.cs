using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;

var bundleOption = new Option<FileInfo>("--output", "File path and name") { IsRequired = true };
var languageOption = new Option<string>("--language", "Programming languages to include (comma-separated or 'all')") { IsRequired = true };
var noteOption = new Option<bool>("--note", "Include source file paths as comments");
var sortOption = new Option<string>("--sort", () => "name", "Sorting order: 'name' or 'type'");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from source code");
var authorOption = new Option<string>("--author", "Author name to include in the bundle");

var bundleCommand = new Command("bundle", "Bundle code files to a single file")
{
    bundleOption,
    languageOption,
    noteOption,
    sortOption,
    removeEmptyLinesOption,
    authorOption
};

bundleCommand.SetHandler(
    (FileInfo output, string language, bool note, string sort, bool removeEmptyLines, string author) =>
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var targetLanguages = language.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? Array.Empty<string>()
                : language.Split(',').Select(lang => lang.Trim()).ToArray();

            var files = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
                .Where(file => targetLanguages.Length == 0 || targetLanguages.Contains(Path.GetExtension(file).TrimStart('.')))
                .OrderBy(file => sort == "type" ? Path.GetExtension(file) : Path.GetFileName(file));

            if (!files.Any())
            {
                Console.WriteLine("No files found matching the specified criteria.");
                return;
            }

            using var writer = new StreamWriter(output.FullName, false, Encoding.UTF8);
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"// Author: {author}");
            }

            foreach (var file in files)
            {
                if (note)
                {
                    writer.WriteLine($"// Source: {Path.GetRelativePath(currentDir, file)}");
                }

                var lines = File.ReadAllLines(file);
                if (removeEmptyLines)
                {
                    lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            Console.WriteLine($"Bundle created at: {output.FullName}");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Error: File path is invalid.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    },
    bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption
);


    var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");
createRspCommand.SetHandler(() =>
{
    Console.Write("Enter output file path and name: ");
    var output = Console.ReadLine();

    Console.Write("Enter programming languages (comma-separated or 'all'): ");
    var language = Console.ReadLine();

    Console.Write("Include source file paths as comments? (true / false): ");
    var note = bool.Parse(Console.ReadLine());

    Console.Write("Sorting order ('name' or 'type'): ");
    var sort = Console.ReadLine();

    Console.Write("Remove empty lines? (true / false): ");
    var removeEmptyLines = bool.Parse(Console.ReadLine());

    Console.Write("Enter author name (optional): ");
    var author = Console.ReadLine();

    var rspContent = new StringBuilder()
        .AppendLine($"bundle --output \"{output}\" --language \"{language}\" --note {note} --sort {sort} --remove-empty-lines {removeEmptyLines} --author \"{author}\"");

    var rspFileName = "bundle.rsp";
    File.WriteAllText(rspFileName, rspContent.ToString());
    Console.WriteLine($"Response file created: {rspFileName}");
});

var rootCommand = new RootCommand("Root command for File Bundle CLI")
{
    bundleCommand,
    createRspCommand
};

await rootCommand.InvokeAsync(args);
