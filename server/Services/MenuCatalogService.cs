using System.Text.Json;
using BhaiGCafe.Api.Models;

namespace BhaiGCafe.Api.Services;

public sealed class MenuCatalogService
{
    private readonly IReadOnlyDictionary<string, MenuItem> _menuById;

    public MenuCatalogService(IWebHostEnvironment environment)
    {
        var repoRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;
        var menuPath = Path.Combine(repoRoot, "assets", "menu.json");
        if (!File.Exists(menuPath))
        {
            _menuById = new Dictionary<string, MenuItem>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        using var stream = File.OpenRead(menuPath);
        var menu = JsonSerializer.Deserialize<List<MenuItem>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        _menuById = menu
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(item => item.Id, item => item, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<MenuItem> GetAll() => _menuById.Values.ToArray();

    public bool TryGet(string id, out MenuItem item) => _menuById.TryGetValue(id, out item!);
}
