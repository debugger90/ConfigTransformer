using System.Text;
using Microsoft.Extensions.Configuration;

namespace ConfigTransformer.Tests;

public class ConfigTransformerTests
{
    private static IConfiguration Transform(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Transform();

    [Fact]
    public void Replaces_placeholder_with_value_from_another_section()
    {
        var config = Transform(new()
        {
            ["Settings:Api:BaseUrl"] = "https://api.example.com",
            ["MyService:Endpoint"] = "{Settings:Api:BaseUrl}/v1/data",
        });

        Assert.Equal("https://api.example.com/v1/data", config["MyService:Endpoint"]);
    }

    [Fact]
    public void Replaces_several_placeholders_in_one_value()
    {
        var config = Transform(new()
        {
            ["Db:Host"] = "db.local",
            ["Db:Port"] = "5432",
            ["Db:Connection"] = "Host={Db:Host};Port={Db:Port}",
        });

        Assert.Equal("Host=db.local;Port=5432", config["Db:Connection"]);
    }

    [Fact]
    public void Resolves_chained_placeholders()
    {
        var config = Transform(new()
        {
            ["A"] = "{B}/a",
            ["B"] = "{C}/b",
            ["C"] = "root",
        });

        Assert.Equal("root/b/a", config["A"]);
        Assert.Equal("root/b", config["B"]);
    }

    [Fact]
    public void Leaves_unknown_placeholders_untouched()
    {
        var config = Transform(new()
        {
            ["Url"] = "{Missing:Key}/path",
        });

        Assert.Equal("{Missing:Key}/path", config["Url"]);
    }

    [Fact]
    public void Leaves_values_without_placeholders_untouched()
    {
        var config = Transform(new()
        {
            ["Plain"] = "no placeholders here",
            ["Json"] = "{\"not\":\"a key\"}",
        });

        Assert.Equal("no placeholders here", config["Plain"]);
        Assert.Equal("{\"not\":\"a key\"}", config["Json"]);
    }

    [Fact]
    public void Stops_on_circular_placeholders_without_hanging()
    {
        var config = Transform(new()
        {
            ["A"] = "{B}",
            ["B"] = "{A}",
        });

        Assert.Contains("{", config["A"]);
    }

    [Fact]
    public void Replaces_placeholders_in_every_array_element()
    {
        var config = Transform(new()
        {
            ["Host"] = "example.com",
            ["Urls:0"] = "https://{Host}/first",
            ["Urls:1"] = "https://{Host}/second",
        });

        Assert.Equal("https://example.com/first", config["Urls:0"]);
        Assert.Equal("https://example.com/second", config["Urls:1"]);
    }

    [Fact]
    public void Replaces_placeholders_inside_objects_in_an_array()
    {
        var config = Transform(new()
        {
            ["Host"] = "example.com",
            ["Endpoints:0:Url"] = "https://{Host}/a",
            ["Endpoints:1:Url"] = "https://{Host}/b",
        });

        Assert.Equal("https://example.com/a", config["Endpoints:0:Url"]);
        Assert.Equal("https://example.com/b", config["Endpoints:1:Url"]);
    }

    [Fact]
    public void Resolves_nested_placeholders_to_build_a_key()
    {
        var config = Transform(new()
        {
            ["Environment"] = "Staging",
            ["Urls:Staging"] = "https://staging.example.com",
            ["Urls:Production"] = "https://example.com",
            ["Client:BaseUrl"] = "{Urls:{Environment}}",
        });

        Assert.Equal("https://staging.example.com", config["Client:BaseUrl"]);
    }

    [Fact]
    public void Throws_when_builder_is_null()
    {
        IConfigurationBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() => builder.Transform());
    }

    [Fact]
    public void Resolves_values_supplied_by_a_later_provider()
    {
        Environment.SetEnvironmentVariable("CT_TEST__Api__BaseUrl", "https://override.example.com");
        try
        {
            var json = """
                {
                  "Api": { "BaseUrl": "https://default.example.com" },
                  "Client": { "Endpoint": "{Api:BaseUrl}/v1" }
                }
                """;

            var config = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                .AddEnvironmentVariables("CT_TEST__")
                .Transform();

            Assert.Equal("https://override.example.com/v1", config["Client:Endpoint"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CT_TEST__Api__BaseUrl", null);
        }
    }

    [Fact]
    public void Works_on_ConfigurationManager_in_place()
    {
        var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://api.example.com",
            ["Client:Endpoint"] = "{Api:BaseUrl}/v1",
        });

        manager.Transform();

        Assert.Equal("https://api.example.com/v1", manager["Client:Endpoint"]);
    }
}
