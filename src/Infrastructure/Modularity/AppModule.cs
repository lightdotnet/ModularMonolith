namespace StarterKit.Infrastructure.Modularity;

public abstract class AppModule : Light.AspNetCore.Modularity.AppModule
{
    public virtual void ShowModuleInfo()
    {
        AppLogging.Information("Module {name} injected", GetModuleName());
    }

    public virtual void ShowEndpointInfo()
    {
        AppLogging.Information("Endpoints {name} injected", GetModuleName());
    }

    private string GetModuleName(string separate = "_")
    {
        string moduleName = GetType().Name;

        string newValue = "";

        for (int i = 0; i < moduleName.Length; i++)
            if (char.IsUpper(moduleName[i]))
                newValue += i == 0 // first char
                    ? char.ToLower(moduleName[i])
                    : separate + char.ToLower(moduleName[i]); // add prefix to upper chars
            else
                newValue += moduleName[i];

        return newValue;
    }
}
