﻿//RU.Данный код написан частично человеком,частично ИИ,данный код можно использововать под себя и редактировать и компилировать как вам удобно. использована библеотека Spectre.Console.Приятного пользования!
//ENG.This code is written partly by humans and partly by AI. You can use it yourself, edit it, and compile it as you see fit. The Spectre.Console library is used. Enjoy!

using Color = Spectre.Console.Color;
using System.Diagnostics;
using Spectre.Console;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FlashFinder;

static class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_MINIMIZE = 6;

    [STAThread]
    static void Main(string[] args)
    {
        Console.Title = "FlashFinder";
        AnsiConsole.Write(new FigletText("FlashFinder").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[bold white]Welcome to FlashFinder![/]");
        AnsiConsole.MarkupLine("------------------------------");

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [yellow]the search mode[/]:")
                .AddChoices(["All system", "Select directory", "Exit"]));

        if (mode == "Exit") return;

        string? targetPath = null;

        if (mode == "Select directory")
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a directory to search for .swf files";
            dialog.UseDescriptionForTitle = true;
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                targetPath = dialog.SelectedPath;
                AnsiConsole.MarkupLine($"[grey]Directory selected: {targetPath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Selection cancelled.[/]");
                return;
            }
        }

        var foundFiles = new List<FileInfo>();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Scanning...", ctx =>
            {
                var scanRoots = targetPath != null
                    ? new List<string> { targetPath }
                    : DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name).ToList();

                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.System
                };

                foreach (var root in scanRoots)
                {
                    ctx.Status($"Search in: [yellow]{root}[/]");
                    try
                    {
                        foreach (var f in Directory.EnumerateFiles(root, "*.swf", options))
                        {
                            foundFiles.Add(new FileInfo(f));
                        }
                    }
                    catch { /* Ignoring access errors */ }
                }
            });

        if (foundFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[bold red]No .swf files found.[/]");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        while (true)
        {
            var selectedFile = AnsiConsole.Prompt(
                new SelectionPrompt<FileInfo>()
                    .Title("Choose file ([blue]Arrows[/] to navigate, [green]Enter[/] to select):")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Scroll down to see more)[/]")
                    .AddChoices(foundFiles)
                    .UseConverter(f => $"{f.Name} [grey]({f.DirectoryName})[/]"));

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What to do?")
                    .AddChoices(new[] { "Start Ruffle", "Open folder", "Back to the list", "Exit" }));

            if (action == "Exit") break;
            if (action == "Back to the list") continue;

            if (action == "Open folder")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedFile.DirectoryName ?? "",
                    UseShellExecute = true,
                    Verb = "open"
                });
                continue;
            }

            if (action == "Start Ruffle")
            {
                string rufflePath = Path.Combine(AppContext.BaseDirectory, "ruffle.exe");

                if (File.Exists(rufflePath))
                {
                    AnsiConsole.MarkupLine($"[green]Launching:[/] {selectedFile.Name}");

                    Process.Start(new ProcessStartInfo(rufflePath, $"\"{selectedFile.FullName}\"")
                    {
                        UseShellExecute = false
                    });

                    IntPtr hWnd = GetConsoleWindow();
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, SW_MINIMIZE);
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Error: ruffle.exe not found in application directory![/]");
                    AnsiConsole.MarkupLine("[grey]Press any key to return to the menu...[/]");
                    Console.ReadKey(true);
                }
            }
        }
    }
}

