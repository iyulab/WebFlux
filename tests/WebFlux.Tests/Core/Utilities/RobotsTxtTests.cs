using AwesomeAssertions;
using WebFlux.Core.Utilities;

namespace WebFlux.Tests.Core.Utilities;

/// <summary>
/// robots.txt 파싱·매칭 단위 테스트 — RFC 9309 기준.
/// </summary>
/// <remarks>
/// <para>
/// 이 매처는 옵션이 크롤 루프에 연결되기 전까지 <em>한 번도 실행되지 않았다</em>. 연결되자마자
/// 정본 allow-all 파일(<c>User-agent: *</c> + 빈 <c>Disallow:</c>)이 사이트 전체 차단으로 읽혔고,
/// 소비자가 게시본에서 재현해 보고했다. 그래서 이 스위트는 «지금 동작» 이 아니라
/// <see href="https://www.rfc-editor.org/rfc/rfc9309.html">RFC 9309</see> 의 규범 문구에서 거꾸로 쓴다.
/// </para>
/// </remarks>
public class RobotsTxtTests
{
    private const string Agent = "WebFlux";

    private static bool Allowed(string robotsTxt, string url, string userAgent = Agent)
        => RobotsTxt.IsAllowed(RobotsTxt.Parse(robotsTxt), url, userAgent);

    // ── RFC 9309 section 2.2.2: "empty-pattern = *WS" — a rule with no value imposes no restriction.

    [Fact]
    public void EmptyDisallow_AllowsEverything()
    {
        // The canonical "everything is allowed" file. Reported by a consumer as blocking the site.
        const string robots = "User-agent: *\nDisallow:\n";

        Allowed(robots, "https://example.com/pub/x.html").Should().BeTrue();
        Allowed(robots, "https://example.com/").Should().BeTrue();
        Allowed(robots, "https://example.com/anything/at/all?q=1").Should().BeTrue();
    }

    [Fact]
    public void EmptyAllow_IsNotARuleAndDoesNotOverrideADisallow()
    {
        const string robots = "User-agent: *\nAllow:\nDisallow: /private\n";

        Allowed(robots, "https://example.com/private/x.html").Should().BeFalse();
        Allowed(robots, "https://example.com/pub/x.html").Should().BeTrue();
    }

    [Fact]
    public void DisallowRoot_BlocksTheWholeSite()
    {
        // The distinction the empty value must not collapse into.
        const string robots = "User-agent: *\nDisallow: /\n";

        Allowed(robots, "https://example.com/pub/x.html").Should().BeFalse();
        Allowed(robots, "https://example.com/").Should().BeFalse();
    }

    // ── RFC 9309 section 2.2.1: consecutive user-agent lines form one group;
    //    multiple matching groups "MUST be combined into one group".

    [Fact]
    public void ConsecutiveUserAgentLines_ShareTheGroupsRules()
    {
        const string robots = "User-agent: *\nUser-agent: Yandex\nDisallow: /private\n";

        Allowed(robots, "https://example.com/private/x.html").Should().BeFalse();
        Allowed(robots, "https://example.com/private/x.html", "Yandex").Should().BeFalse();
        Allowed(robots, "https://example.com/pub/x.html").Should().BeTrue();
    }

    [Fact]
    public void RepeatedGroupsForTheSameAgent_AreMergedNotOverwritten()
    {
        const string robots =
            "User-agent: Yandex\nDisallow: /first\n" +
            "\n" +
            "User-agent: *\nDisallow: /star\n" +
            "\n" +
            "User-agent: Yandex\nDisallow: /second\n";

        Allowed(robots, "https://example.com/first/x", "Yandex").Should().BeFalse();
        Allowed(robots, "https://example.com/second/x", "Yandex").Should().BeFalse();
        // The star group is not Yandex's, so it must not leak into it.
        Allowed(robots, "https://example.com/star/x", "Yandex").Should().BeTrue();
        Allowed(robots, "https://example.com/star/x").Should().BeFalse();
    }

