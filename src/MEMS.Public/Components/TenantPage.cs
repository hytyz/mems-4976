using MEMS.Public.Services;
using Microsoft.AspNetCore.Components;

namespace MEMS.Public.Components;

/// <summary>
/// Base for pages under <c>/{tenant}/...</c>. Keeps the visitor's current guide selections
/// for that tenant up to date, so add/remove buttons always reflect what is stored.
/// </summary>
public abstract class TenantPage : ComponentBase, IDisposable
{
    [Inject] protected VotingGuide Guide { get; set; } = default!;
    [Inject] protected ApiClient Api { get; set; } = default!;

    [Parameter] public string Tenant { get; set; } = string.Empty;

    protected string Slug => Tenant.ToLowerInvariant();

    protected List<BallotEntry> Selections { get; private set; } = [];

    /// <summary>A friendly message when data could not be loaded.</summary>
    protected string? LoadError { get; set; }

    /// <summary>Feedback after a blocked "add to guide".</summary>
    protected string? GuideMessage { get; set; }

    protected override void OnInitialized() => Guide.Changed += OnGuideChanged;

    protected async Task RefreshSelectionsAsync() => Selections = await Guide.GetAsync(Slug);

    protected void ShowGuideMessage(string message) => GuideMessage = message;

    /// <summary>Runs a load, turning an unreachable API into a message instead of a crash.</summary>
    protected async Task LoadSafelyAsync(Func<Task> load)
    {
        LoadError = null;
        try
        {
            await load();
        }
        catch (ApiUnavailableException ex)
        {
            LoadError = ex.Message;
        }
    }

    private async void OnGuideChanged(string slug)
    {
        if (string.Equals(slug, Slug, StringComparison.OrdinalIgnoreCase))
        {
            GuideMessage = null;
            await RefreshSelectionsAsync();
            await OnSelectionsChangedAsync();
            StateHasChanged();
        }
    }

    protected virtual Task OnSelectionsChangedAsync() => Task.CompletedTask;

    public virtual void Dispose()
    {
        Guide.Changed -= OnGuideChanged;
        GC.SuppressFinalize(this);
    }
}
