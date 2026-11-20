using System.Reflection;

namespace MvcBB.ModKit
{
    /// <summary>
    /// Shared discovery logic used by both MvcBB.API's and MvcBB.App's startup
    /// mod-loading bootstrap, so the "scan a folder, load assemblies, find
    /// implementations" mechanics live in one place instead of being copied
    /// between the two Program.cs files.
    /// </summary>
    public static class ModLoader
    {
        /// <summary>
        /// Loads every .dll in <paramref name="modsFolderPath"/> (non-recursive).
        /// Missing folder just means no mods are installed - returns empty.
        /// </summary>
        public static IReadOnlyList<Assembly> LoadModAssemblies(string modsFolderPath)
        {
            if (!Directory.Exists(modsFolderPath))
                return Array.Empty<Assembly>();

            var assemblies = new List<Assembly>();
            foreach (var dllPath in Directory.GetFiles(modsFolderPath, "*.dll"))
            {
                assemblies.Add(Assembly.LoadFrom(dllPath));
            }
            return assemblies;
        }

        /// <summary>
        /// Instantiates every public, non-abstract, parameterless-constructable
        /// type implementing <typeparamref name="T"/> across the given assemblies.
        /// </summary>
        public static IReadOnlyList<T> CreateMods<T>(IEnumerable<Assembly> assemblies) where T : class
        {
            var mods = new List<T>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !type.IsClass || !typeof(T).IsAssignableFrom(type))
                        continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null)
                        continue;

                    if (Activator.CreateInstance(type) is T mod)
                        mods.Add(mod);
                }
            }
            return mods;
        }

        /// <summary>
        /// Whether <paramref name="coreVersion"/> falls within [minCoreVersion, maxCoreVersion]
        /// (inclusive; a null/unparsable max means no upper bound). Used to decide a
        /// mod's compatibility status before it's allowed to be enabled.
        /// </summary>
        public static bool IsCompatible(string minCoreVersion, string? maxCoreVersion, Version coreVersion)
        {
            if (!Version.TryParse(minCoreVersion, out var min))
                return false;
            if (coreVersion < min)
                return false;

            if (!string.IsNullOrWhiteSpace(maxCoreVersion) && Version.TryParse(maxCoreVersion, out var max) && coreVersion > max)
                return false;

            return true;
        }
    }
}