    [Fact]
    public void UserAgentMatching_IsCaseInsensitive()
    {
        const string robots = "User-agent: WebFlux\nDisallow: /private\n";

        Allowed(robots, "https://example.com/private/x", "webflux").Should().BeFalse();
        Allowed(robots, "https://example.com/private/x", "WEBFLUX").Should().BeFalse();
    }

    [Fact]
    public void AMatchingGroupWins_OverTheStarGroup()
    {
        const string robots =
            "User-agent: *\nDisallow: /\n" +
            "\n" +
            "User-agent: WebFlux\nDisallow: /private\n";

        // Our own group says only /private is off limits; the star group must not apply to us.
        Allowed(robots, "https://example.com/pub/x").Should().BeTrue();
        Allowed(robots, "https://example.com/private/x").Should().BeFalse();
        // Another agent falls back to the star group.
        Allowed(robots, "https://example.com/pub/x", "SomeoneElse").Should().BeFalse();
    }

    [Fact]
    public void NoMatchingGroupAndNoStarGroup_AllowsEverything()
    {
        const string robots = "User-agent: Yandex\nDisallow: /\n";

        Allowed(robots, "https://example.com/anything").Should().BeTrue();
    }

    [Fact]
    public void AnEmptyOrCommentOnlyFile_AllowsEverything()
    {
        Allowed("", "https://example.com/x").Should().BeTrue();
        Allowed("# nothing to see here\n", "https://example.com/x").Should().BeTrue();
    }

    // ── RFC 9309 section 2.2.2: "The most specific match found MUST be used. The most specific
    //    match is the match that has the most octets." / "If an allow rule and a disallow rule are
    //    equivalent, then the allow rule SHOULD be used."

    [Fact]
    public void LongestMatchWins_DisallowOverAShorterAllow()
    {
        const string robots = "User-agent: *\nAllow: /\nDisallow: /private\n";

        Allowed(robots, "https://example.com/private/x.html").Should().BeFalse();
        Allowed(robots, "https://example.com/pub/x.html").Should().BeTrue();
    }

    [Fact]
    public void LongestMatchWins_AllowOverAShorterDisallow()
    {
        const string robots = "User-agent: *\nDisallow: /private\nAllow: /private/ok\n";

        Allowed(robots, "https://example.com/private/ok/x.html").Should().BeTrue();
        Allowed(robots, "https://example.com/private/secret.html").Should().BeFalse();
    }

    [Fact]
    public void EquivalentRules_AllowWinsTheTie()
    {
        const string robots = "User-agent: *\nDisallow: /x\nAllow: /x\n";

        Allowed(robots, "https://example.com/x/y").Should().BeTrue();
    }

    [Fact]
    public void NoRuleMatchesThePath_IsAllowed()
    {
        const string robots = "User-agent: *\nDisallow: /private\n";

        Allowed(robots, "https://example.com/pub/x.html").Should().BeTrue();
    }

    // ── RFC 9309 section 2.2.3: "*" designates 0 or more instances of any character;
    //    "$" designates the end of the match pattern.

    [Fact]
    public void DollarAnchorsTheEndOfThePath()
    {
        const string robots = "User-agent: *\nDisallow: /*.pdf$\n";

        Allowed(robots, "https://example.com/a/b.pdf").Should().BeFalse();
        // The path does not end there, so the anchored pattern must not match.
        Allowed(robots, "https://example.com/a/b.pdf?download=1").Should().BeTrue();
        Allowed(robots, "https://example.com/a/b.pdf.html").Should().BeTrue();
    }

    [Fact]
    public void StarMatchesZeroOrMoreOfAnyCharacter()
    {
        const string robots = "User-agent: *\nDisallow: /private/*/secret\n";

        Allowed(robots, "https://example.com/private/a/secret").Should().BeFalse();
        Allowed(robots, "https://example.com/private//secret").Should().BeFalse();
        Allowed(robots, "https://example.com/private/a/b/secret").Should().BeFalse();
        Allowed(robots, "https://example.com/private/a/public").Should().BeTrue();
    }

