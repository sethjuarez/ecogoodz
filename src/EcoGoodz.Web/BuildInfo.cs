using System.Reflection;

namespace EcoGoodz.Web;

/// <summary>
/// Exposes the build version stamped into the assembly at compile time
/// (see the SetBuildVersion target in EcoGoodz.Web.csproj): vYYYYMMDD.HH.MM.SS+&lt;short-sha&gt;.
/// Shown in the footer so it's obvious, at a glance, exactly which build/commit
/// is running - useful when confirming a deploy actually took effect.
/// </summary>
public static class BuildInfo
{
    public static string Version { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "dev";
}
