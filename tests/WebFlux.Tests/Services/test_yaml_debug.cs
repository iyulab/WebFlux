using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var yamlContent = """
    title: "Test"
    author:
      name: "John"
      email: "john@example.com"
    """;

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build();

var parsed = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

Console.WriteLine($"Keys: {string.Join(", ", parsed.Keys)}");
Console.WriteLine($"Title type: {parsed["title"].GetType().Name}");
Console.WriteLine($"Author exists: {parsed.ContainsKey("author")}");

if (parsed.TryGetValue("author", out var author))
{
    Console.WriteLine($"Author type: {author.GetType().FullName}");
    Console.WriteLine($"Is Dictionary<string,object>? {author is Dictionary<string, object>}");
    Console.WriteLine($"Is IDictionary? {author is System.Collections.IDictionary}");
    
    if (author is System.Collections.IDictionary dict)
    {
        Console.WriteLine($"Dict keys: {string.Join(", ", dict.Keys.Cast<object>())}");
        foreach (var key in dict.Keys)
        {
            Console.WriteLine($"  {key} ({key.GetType().Name}): {dict[key]}");
        }
    }
}
