using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Shared.Core.Json;

namespace Unity.Shared.Core.Tests
{
    public class SettingsTests
    {
        [Test]
        public void DefaultsMatchTheStandard()
        {
            var s = ProfileSettings.CreateDefault("Alice");
            Assert.That(s.Root.GetString("format"), Is.EqualTo("mindattic.settings"));
            Assert.That(s.Version, Is.EqualTo(1));
            Assert.That(s.Root.Keys, Is.EqualTo(new[] { "format", "version", "common", "game" }));
            Assert.That(s.Common.MusicVolume, Is.EqualTo(0.6f));
            Assert.That(s.Common.SfxVolume, Is.EqualTo(0.85f));
            Assert.That(s.Common.MuteMusic, Is.False);
            Assert.That(s.Common.MuteSfx, Is.False);
            Assert.That(s.Common.ReduceMotion, Is.False);
            Assert.That(s.Common.Language, Is.EqualTo("en"));
            Assert.That(s.Common.DisplayName, Is.EqualTo("Alice"));
            Assert.That(s.Common.HasDisplayName, Is.False);
            Assert.That(s.Common.Color, Is.EqualTo(RgbColor.ForName("Alice")));
            Assert.That(s.Game.Count, Is.EqualTo(0));
            Assert.That(s.Common.Data.Keys, Is.EquivalentTo(new[] { "musicVolume", "sfxVolume", "muteMusic", "muteSfx", "reduceMotion", "language" }));
        }

        [Test]
        public void RoundTripKeepsUnknownFieldsInCommonGameAndTopLevel()
        {
            const string json = "{\"format\":\"mindattic.settings\",\"version\":1,\"extra\":{\"x\":[1,2]}," +
                                "\"common\":{\"musicVolume\":0.25,\"futureKey\":\"keep me\",\"muteSfx\":true}," +
                                "\"game\":{\"panSpeed\":0.05,\"nested\":{\"deep\":true}}}";
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("settings.json");
                File.WriteAllText(path, json);
                var s = ProfileSettings.Load(path);
                Assert.That(s.Status, Is.EqualTo(SettingsLoadStatus.Loaded));
                Assert.That(s.Common.MusicVolume, Is.EqualTo(0.25f));
                Assert.That(s.Common.MuteSfx, Is.True);
                s.Common.SfxVolume = 0.5f;
                s.Game.Set("panSpeed", 0.1f);
                s.Save(path);

                var back = ProfileSettings.Load(path);
                Assert.That(back.Common.Data.GetString("futureKey"), Is.EqualTo("keep me"));
                Assert.That(back.Common.SfxVolume, Is.EqualTo(0.5f));
                Assert.That(back.Game.GetFloat("panSpeed"), Is.EqualTo(0.1f));
                Assert.That(back.Game.GetObject("nested").GetBool("deep"), Is.True);
                Assert.That(back.Root.GetObject("extra").GetArray("x").Count, Is.EqualTo(2));
                Assert.That(((JsonNumber)back.Common.Data["musicVolume"]).Raw, Is.EqualTo("0.25"));
            }
        }

        [Test]
        public void MissingFileGivesDefaults()
        {
            using (var dir = new TempFolder())
            {
                var s = ProfileSettings.Load(dir.Combine("nope", "settings.json"), null, "Bob");
                Assert.That(s.Status, Is.EqualTo(SettingsLoadStatus.Missing));
                Assert.That(s.Common.MusicVolume, Is.EqualTo(CommonSettings.DefaultMusicVolume));
                Assert.That(s.Common.DisplayName, Is.EqualTo("Bob"));
            }
        }

        [Test]
        public void MissingAndWrongTypedKeysFallBackToDefaults()
        {
            var s = ProfileSettings.FromJson("{\"common\":{\"musicVolume\":\"loud\",\"muteMusic\":1,\"language\":\"  \"}}");
            Assert.That(s.Common.MusicVolume, Is.EqualTo(CommonSettings.DefaultMusicVolume));
            Assert.That(s.Common.MuteMusic, Is.False);
            Assert.That(s.Common.Language, Is.EqualTo("en"));
            Assert.That(s.Version, Is.EqualTo(1));
            Assert.That(s.Root.GetString("format"), Is.EqualTo("mindattic.settings"));
            Assert.That(s.Game, Is.Not.Null);
        }

        [Test]
        public void VolumesAreClampedAndNamesTrimmed()
        {
            var s = ProfileSettings.FromJson("{\"common\":{\"musicVolume\":3,\"sfxVolume\":-1}}");
            Assert.That(s.Common.MusicVolume, Is.EqualTo(1f));
            Assert.That(s.Common.SfxVolume, Is.EqualTo(0f));
            s.Common.MusicVolume = 2f;
            Assert.That(s.Common.Data.GetDouble("musicVolume"), Is.EqualTo(1));
            s.Common.DisplayName = "  " + new string('x', 40) + "  ";
            Assert.That(s.Common.DisplayName, Is.EqualTo(new string('x', CommonSettings.MaxDisplayNameLength)));
            s.Common.DisplayName = " ";
            Assert.That(s.Common.HasDisplayName, Is.False);
        }

