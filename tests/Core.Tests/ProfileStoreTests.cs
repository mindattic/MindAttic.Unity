using System;
using System.IO;
using NUnit.Framework;

namespace Unity.Shared.Core.Tests
{
    public class ProfileStoreTests
    {
        TempFolder dir;
        ProfileStore store;

        [SetUp]
        public void SetUp()
        {
            dir = new TempFolder();
            store = new ProfileStore(dir.Combine("Profiles"));
        }

        [TearDown]
        public void TearDown() => dir.Dispose();

        [TestCase("Default")]
        [TestCase("Ryan")]
        [TestCase("Player 2")]
        [TestCase("Zoë-Ω_1")]
        [TestCase("a.b")]
        public void ValidNames(string name) => Assert.That(ProfileStore.IsValidName(name), Is.True);

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" lead")]
        [TestCase("trail ")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase(".trash")]
        [TestCase("dot.")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("c:")]
        [TestCase("what?")]
        [TestCase("star*")]
        [TestCase("q\"uote")]
        [TestCase("<x>")]
        [TestCase("pi|pe")]
        [TestCase("tab\there")]
        [TestCase("CON")]
        [TestCase("nul.txt")]
        [TestCase("com1")]
        public void InvalidNames(string name) => Assert.That(ProfileStore.IsValidName(name), Is.False);

        [Test]
        public void NamesUpTo64CharactersOnly()
        {
            Assert.That(ProfileStore.IsValidName(new string('a', 64)), Is.True);
            Assert.That(ProfileStore.IsValidName(new string('a', 65)), Is.False);
        }

        [Test]
        public void CreateMakesFolderSavesAndDefaultSettings()
        {
            var p = store.Create("Ryan");
            Assert.That(Directory.Exists(p.Folder));
            Assert.That(Directory.Exists(p.SavesFolder));
            Assert.That(p.SavesFolder, Is.EqualTo(Path.Combine(dir.Combine("Profiles"), "Ryan", "Saves")));
            Assert.That(File.Exists(p.SettingsPath));
            Assert.That(ProfileSettings.Load(p.SettingsPath).Status, Is.EqualTo(SettingsLoadStatus.Loaded));
            Assert.That(store.List(), Is.EqualTo(new[] { "Ryan" }));
        }

        [Test]
        public void CreateRefusesBadAndDuplicateNames()
        {
            store.Create("Ryan");
            Assert.Throws<InvalidOperationException>(() => store.Create("ryan"));
            Assert.Throws<ArgumentException>(() => store.Create("a/b"));
        }

        [Test]
        public void ListIsSortedIgnoringCaseAndSkipsTrashAndStrays()
        {
            store.Create("bravo");
            store.Create("Alpha");
            store.Create("charlie");
            store.Delete("charlie");
            Directory.CreateDirectory(Path.Combine(store.Root, ".hidden"));
            File.WriteAllText(Path.Combine(store.Root, "loose.txt"), "");
            Assert.That(store.List(), Is.EqualTo(new[] { "Alpha", "bravo" }));
        }

        [Test]
        public void CurrentWithNothingThereCreatesAndSelectsDefault()
        {
            var p = store.Current;
            Assert.That(p.Name, Is.EqualTo(ProfileStore.DefaultName));
            Assert.That(File.ReadAllText(store.CurrentFile).Trim(), Is.EqualTo("Default"));
            Assert.That(store.List(), Is.EqualTo(new[] { "Default" }));
        }

        [Test]
        public void SelectIsRememberedAcrossStores()
        {
            store.Create("A");
            store.Create("B");
            store.Select("B");
            Assert.That(new ProfileStore(store.Root).Current.Name, Is.EqualTo("B"));
            Assert.That(store.Select("a").Name, Is.EqualTo("A"), "select ignores case and returns the real name");
            Assert.Throws<ArgumentException>(() => store.Select("Nobody"));
        }

        [Test]
        public void CurrentFallsBackToFirstProfileWhenCurrentTxtIsStale()
        {
            store.Create("Zed");
            store.Create("Amy");
            Directory.CreateDirectory(store.Root);
            File.WriteAllText(store.CurrentFile, "Gone");
            Assert.That(store.Current.Name, Is.EqualTo("Amy"));
            File.WriteAllText(store.CurrentFile, "../../etc");
            Assert.That(store.Current.Name, Is.EqualTo("Amy"));
        }

        [Test]
        public void SameProfileObjectSoSettingsChangesAreShared()
        {
            store.Create("A");
            store.Select("A");
            store.Current.Settings.Common.DisplayName = "Ace";
            Assert.That(store.Get("a").Settings.Common.DisplayName, Is.EqualTo("Ace"));
            Assert.That(store.Current, Is.SameAs(store.Get("A")));
        }

        [Test]
        public void RenameMovesFolderKeepsSettingsAndFollowsCurrent()
        {
            var p = store.Create("Old");
            store.Select("Old");
            p.Settings.Common.MuteMusic = true;
            p.SaveSettings();
            File.WriteAllText(Path.Combine(p.SavesFolder, "slot1.sav"), "data");

            var renamed = store.Rename("Old", "New");
            Assert.That(renamed, Is.SameAs(p));
            Assert.That(p.Name, Is.EqualTo("New"));
            Assert.That(store.List(), Is.EqualTo(new[] { "New" }));
            Assert.That(File.ReadAllText(Path.Combine(p.SavesFolder, "slot1.sav")), Is.EqualTo("data"));
            Assert.That(new ProfileStore(store.Root).Current.Name, Is.EqualTo("New"));
            Assert.That(p.ReloadSettings().Common.MuteMusic, Is.True);
            Assert.That(p.Settings.Common.DisplayName, Is.EqualTo("New"), "an unset display name follows the profile name");
        }

        [Test]
        public void RenameCaseOnlyWorks()
        {
            store.Create("ryan");
            store.Rename("ryan", "Ryan");
            Assert.That(store.List(), Is.EqualTo(new[] { "Ryan" }));
        }

        [Test]
        public void RenameRefusesClashesBadNamesAndMissingProfiles()
        {
            store.Create("A");
            store.Create("B");
            Assert.Throws<InvalidOperationException>(() => store.Rename("A", "b"));
            Assert.Throws<ArgumentException>(() => store.Rename("A", "bad/name"));
            Assert.Throws<ArgumentException>(() => store.Rename("Nobody", "C"));
        }

        [Test]
        public void DeleteMovesToTrashNeverErases()
        {
            var p = store.Create("Doomed");
            File.WriteAllText(Path.Combine(p.SavesFolder, "slot.sav"), "keep");
            store.Select("Doomed");

            string trashed = store.Delete("Doomed");
            Assert.That(Path.GetDirectoryName(trashed), Is.EqualTo(store.TrashFolder));
            Assert.That(Path.GetFileName(trashed), Does.StartWith("Doomed~"));
            Assert.That(File.ReadAllText(Path.Combine(trashed, "Saves", "slot.sav")), Is.EqualTo("keep"));
            Assert.That(store.Exists("Doomed"), Is.False);
            Assert.That(File.Exists(store.CurrentFile), Is.False);
            Assert.That(store.Current.Name, Is.EqualTo(ProfileStore.DefaultName), "nothing left, so a Default is made");
        }

        [Test]
        public void DeletingTheSameNameTwiceKeepsBothInTrash()
        {
            store.Create("X");
            string first = store.Delete("X");
            store.Create("X");
            string second = store.Delete("X");
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(Directory.Exists(first) && Directory.Exists(second));
        }

        [Test]
        public void DamagedSettingsInAProfileNeverThrow()
        {
            var p = store.Create("A");
            File.WriteAllText(p.SettingsPath, "{{{{");
            ProfileSettings s = null;
            Assert.DoesNotThrow(() => s = store.Get("A").ReloadSettings());
            Assert.That(s.Status, Is.EqualTo(SettingsLoadStatus.Damaged));
            Assert.That(Directory.GetFiles(p.Folder, "settings.json.bad-*").Length, Is.EqualTo(1));
            p.SaveSettings();
            Assert.That(p.ReloadSettings().Status, Is.EqualTo(SettingsLoadStatus.Loaded));
        }

        [Test]
        public void ForDataPathUsesTheProfilesSubfolder()
        {
            var s = ProfileStore.ForDataPath(dir.Path);
            Assert.That(s.Root, Is.EqualTo(Path.Combine(dir.Path, "Profiles")));
            Assert.That(s.CurrentFile, Is.EqualTo(Path.Combine(dir.Path, "Profiles", "current.txt")));
        }
    }
}
