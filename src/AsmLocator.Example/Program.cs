

using AsmLocator;
using AsmResolver.DotNet;

const string defaultPath = @"C:\Program Files\Easeware\DriverEasy";
const string dllName = "Easeware.Driver.Core.dll";
const string appName = "DriverEasy.exe";
string appPath = Path.Combine(defaultPath, appName);
string dllPath = Path.Combine(defaultPath, dllName);

if (!File.Exists(dllPath))
{
    Console.WriteLine("DriverEasy installation was not found.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("Enter DriverEasy directory path: ");

        string? directory = Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(directory))
        {
            Console.WriteLine("Path cannot be empty.");
            continue;
        }

        dllPath = Path.Combine(directory, dllName);

        if (File.Exists(dllPath))
            break;

        Console.WriteLine($"DLL not found: {dllPath}");
        Console.WriteLine("Please try again.");
        Console.WriteLine();
    }
}


try
{
    var locator = AssemblyLocator.FromFile("MyApplication.dll");

    TypeSpec typeSpec = "MyApplication.Services.UserService";
    var method = locator.GetMethod(typeSpec, MethodSpec.ByName("GetUser"));

    var body = method.CilMethodBody;

    Console.WriteLine($"Method: {method.FullName}");
    Console.WriteLine($"Instruction count: {body?.Instructions.Count ?? 0}");

    locator.Save("MyApplication.patched.dll");
}
catch
{

}