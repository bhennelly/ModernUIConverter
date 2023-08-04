// See https://aka.ms/new-console-template for more information
using ModernUIConverter;

Console.WriteLine("Started Modern UI Converter");

if (args == null || args.Length == 0)
{
    throw new ArgumentNullException("application requires 2 arguments.");
}

Console.WriteLine("Arguments:");
var argCntr = 0;
foreach (var arg in args)
{
    argCntr++;
    Console.WriteLine($"{argCntr}: {arg}");
}

var classPageFile = args[0] ?? throw new ArgumentNullException("First parameter required - full path of classic aspx file");
var outputDir = args[1] ?? throw new ArgumentNullException("Second parameter required - output directory");

if (!Directory.Exists(outputDir))
{
    throw new DirectoryNotFoundException(outputDir);
}

Console.WriteLine("Reading page file");
var uiReader = new ClassicUIReader(classPageFile);

Console.WriteLine("Building TS File");

var tsFile = new TSFileBuilder(uiReader.GraphType, uiReader.PrimaryView, uiReader.ScreenID);

if (uiReader.Views.TryGetValue(uiReader.PrimaryView, out var primaryView))
{
    tsFile.AddView(primaryView);
}

foreach (var view in uiReader.Views.Values)
{
    if (view.Name == uiReader.PrimaryView)
    {
        continue;
    }

    tsFile.AddView(view);
}

var outputFolder = Path.Combine(outputDir, uiReader.ScreenID);
if (!Directory.Exists(outputFolder))
{
    Directory.CreateDirectory(outputFolder);
}

var outputTSFile = Path.Combine(outputFolder, $"{uiReader.ScreenID}.ts");
Console.WriteLine($"Saving TS file: {outputTSFile}");
File.WriteAllText(outputTSFile, tsFile.GetFileContent());
Console.WriteLine("File saved");

Console.WriteLine("Building HTML File");

var htmlFile = new HTMLFileBuilder();

foreach (var pageContent in uiReader.PageContents)
{
    htmlFile.AddPageContent(pageContent);
}


var outputHTMLFile = Path.Combine(outputFolder, $"{uiReader.ScreenID}.html");
Console.WriteLine($"Saving HTML file: {outputHTMLFile}");
File.WriteAllText(outputHTMLFile, htmlFile.GetFileContent());
Console.WriteLine("File saved");