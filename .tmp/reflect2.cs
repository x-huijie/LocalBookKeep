using System.Reflection;

var path = @"C:\Users\Administrator\.nuget\packages\avalonia\12.1.4\lib\net10.0\Avalonia.Base.dll";
var asm = Assembly.LoadFrom(path);
var t = asm.GetType("Avalonia.StyledElement")!;

Console.WriteLine("=== StyleKey related members ===");
foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
             .Where(p => p.Name.Contains("StyleKey")))
{
    var g = p.GetMethod;
    Console.WriteLine($"PROP {p.PropertyType.Name} {p.Name} virtual={g?.IsVirtual} family={g?.IsFamily}");
}

foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
             .Where(m => m.Name.Contains("StyleKey")))
{
    Console.WriteLine($"METHOD {m.ReturnType.Name} {m.Name}() virtual={m.IsVirtual} family={m.IsFamily}");
}
