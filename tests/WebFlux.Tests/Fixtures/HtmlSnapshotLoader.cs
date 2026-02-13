using System.Reflection;

namespace WebFlux.Tests.Fixtures;

/// <summary>
/// HTML 스냅샷 로더
/// 테스트용 HTML 파일을 카테고리/이름 기반으로 로드
/// </summary>
public static class HtmlSnapshotLoader
{
    private static readonly string SnapshotsDirectory;

    static HtmlSnapshotLoader()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        SnapshotsDirectory = Path.Combine(assemblyDir, "Fixtures", "HtmlSnapshots");
    }

    /// <summary>
    /// 카테고리와 이름으로 HTML 스냅샷 로드
    /// </summary>
    /// <param name="category">카테고리 (파일명 프리픽스로 사용)</param>
    /// <param name="name">스냅샷 이름 (전체 파일명에서 .html 제외)</param>
    /// <returns>HTML 문자열</returns>
    public static string Load(string category, string name)
    {
        var fileName = $"{name}.html";
        var filePath = Path.Combine(SnapshotsDirectory, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"HTML snapshot not found: {filePath}. Category={category}, Name={name}");

        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// 모든 HTML 스냅샷 로드
    /// </summary>
    public static IEnumerable<(string Category, string Name, string Html)> LoadAll()
    {
        foreach (var (category, name) in GetAllSnapshots())
        {
            var html = Load(category, name);
            yield return (category, name, html);
        }
    }

    /// <summary>
    /// 특정 카테고리의 HTML 스냅샷 로드
    /// </summary>
    public static IEnumerable<(string Category, string Name, string Html)> LoadByCategory(string category)
    {
        foreach (var (cat, name) in GetAllSnapshots())
        {
            if (string.Equals(cat, category, StringComparison.OrdinalIgnoreCase))
            {
                var html = Load(cat, name);
                yield return (cat, name, html);
            }
        }
    }

    /// <summary>
    /// 등록된 모든 스냅샷 목록 반환 (카테고리, 이름)
    /// </summary>
    public static IReadOnlyList<(string Category, string Name)> GetAllSnapshots()
    {
        return
        [
            ("News", "news-bbc"),
            ("News", "news-naver"),
            ("TechDoc", "techdoc-mdn"),
            ("TechDoc", "techdoc-mslearn"),
            ("Blog", "blog-medium"),
            ("Blog", "blog-devto"),
            ("Ecommerce", "ecom-product"),
            ("Forum", "forum-stackoverflow"),
            ("Korean", "korean-tistory"),
            ("Korean", "korean-namu"),
            ("Edge", "edge-minimal"),
            ("Edge", "edge-table-heavy"),
            ("Edge", "edge-image-heavy")
        ];
    }
}
