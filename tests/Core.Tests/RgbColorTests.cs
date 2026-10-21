using NUnit.Framework;

namespace Unity.Shared.Core.Tests
{
    public class RgbColorTests
    {
        [TestCase("#12ABef", 0x12, 0xAB, 0xEF)]
        [TestCase("000000", 0, 0, 0)]
        [TestCase(" #FFFFFF ", 255, 255, 255)]
        public void ParsesHex(string text, int r, int g, int b)
        {
            Assert.That(RgbColor.TryParse(text, out var c), Is.True);
            Assert.That(c, Is.EqualTo(new RgbColor((byte)r, (byte)g, (byte)b)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("#FFF")]
        [TestCase("#GGGGGG")]
        [TestCase("#1234567")]
        public void RejectsNonHex(string text) => Assert.That(RgbColor.TryParse(text, out _), Is.False);

        [Test]
        public void FloatsRoundTripAndClamp()
        {
            var c = RgbColor.FromFloats(1f, 0.5f, -3f);
            Assert.That(c, Is.EqualTo(new RgbColor(255, 128, 0)));
            Assert.That(c.RedFloat, Is.EqualTo(1f));
        }

        [Test]
        public void ForNameIsStableIgnoresCaseAndUsesThePalette()
        {
            Assert.That(RgbColor.ForName("Ryan"), Is.EqualTo(RgbColor.ForName("ryan")));
            Assert.That(RgbColor.Palette, Does.Contain(RgbColor.ForName("anything")));
            Assert.That(RgbColor.ForName("Default").ToHex(), Is.EqualTo(RgbColor.ForName("Default").ToHex()));
        }
    }
}
