using NUnit.Framework;
using Unity.Shared.Core.Json;

namespace Unity.Shared.Core.Tests
{
    public class JsonTests
    {
        [Test]
        public void RoundTripKeepsKeyOrderNumbersAndNesting()
        {
            const string json = "{\"z\":1,\"a\":0.6,\"big\":12345678901234567890,\"e\":1.5e-7,\"s\":\"x\\\"y\\\\z\\n\\u00e9\",\"t\":true,\"f\":false,\"n\":null,\"arr\":[1,\"two\",[],{}],\"o\":{\"k\":-0}}";
            var obj = MiniJson.ParseObject(json);
            Assert.That(obj.Keys, Is.EqualTo(new[] { "z", "a", "big", "e", "s", "t", "f", "n", "arr", "o" }));
            var again = MiniJson.ParseObject(MiniJson.Write(obj));
            Assert.That(MiniJson.Write(again), Is.EqualTo(MiniJson.Write(obj)));
            Assert.That(((JsonNumber)again["big"]).Raw, Is.EqualTo("12345678901234567890"));
            Assert.That(again.GetString("s"), Is.EqualTo("x\"y\\z\né"));
            Assert.That(again.GetDouble("a"), Is.EqualTo(0.6));
            Assert.That(again.ContainsKey("n") && again["n"] == null);
            Assert.That(again.GetArray("arr").Count, Is.EqualTo(4));
        }

        [Test]
        public void WritesIndentedLfTextWithFloatsInShortestForm()
        {
            var obj = new JsonObject().Set("v", 0.6f).Set("list", new JsonArray(new object[] { 1, true }));
            string text = MiniJson.Write(obj);
            Assert.That(text, Is.EqualTo("{\n  \"v\": 0.6,\n  \"list\": [\n    1,\n    true\n  ]\n}\n"));
            Assert.That(text, Does.Not.Contain("\r"));
        }

        [Test]
        public void SurrogatePairsAndControlCharactersSurvive()
        {
            var obj = new JsonObject().Set("s", "\U0001F600\u0001");
            var back = MiniJson.ParseObject(MiniJson.Write(obj));
            Assert.That(back.GetString("s"), Is.EqualTo("\U0001F600\u0001"));
            Assert.That(MiniJson.ParseObject("{\"s\":\"\\ud83d\\ude00\"}").GetString("s"), Is.EqualTo("\U0001F600"));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("{\"a\":}")]
        [TestCase("{\"a\":1,}")]
        [TestCase("{'a':1}")]
        [TestCase("{\"a\":01}")]
        [TestCase("{\"a\":1} x")]
        [TestCase("[1,2]")]
        [TestCase("{\"a\":\"unterminated}")]
        [TestCase("{\"a\":tru}")]
        [TestCase("{\"a\":\"\\x\"}")]
        public void BadTextThrowsJsonFormatException(string json)
        {
            Assert.Throws<JsonFormatException>(() => MiniJson.ParseObject(json));
        }

        [Test]
        public void DeepNestingIsRefusedNotAStackOverflow()
        {
            string json = "{\"a\":" + new string('[', 10000) + new string(']', 10000) + "}";
            Assert.Throws<JsonFormatException>(() => MiniJson.ParseObject(json));
        }

        [Test]
        public void TypedGettersFallBackOnMissingOrWrongType()
        {
            var obj = MiniJson.ParseObject("{\"s\":5,\"b\":\"yes\",\"n\":\"1\",\"i\":3.5,\"o\":[]}");
            Assert.That(obj.GetString("s", "d"), Is.EqualTo("d"));
            Assert.That(obj.GetBool("b", true), Is.True);
            Assert.That(obj.GetDouble("n", 2), Is.EqualTo(2));
            Assert.That(obj.GetInt("i", 7), Is.EqualTo(7));
            Assert.That(obj.GetObject("o"), Is.Null);
            Assert.That(obj.GetInt("missing", 9), Is.EqualTo(9));
        }

        [Test]
        public void SetKeepsPositionAndCloneIsDeep()
        {
            var obj = MiniJson.ParseObject("{\"a\":1,\"b\":{\"c\":2}}");
            obj.Set("a", 5);
            Assert.That(obj.Keys, Is.EqualTo(new[] { "a", "b" }));
            var copy = obj.Clone();
            copy.GetObject("b").Set("c", 3);
            Assert.That(obj.GetObject("b").GetInt("c"), Is.EqualTo(2));
            Assert.That(obj.Remove("a"), Is.True);
            Assert.That(obj.Keys, Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void ByteOrderMarkIsSkipped()
        {
            Assert.That(MiniJson.ParseObject("\uFEFF{\"a\":1}").GetInt("a"), Is.EqualTo(1));
        }
    }
}
