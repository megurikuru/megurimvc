using System.Collections.Generic;

namespace Meguri.ViewModels;

public sealed class PaginationViewModel
{
    public string? Controller { get; init; }
    public string Action { get; init; } = "Index";

    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }
    public int TotalCount { get; init; }
    public int Skip { get; init; }
    public int PreviousSkip { get; init; }
    public int NextSkip { get; init; }
    public int CurrentCount { get; init; }

    public int PageSize { get; init; } = 10;

    public Dictionary<string, string> RouteValues { get; init; } = new();
}