    [Fact]
    public void ATrailingStarIsTheSameAsAPrefix()
    {
        const string robots = "User-agent: *\nDisallow: /private*\n";

        Allowed(robots, "https://example.com/private").Should().BeFalse();
        Allowed(robots, "https://example.com/private/x").Should().BeFalse();
        Allowed(robots, "https://example.com/pub").Should().BeTrue();
    }

    [Fact]
    public void AQueryStringIsPartOfTheMatchedPath()
    {
        // From google.com's own file: "Disallow: /index.html?"
        const string robots = "User-agent: *\nDisallow: /index.html?\n";

        Allowed(robots, "https://example.com/index.html?q=1").Should().BeFalse();
        Allowed(robots, "https://example.com/index.html").Should().BeTrue();
    }

    // ── The real-world file the consumer reported on.

    /// <summary>
    /// A faithful excerpt of <c>https://www.google.com/robots.txt</c> as fetched 2026-09-20.
    /// It is the fixture because it exercises three of this matcher's rules at once: consecutive
    /// user-agent lines, a group repeated for the same agent, and longest-match between an Allow
    /// and a Disallow that share a prefix.
    /// </summary>
    private const string GoogleRobotsExcerpt =
        "User-agent: *\n" +
        "User-agent: Yandex\n" +
        "Disallow: /search\n" +
        "Allow: /search/about\n" +
        "Allow: /search/howsearchworks\n" +
        "Disallow: /sdch\n" +
        "Disallow: /groups\n" +
        "Disallow: /index.html?\n" +
        "\n" +
        "# AdsBot\n" +
        "User-agent: AdsBot-Google\n" +
        "Disallow: /maps/api/js/\n" +
        "Allow: /maps/api/js\n" +
        "Disallow: /maps/api/staticmap\n" +
        "\n" +
        "User-agent: Yandex\n" +
        "Disallow: /about/careers/applications/jobs/results\n" +
        "\n" +
        "User-agent: facebookexternalhit\n" +
        "User-agent: Twitterbot\n" +
        "Allow: /imgres\n" +
        "Disallow: /groups\n" +
        "\n" +
        "Sitemap: https://www.google.com/sitemap.xml\n";

    [Fact]
    public void GoogleRobots_TheStarGroupOwnsTheRulesUnderTheSecondUserAgentLine()
    {
        // The reported symptom: because "*" was stored with no rules, /search was fetched.
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search?q=x").Should().BeFalse();
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/groups").Should().BeFalse();
    }

    [Fact]
    public void GoogleRobots_TheLongerAllowUncoversASubtree()
    {
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search/about").Should().BeTrue();
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search/howsearchworks").Should().BeTrue();
    }

    [Fact]
    public void GoogleRobots_YandexGetsBothOfItsGroups()
    {
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search?q=x", "Yandex").Should().BeFalse();
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/about/careers/applications/jobs/results", "Yandex")
            .Should().BeFalse();
    }

    [Fact]
    public void GoogleRobots_LongestMatchBetweenNeighbouringAllowAndDisallow()
    {
        // "Disallow: /maps/api/js/" (22) vs "Allow: /maps/api/js" (19): the trailing slash is longer.
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/maps/api/js/x", "AdsBot-Google").Should().BeFalse();
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/maps/api/js", "AdsBot-Google").Should().BeTrue();
    }

    [Fact]
    public void GoogleRobots_AnUnlistedAgentFallsBackToTheStarGroup()
    {
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search?q=x", "WebFlux").Should().BeFalse();
        Allowed(GoogleRobotsExcerpt, "https://www.google.com/search/about", "WebFlux").Should().BeTrue();
    }

    [Fact]
    public void Parse_KeepsSitemapsAndCrawlDelay()
    {
        const string robots =
            "User-agent: *\n" +
            "Crawl-delay: 7\n" +
            "Disallow: /private\n" +
            "Sitemap: https://example.com/sitemap.xml\n";

        var info = RobotsTxt.Parse(robots);

        info.Sitemaps.Should().ContainSingle().Which.Should().Be("https://example.com/sitemap.xml");
        info.Rules["*"].CrawlDelay.Should().Be(7);
    }
}
