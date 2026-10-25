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
        string savedMutedEnv;

        [SetUp]
        public void SetUp()
        {
            dir = new TempFolder();
            savedEnv = Environment.GetEnvironmentVariable(Profiles.RootEnvironmentVariable);
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, null);
            savedMutedEnv = Environment.GetEnvironmentVariable(Profiles.MutedEnvironmentVariable);
            Environment.SetEnvironmentVariable(Profiles.MutedEnvironmentVariable, null);
            Profiles.SessionMuted = false;
            Profiles.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(Profiles.RootEnvironmentVariable, savedEnv);
            Environment.SetEnvironmentVariable(Profiles.MutedEnvironmentVariable, savedMutedEnv);
            Profiles.SessionMuted = false;
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

        [Test]
        public void SessionMuteReportsMutedWithoutTouchingSettingsJson()
        {
            Profiles.UseDataPath(dir.Path);
            var common = Profiles.Current.Settings.Common;
            common.MusicVolume = 0.4f;
            Profiles.Current.SaveSettings();
            string path = Path.Combine(dir.Path, "Profiles", "Default", "settings.json");
            string before = File.ReadAllText(path);

            Profiles.SessionMuted = true;
            Assert.That(common.MuteMusic, Is.True);
            Assert.That(common.MuteSfx, Is.True);
            Assert.That(common.MusicVolume, Is.EqualTo(0f));
            Assert.That(common.SfxVolume, Is.EqualTo(0f));

            Profiles.Current.SaveSettings();
            Assert.That(File.ReadAllText(path), Is.EqualTo(before), "the session mute never reaches settings.json");

            Profiles.SessionMuted = false;
            Assert.That(common.MuteMusic, Is.False);
            Assert.That(common.MusicVolume, Is.EqualTo(0.4f));
        }

        [TestCase("1", true)]
        [TestCase("true", true)]
        [TestCase(" YES ", true)]
        [TestCase("0", false)]
        [TestCase("", false)]
        public void MutedEnvironmentVariableIsReadAndApplied(string value, bool muted)
        {
            Environment.SetEnvironmentVariable(Profiles.MutedEnvironmentVariable, value);
            Assert.That(Profiles.MutedOverride(), Is.EqualTo(muted));
            Assert.That(Profiles.ApplyMutedOverride(), Is.EqualTo(muted));
            Assert.That(Profiles.SessionMuted, Is.EqualTo(muted));
        }
    }
}
