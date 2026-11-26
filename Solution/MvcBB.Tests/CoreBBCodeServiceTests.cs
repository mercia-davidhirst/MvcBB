using MvcBB.Shared.Services;

namespace MvcBB.Tests
{
    public class CoreBBCodeServiceTests
    {
        private readonly CoreBBCodeService _service = new();

        [Fact]
        public void ParseBBCode_ConvertsLineBreaksToBr()
        {
            var html = _service.ParseBBCode("First paragraph\r\n\r\nSecond line\nThird");

            Assert.Equal("First paragraph<br /><br />Second line<br />Third", html);
        }

        [Fact]
        public void ParseBBCode_KeepsCodeBlockNewlinesVerbatim()
        {
            var html = _service.ParseBBCode("Before\n[code]line 1\nline 2[/code]\nAfter");

            Assert.Equal("Before<pre><code>line 1\nline 2</code></pre>After", html);
        }

        [Fact]
        public void ParseBBCode_DropsSingleNewlineAroundQuoteTags()
        {
            var html = _service.ParseBBCode("Intro\n[quote]\nQuoted\ntext\n[/quote]\nReply");

            Assert.Equal("Intro<blockquote>Quoted<br />text</blockquote>Reply", html);
        }

        [Fact]
        public void ParseBBCode_StillEncodesUserMarkup()
        {
            var html = _service.ParseBBCode("<pre>\nx\n</pre>");

            Assert.Equal("&lt;pre&gt;<br />x<br />&lt;/pre&gt;", html);
        }

        [Fact]
        public void ParseBBCode_RendersSpoilerAsClosedDetails()
        {
            var html = _service.ParseBBCode("[spoiler]The butler did it[/spoiler]");

            Assert.Equal("<details class=\"bbcode-spoiler\"><summary>Spoiler</summary><div class=\"bbcode-spoiler-body\">The butler did it</div></details>", html);
        }

        [Fact]
        public void ParseBBCode_SpoilerTitleIsEncoded()
        {
            var html = _service.ParseBBCode("[spoiler=<b>Ending</b>]x[/spoiler]");

            Assert.Contains("<summary>&lt;b&gt;Ending&lt;/b&gt;</summary>", html);
            Assert.DoesNotContain("<b>", html);
        }

        [Fact]
        public void StripBBCode_HidesSpoilerText()
        {
            var text = _service.StripBBCode("Before [spoiler=Ending]secret [spoiler]deeper[/spoiler] more[/spoiler] after");

            Assert.Equal("Before (spoiler) after", text);
        }

        [Fact]
        public void ParseBBCode_LinksKnownMentionsOnly()
        {
            var html = _service.ParseBBCode("Hi @Alice and @nobody, mail bob@example.com.", null, new[] { "alice" });

            Assert.Contains("<a class=\"mention\" href=\"/Account/Profile?username=alice\">@alice</a>", html);
            Assert.Contains("@nobody", html);
            Assert.DoesNotContain("username=nobody", html);
            Assert.Contains("bob@example.com.", html);
        }

        [Fact]
        public void ParseBBCode_LinksQuotedMentionAndKeepsTrailingFullStop()
        {
            var html = _service.ParseBBCode("Thanks @\"Some One\". And @bob.", null, new[] { "Some One", "bob" });

            Assert.Contains(">@Some One</a>. And", html);
            Assert.EndsWith(">@bob</a>.", html);
        }

        [Fact]
        public void ParseBBCode_DoesNotLinkMentionsInCode()
        {
            var html = _service.ParseBBCode("[code]@alice[/code]", null, new[] { "alice" });

            Assert.DoesNotContain("mention", html);
        }
    }

    public class MentionParserTests
    {
        [Fact]
        public void Extract_FindsBareAndQuotedNames_SkippingQuotesAndCode()
        {
            var names = MentionParser.Extract("@alice hi @\"Some One\" [quote=1]@carol[/quote] [code]@dave[/code] @Alice again, email x@y.com");

            Assert.Equal(new[] { "alice", "Some One" }, names);
        }

        [Fact]
        public void Extract_DropsTrailingPunctuation()
        {
            Assert.Equal(new[] { "bob" }, MentionParser.Extract("Ask @bob."));
        }

        [Theory]
        [InlineData("alice", "@alice")]
        [InlineData("Some One", "@\"Some One\"")]
        [InlineData("x.y-z", "@x.y-z")]
        public void Format_QuotesNamesThatNeedIt(string username, string expected)
        {
            Assert.Equal(expected, MentionParser.Format(username));
        }
    }
}
