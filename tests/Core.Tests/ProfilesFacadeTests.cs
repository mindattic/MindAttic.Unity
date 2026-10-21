using System;
using System.IO;
using NUnit.Framework;

namespace Unity.Shared.Core.Tests
{
    [NonParallelizable]
    public class ProfilesFacadeTests
    {
        TempFolder dir;
        string savedEnv;

        [SetUp]
        public void SetUp()
        {
            dir = new TempFolder();
            savedEnv = Environment.GetEnvironmentVariable(Profiles.RootEnvironmentVariable);
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, null);
            Profiles.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, savedEnv);
            Profiles.Reset();
            dir.Dispose();
        }

        [Test]
        public void UnconfiguredThrowsAClearError()
        {
            var e = Assert.Throws<InvalidOperationException>(() => { var _ = Profiles.Current; });
            Assert.That(e.Message, Does.Contain("Profiles.UseRoot"));
        }

        [Test]
        public void TheOneLineIdentityApiReadsAndWritesNameAndColour()
        {
            Profiles.UseDataPath(dir.Path);
            Assert.That(Profiles.Current.Settings.Common.DisplayName, Is.EqualTo("Default"));

            Profiles.Current.Settings.Common.DisplayName = "Ryan";
            Profiles.Current.Settings.Common.Color = RgbColor.Parse("#FF8800");
            Profiles.Current.SaveSettings();

            Profiles.Reset();
            Profiles.UseDataPath(dir.Path);
            Assert.That(Profiles.Current.Settings.Common.DisplayName, Is.EqualTo("Ryan"));
            Assert.That(Profiles.Current.Settings.Common.Color.ToHex(), Is.EqualTo("#FF8800"));
            Assert.That(File.Exists(Path.Combine(dir.Path, "Profiles", "Default", "settings.json")));
        }

        [Test]
        public void EnvironmentVariableOverridesTheDataPath()
        {
            string overrideRoot = dir.Combine("Automation");
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, overrideRoot);
            Profiles.UseDataPath(dir.Combine("Ignored"));
            Assert.That(Profiles.Store.Root, Is.EqualTo(Path.GetFullPath(overrideRoot)));
            Profiles.Reset();
            Assert.That(Profiles.Store.Root, Is.EqualTo(Path.GetFullPath(overrideRoot)), "the override alone configures the store");
        }

        [Test]
        public void UseRootIsExactAndWinsOverTheEnvironment()
        {
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, dir.Combine("Env"));
            Profiles.UseRoot(dir.Combine("Exact"));
            Assert.That(Profiles.Store.Root, Is.EqualTo(dir.Combine("Exact")));
            Assert.That(Profiles.IsConfigured, Is.True);
        }
    }
}