        [TestCase("{\"common\":")]
        [TestCase("not json at all")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"format\":\"something.else\",\"version\":1}")]
        public void DamagedFileIsMovedAsideAndDefaultsUsed(string damaged)
        {
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("settings.json");
                File.WriteAllText(path, damaged);
                ProfileSettings s = null;
                Assert.DoesNotThrow(() => s = ProfileSettings.Load(path));
                Assert.That(s.Status, Is.EqualTo(SettingsLoadStatus.Damaged));
                Assert.That(s.Common.MusicVolume, Is.EqualTo(CommonSettings.DefaultMusicVolume));
                Assert.That(File.Exists(path), Is.False);
                Assert.That(s.MovedAsidePath, Does.StartWith(path + ".bad-"));
                Assert.That(File.ReadAllText(s.MovedAsidePath), Is.EqualTo(damaged));
            }
        }

        [Test]
        public void TwoDamagedFilesInTheSameInstantBothSurvive()
        {
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("settings.json");
                File.WriteAllText(path, "bad one");
                var first = ProfileSettings.Load(path).MovedAsidePath;
                File.WriteAllText(path, "bad two");
                var second = ProfileSettings.Load(path).MovedAsidePath;
                Assert.That(second, Is.Not.EqualTo(first));
                Assert.That(Directory.GetFiles(dir.Path, "settings.json.bad-*").Length, Is.EqualTo(2));
            }
        }

        [Test]
        public void SaveIsAtomicAndLeavesNoTempFile()
        {
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("p", "settings.json");
                var s = ProfileSettings.CreateDefault("A");
                s.Save(path);
                File.WriteAllText(path + AtomicFile.TempSuffix, "stale temp from a crash");
                s.Common.MuteMusic = true;
                s.Save(path);
                Assert.That(File.Exists(path + AtomicFile.TempSuffix), Is.False);
                Assert.That(ProfileSettings.Load(path).Common.MuteMusic, Is.True);
                Assert.That(Directory.GetFiles(dir.Combine("p")).Select(Path.GetFileName), Is.EqualTo(new[] { "settings.json" }));
            }
        }

        [Test]
        public void FailedWriteLeavesTheOldFileIntact()
        {
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("settings.json");
                var s = ProfileSettings.CreateDefault("A");
                s.Common.Language = "fr";
                s.Save(path);
                string before = File.ReadAllText(path);

                // The temp path is a folder, so the write cannot even start: the real file must not change.
                Directory.CreateDirectory(path + AtomicFile.TempSuffix);
                s.Common.Language = "de";
                Assert.That(() => s.Save(path), Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
                Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            }
        }

        [Test]
        public void WritesUtf8WithoutBomAndLfOnly()
        {
            using (var dir = new TempFolder())
            {
                string path = dir.Combine("settings.json");
                var s = ProfileSettings.CreateDefault("A");
                s.Common.DisplayName = "Zoë";
                s.Save(path);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(bytes[0], Is.EqualTo((byte)'{'));
                Assert.That(bytes, Does.Not.Contain((byte)'\r'));
                Assert.That(ProfileSettings.Load(path).Common.DisplayName, Is.EqualTo("Zoë"));
            }
        }

        [Test]
        public void NewerVersionKeepsItsNumber()
        {
            var s = ProfileSettings.FromJson("{\"format\":\"mindattic.settings\",\"version\":2,\"common\":{}}");
            Assert.That(s.Version, Is.EqualTo(2));
            Assert.That(ProfileSettings.FromJson(s.ToJson()).Version, Is.EqualTo(2));
        }

        [Test]
        public void ColorIsStoredAsHexAndBadHexFallsBack()
        {
            var s = ProfileSettings.CreateDefault("A");
            s.Common.Color = new RgbColor(0x12, 0xAB, 0xEF);
            Assert.That(s.Common.Data.GetString("color"), Is.EqualTo("#12ABEF"));
            Assert.That(ProfileSettings.FromJson(s.ToJson(), null, "A").Common.Color, Is.EqualTo(new RgbColor(0x12, 0xAB, 0xEF)));
            s.Common.Data.Set("color", "chartreuse");
            Assert.That(s.Common.HasColor, Is.False);
            Assert.That(s.Common.Color, Is.EqualTo(RgbColor.ForName("A")));
        }

        sealed class CountingCodec : IJsonCodec
        {
            public int Parses, Writes;
            public JsonObject Parse(string text) { Parses++; return MiniJson.ParseObject(text); }
            public string Write(JsonObject value) { Writes++; return MiniJson.Write(value); }
        }

        [Test]
        public void AnInjectedCodecIsUsedForReadAndWrite()
        {
            using (var dir = new TempFolder())
            {
                var codec = new CountingCodec();
                var store = new ProfileStore(dir.Path, codec);
                var p = store.Create("A");
                p.Settings.Common.MuteSfx = true;
                p.SaveSettings();
                Assert.That(codec.Writes, Is.EqualTo(2));
                Assert.That(p.ReloadSettings().Common.MuteSfx, Is.True);
                Assert.That(codec.Parses, Is.GreaterThanOrEqualTo(1));
            }
        }
    }
}
