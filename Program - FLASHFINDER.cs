//RU.Данный код написан частично человеком,частично ИИ,данный код можно использововать под себя и редактировать и компилировать как вам удобно. использована библеотека Spectre.Console.Приятного пользования!
//ENG.This code is written partly by humans and partly by AI. You can use it yourself, edit it, and compile it as you see fit. The Spectre.Console library is used. Enjoy!

using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

Console.Title = "FlashFinder";

AnsiConsole.Write(new FigletText("FlashFinder").Color(Color.Cyan1));
AnsiConsole.MarkupLine("[bold white]Welcome to FlashFinder![/]");
AnsiConsole.MarkupLine("------------------------------");

if (!AnsiConsole.Confirm("Start searching for files [yellow].swf[/]?", true))
{
    AnsiConsole.MarkupLine("[red]Program close...[/]");
    return;
}

var foundFiles = new List<FileInfo>();
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Scanning...", async ctx =>
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
        foreach (var drive in drives)
        {
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.System
            };

            try 
            {
                var files = Directory.EnumerateFiles(drive.Name, "*.swf", options);
                foreach (var f in files) 
                {
                    foundFiles.Add(new FileInfo(f));
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"Error {drive.Name}: {ex.Message}");
            }
        }
    });

if (foundFiles.Count == 0)
{
    AnsiConsole.MarkupLine("[bold red]Files not found.[/]");
    Console.ReadKey();
    return;
}

AnsiConsole.MarkupLine($"[bold green]Search is over! Files found: {foundFiles.Count}[/]\n");

while (true)
{
    var selectedFile = AnsiConsole.Prompt(
        new SelectionPrompt<FileInfo>()
            .Title("Выберите файл ([blue]Arrows[/] to navigate, [green]Enter[/] to select):")
            .PageSize(10)
            .MoreChoicesText("[grey](Scroll down to see more)[/]")
            .AddChoices(foundFiles)
            .UseConverter(f => $"{f.Name} [grey]({f.DirectoryName})[/]"));

    var action = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Что сделать?")
            .AddChoices(new[] { "Start Ruffle", "Open folder", "Back to the list", "Exit" }));

    if (action == "Exit") break;
    if (action == "Back to the list") continue;

    if (action == "Start Ruffle")
    {
        string baseDir = AppContext.BaseDirectory; 
        string rufflePath = Path.Combine(baseDir, "ruffle.exe");

        
        if (!File.Exists(rufflePath))
        {
            AnsiConsole.MarkupLine("[red]Error: ruffle.exe not found in program folder![/]");
            continue;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = rufflePath,
            Arguments = $"\"{selectedFile.FullName}\"",
            UseShellExecute = false
        };

        MinimizeConsole();
        var process = Process.Start(startInfo);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        await process!.WaitForExitAsync();
        RestoreConsole();
    }
    else if (action == "Open folder")
    {
        Process.Start("explorer.exe", $"/select,\"{selectedFile.FullName}\"");
    }
}

static void MinimizeConsole() => ShowWindow(GetConsoleWindow(), 6); // 6 = SW_MINIMIZE
static void RestoreConsole() => ShowWindow(GetConsoleWindow(), 9);  // 9 = SW_RESTORE

[DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
[DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


