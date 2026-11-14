using AsmLocator;

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
    var locator = AssemblyLocator.FromFile(dllPath);

    TypeSpec type = TypeSpec.ByFullName("Easeware.Driver.Core.Service.Impl.ProductService");

    //var spec = MethodSpec.ByName("b") with
    //{
    //    ParameterCount = 3,
    //    HasBody = true
    //};

    var spec = MethodSpec.Of("b", "System.String", "System.String", "System.Boolean");

    var method = locator.GetMethod(type, spec);

    var body = method.CilMethodBody;

    Console.WriteLine($"Instruction count: {body?.Instructions.Count ?? 0}");
    Console.WriteLine($"Method: {method.FullName}");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("Failed to load assembly.");
    Console.WriteLine(ex);
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();