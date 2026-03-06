using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var wia = Assembly.LoadFrom("$(find ~/.nuget/packages/wiadotnet/1.0.0/lib -name Interop.WIA.dll | head -n 1)");
        foreach(var t in wia.GetTypes())
        {
            if (t.Name.Contains("Prop")) Console.WriteLine(t.Name);
        }
    }
}
