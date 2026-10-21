using System;
using System.IO;
using Unity.Shared.Core.Json;

namespace Unity.Shared.Core
{
    /// <summary>
    /// The one-line way in: <c>Profiles.Current.Settings.Common.DisplayName</c>. In Unity the package's runtime
    /// assembly points this at <c>Application.persistentDataPath</c> before the first scene loads; anywhere else
    /// (dotnet tests, tools) call <see cref="UseRoot"/> or <see cref="UseDataPath"/> first, or set the
    /// <see cref="RootEnvironmentVariable"/> environment variable.
    /// </summary>
    public static class Profiles
    {
        /// <summary>An environment variable naming a Profiles folder to use instead of the data path's (tests, automation, CI).</summary>
        public const string RootEnvironmentVariable = "MINDATTIC_PROFILES_ROOT";

        /// <summary>A command-line switch naming a Profiles folder, as <c>-profiles-root &lt;path&gt;</c> or <c>-profiles-root=&lt;path&gt;</c>.</summary>
        public const string RootCommandLineSwitch = "-profiles-root";

        static ProfileStore store;

        /// <summary>Whether a store has been set (by the Unity bootstrap, <see cref="UseRoot"/>, <see cref="UseDataPath"/> or the override).</summary>
        public static bool IsConfigured => store != null;

        /// <summary>
        /// The store in use. If none was set but the environment variable or command-line override names a
        /// folder, that is used; otherwise this throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public static ProfileStore Store
        {
            get
            {
                if (store != null) return store;
                string overrideRoot = OverrideRoot();
                if (overrideRoot != null) return store = new ProfileStore(overrideRoot);
                throw new InvalidOperationException(
                    "Profiles has no folder yet. In Unity the Unity.Shared assembly sets it; elsewhere call Profiles.UseRoot(path) or Profiles.UseDataPath(path), or set " + RootEnvironmentVariable + ".");
            }
        }

        /// <summary>The profile in use (see <see cref="ProfileStore.Current"/>).</summary>
        public static Profile Current => Store.Current;

        /// <summary>
        /// Uses <c>&lt;dataPath&gt;/Profiles</c>, unless the environment variable or command-line override names
        /// another folder, which wins. Returns the store.
        /// </summary>
        public static ProfileStore UseDataPath(string dataPath, IJsonCodec codec = null)
        {
            string overrideRoot = OverrideRoot();
            store = overrideRoot != null ? new ProfileStore(overrideRoot, codec) : ProfileStore.ForDataPath(dataPath, codec);
            return store;
        }

        /// <summary>Uses exactly <paramref name="profilesRoot"/> as the Profiles folder (no override applies). Returns the store.</summary>
        public static ProfileStore UseRoot(string profilesRoot, IJsonCodec codec = null) => store = new ProfileStore(profilesRoot, codec);

        /// <summary>Uses a store you built. Returns it.</summary>
        public static ProfileStore Use(ProfileStore profileStore) => store = profileStore ?? throw new ArgumentNullException(nameof(profileStore));

        /// <summary>Forgets the store, so the next use needs one set again (tests).</summary>
        public static void Reset() => store = null;

        /// <summary>The Profiles folder the environment variable or command line names, or null.</summary>
        public static string OverrideRoot()
        {
            string env = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            string[] args;
            try
            {
                args = Environment.GetCommandLineArgs();
            }
            catch (NotSupportedException)
            {
                return null;
            }
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.Equals(a, RootCommandLineSwitch, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
                if (a.StartsWith(RootCommandLineSwitch + "=", StringComparison.OrdinalIgnoreCase))
                    return a.Substring(RootCommandLineSwitch.Length + 1);
            }
            return null;
        }
    }
}
