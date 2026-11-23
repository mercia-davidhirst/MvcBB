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
    }
}
