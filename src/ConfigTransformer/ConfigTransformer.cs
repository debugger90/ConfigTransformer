using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Resolves <c>{Section:Key}</c> placeholders in configuration values using other values
/// from the same configuration.
/// </summary>
/// <remarks>
/// The type lives in the global namespace so that <see cref="Transform"/> is available
/// without an extra <c>using</c> directive, as it has been since version 1.0.
/// </remarks>
public static partial class ConfigTransformer
{
    /// <summary>
    /// The maximum number of passes over the configuration. Each pass resolves one more level
    /// of chained placeholders; the limit stops circular references from looping forever.
    /// </summary>
    public const int MaxPasses = 10;

    /// <summary>
    /// Builds the configuration and replaces every <c>{Section:Key}</c> placeholder with the
    /// value of that key. Placeholders whose key does not exist are left unchanged.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder. Call this after every configuration source has been added.
    /// </param>
    /// <returns>
    /// The built configuration with placeholders resolved. For an ASP.NET Core
    /// <see cref="ConfigurationManager"/> this is the manager itself, so the result can be ignored.
    /// </returns>
    /// <example>
    /// <code>
    /// builder.Configuration
    ///     .AddJsonFile("appsettings.json")
    ///     .AddEnvironmentVariables()
    ///     .Transform();
    /// </code>
    /// </example>
    public static IConfiguration Transform(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IConfiguration config = builder.Build();

        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool changed = false;

            foreach (IConfigurationSection leaf in GetLeaves(config))
            {
                string value = leaf.Value!;
                if (!value.Contains('{'))
                {
                    continue;
                }

                string resolved = ReplacePlaceholders(value, config);
                if (resolved != value)
                {
                    leaf.Value = resolved;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        return config;
    }

    internal static string ReplacePlaceholders(string input, IConfiguration config) =>
        PlaceholderPattern().Replace(input, match => config[match.Groups[1].Value] ?? match.Value);

    // Materialized up front so that values can be updated while iterating.
    private static List<IConfigurationSection> GetLeaves(IConfiguration config)
    {
        var leaves = new List<IConfigurationSection>();
        var pending = new Stack<IConfigurationSection>(config.GetChildren());

        while (pending.Count > 0)
        {
            IConfigurationSection section = pending.Pop();
            if (section.Value is not null)
            {
                leaves.Add(section);
            }

            foreach (IConfigurationSection child in section.GetChildren())
            {
                pending.Push(child);
            }
        }

        return leaves;
    }

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex PlaceholderPattern();
}
