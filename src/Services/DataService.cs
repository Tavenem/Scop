using Microsoft.JSInterop;
using Scop.Enums;
using Scop.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tavenem.Blazor.IndexedDB;
using Tavenem.Randomize;

namespace Scop.Services;

public class DataService(
    HttpClient httpClient,
    ScopJsInterop jsInterop,
    [FromKeyedServices("scop")] IndexedDb database) : IDisposable
{
    internal const string ObjectStoreName = "story_data";

    private static string[] DefaultEthnicityHierarchy { get; } = ["caucasian", "american"];

    private NameSet? _defaultNameSet;
    private bool _disposedValue;
    private DotNetObjectReference<DataService>? _dotNetObjectRef;
    private bool _loaded = false;
    private Ethnicity? _nameSetEthnicity;
    private NameSet? _nameSet;

    public ScopData Data { get; set; } = new() { Version = ScopData.CurrentVersion };

    public bool GDriveSync { get; set; }

    public DateTime LastGDriveSync { get; set; }

    public DateTime LastLocalSync { get; set; }

    public event EventHandler? DataLoaded;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dotNetObjectRef?.Dispose();
                _dotNetObjectRef = null;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task AddEthnicityAsync(Ethnicity value)
    {
        (Data.Ethnicities ??= []).Add(value);
        await SaveAsync();
    }

    public async Task AddEthnicityAsync(Ethnicity parent, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        var hierarchy = new string[(parent.Hierarchy?.Length ?? 0) + 1];
        if (parent.Hierarchy is not null)
        {
            Array.Copy(parent.Hierarchy, hierarchy, parent.Hierarchy.Length);
        }
        hierarchy[^1] = newValue;

        var newEthnicity = new Ethnicity
        {
            Hierarchy = hierarchy,
            Parent = parent,
            Type = newValue,
        };

        (parent.Types ??= []).Add(newEthnicity);
        await SaveAsync();
    }

    public async Task AddGenreAsync(Genre value)
    {
        (Data.Genres ??= []).Add(value);
        await SaveAsync();
    }

    public async Task AddPlotAsync(Plot value)
    {
        (Data.Plots ??= []).Add(value);
        await SaveAsync();
    }

    public async Task AddStoryTraitAsync(Trait value)
    {
        (Data.StoryTraits ??= []).Add(value);
        await SaveAsync();
    }

    public async Task AddStoryTraitAsync(Trait parent, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        AddNewTrait(parent, newValue);
        await SaveAsync();
    }

    public async Task AddTraitAsync(Trait value)
    {
        (Data.Traits ??= []).Add(value);
        await SaveAsync();
    }

    public async Task AddTraitAsync(Trait parent, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        AddNewTrait(parent, newValue);
        await SaveAsync();
    }

    public async Task DeleteLocalAsync()
    {
        var store = database[ObjectStoreName];
        if (store is not null)
        {
            await store.ClearAsync();
        }
        LastLocalSync = DateTime.MinValue;

        if (LastGDriveSync == DateTime.MinValue)
        {
            Data = new();
            await LoadInitialDataAsync();
            DataLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// <para>
    /// Invoked when a Google Drive file is finished loading.
    /// </para>
    /// <para>
    /// This method is invoked by internal JavaScript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    [JSInvokable]
    public async Task DriveFileLoaded(string data)
    {
        var scopData = JsonSerializer.Deserialize<ScopData>(data);
        if (scopData is not null)
        {
            LastGDriveSync = scopData.LastSync;
            if (scopData.LastSync >= Data.LastSync
                && await UpgradeAsync(scopData))
            {
                DataLoaded?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public async Task EditEthnicityAsync(Ethnicity ethnicity, string? newType = null)
    {
        newType = newType?.Trim();
        if (string.IsNullOrWhiteSpace(newType))
        {
            return;
        }
        ethnicity.Type = newType;

        if (ethnicity.Hierarchy?.Length > 0)
        {
            ethnicity.Hierarchy[^1] = newType;
            ethnicity.InitializeChildren();
        }

        await SaveAsync();
    }

    public async Task EditGenreAsync(Genre genre, string? newName)
    {
        newName = newName?.Trim();

        if (string.IsNullOrEmpty(newName)
            || Data.Genres?.Any(x => x.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) == true) == true)
        {
            return;
        }

        genre.Name = newName;
        await SaveAsync();
    }

    public async Task EditPlotAsync(Plot plot, string? newName)
    {
        newName = newName?.Trim();

        if (string.IsNullOrEmpty(newName)
            || Data.Plots?.Any(x => x.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) == true) == true)
        {
            return;
        }

        plot.Name = newName;
        await SaveAsync();
    }

    public async ValueTask<List<NameData>> GetNameListAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        return set?.GetNamesForGender(gender) ?? [];
    }

    public async ValueTask<(string? given, string? middle, string? surname)> GetRandomFullNameAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        if (set is null)
        {
            return (null, null, null);
        }
        return (
            set.GetRandomName(gender),
            Randomizer.Instance.NextDouble() < 0.9
                ? set.GetRandomName(gender)
                : null,
            set.GetRandomSurname());
    }

    public List<string[]> GetRandomEthnicities() => Data.Ethnicities is null ? [] : Ethnicity.GetRandomEthnicities(Data.Ethnicities);

    public async ValueTask<string?> GetRandomNameAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        return set?.GetRandomName(gender);
    }

    public async ValueTask<string?> GetRandomSurnameAsync(List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        return set?.GetRandomSurname();
    }

    public async ValueTask<List<NameData>> GetSurnameListAsync(List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        return set?.Surnames ?? [];
    }

    public async ValueTask LoadAsync(bool reload = false)
    {
        if (!_loaded)
        {
            await LoadInitialDataAsync();
        }
        else if (!reload)
        {
            return;
        }

        var store = database[ObjectStoreName];
        var localData = store is null
            ? null
            : await store.GetItemAsync<LocalData>(LocalData.IdValue);

        if (localData?.Data is not null)
        {
            LastLocalSync = localData.Data.LastSync;
            if (localData.Data.LastSync > Data.LastSync
                && await UpgradeAsync(localData.Data))
            {
                DataLoaded?.Invoke(this, EventArgs.Empty);
            }
        }

        if (!GDriveSync)
        {
            GDriveSync = await jsInterop.GetDriveSignedIn();
        }
        if (GDriveSync)
        {
            await LoadGDriveAsync();
        }

        _loaded = true;
    }

    public async Task RemoveEthnicityAsync(Ethnicity value)
    {
        if (value.Parent?.Types?.Remove(value) == true)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveGenreAsync(Genre value)
    {
        if (Data.Genres?.Remove(value) == true)
        {
            await SaveAsync();
        }
    }

    public async Task RemovePlotAsync(Plot value)
    {
        if (Data.Plots?.Remove(value) == true)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveStoryTraitAsync(Trait value)
    {
        if (Data.StoryTraits?.Remove(value) == true)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveTraitAsync(Trait value)
    {
        if (Data.Traits?.Remove(value) == true)
        {
            await SaveAsync();
        }
    }

    public async Task SaveAsync()
    {
        if (!(Data.Ethnicities?.Count > 0
            || Data.Genres?.Count > 0
            || Data.Plots?.Count > 0
            || Data.Stories.Count > 0
            || Data.StoryTraits?.Count > 0
            || Data.Traits?.Count > 0))
        {
            return;
        }

        await SaveLocalAsync();

        if (GDriveSync)
        {
            await SaveGDriveAsync();
        }
    }

    public async Task SaveGDriveAsync()
    {
        if (!GDriveSync)
        {
            return;
        }

        var serializedData = JsonSerializer.Serialize(
            Data,
            ScopSerializerOptions.Instance);

        await jsInterop.SaveDriveData(serializedData);

        LastGDriveSync = Data.LastSync;
    }

    public async Task SaveLocalAsync()
    {
        if (!(Data.Ethnicities?.Count > 0
            || Data.Genres?.Count > 0
            || Data.Plots?.Count > 0
            || Data.Stories.Count > 0
            || Data.StoryTraits?.Count > 0
            || Data.Traits?.Count > 0))
        {
            return;
        }

        Data.LastSync = DateTime.UtcNow;

        var localData = new LocalData { Data = Data };
        var store = database[ObjectStoreName];
        if (store is not null)
        {
            await store.StoreItemAsync(localData);
        }
        LastLocalSync = Data.LastSync;
    }

    public async Task UploadAsync(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }
        try
        {
            var data = JsonSerializer.Deserialize<ScopData>(json, ScopSerializerOptions.Instance);
            if (data is not null
                && await UpgradeAsync(data))
            {
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static void AddNewTrait(Trait parent, string newValue)
    {
        var hierarchy = new string[(parent.Hierarchy?.Length ?? 0) + 1];
        if (parent.Hierarchy is not null)
        {
            Array.Copy(parent.Hierarchy, hierarchy, parent.Hierarchy.Length);
        }
        hierarchy[^1] = newValue;

        var newTrait = new Trait
        {
            Hierarchy = hierarchy,
            Parent = parent,
            Name = newValue,
        };

        (parent.Children ??= []).Add(newTrait);
    }

    private async ValueTask AddNamesAsync(NameSet names, Ethnicity ethnicity)
    {
        var nameSet = await GetNameSetAsync(ethnicity);
        if (nameSet is not null)
        {
            if (nameSet.FemaleNames is not null)
            {
                (names.FemaleNames ??= []).AddRange(nameSet.FemaleNames);
            }
            if (nameSet.MaleNames is not null)
            {
                (names.MaleNames ??= []).AddRange(nameSet.MaleNames);
            }
            if (nameSet.Surnames is not null)
            {
                (names.Surnames ??= []).AddRange(nameSet.Surnames);
            }
        }
        else if (ethnicity.Types?.Count > 0)
        {
            var child = Randomizer.Instance.Next(ethnicity.Types)!;
            await AddNamesAsync(names, child);
        }
    }

    private async Task<List<Ethnicity>> GetDefaultEthnicitiesAsync()
        => await httpClient
            .GetFromJsonAsync<List<Ethnicity>>("./ethnicities.json")
            ?? [];

    private async Task<List<Genre>> GetDefaultGenresAsync()
        => await httpClient
            .GetFromJsonAsync<List<Genre>>("./genres.json")
            ?? [];

    private async Task<List<Plot>> GetDefaultPlotsAsync()
        => await httpClient
            .GetFromJsonAsync<List<Plot>>("./plots.json")
            ?? [];

    private async Task<List<RelationshipType>> GetDefaultRelationshipTypesAsync()
        => await httpClient
            .GetFromJsonAsync<List<RelationshipType>>("./relationships.json")
            ?? [];

    private async Task<List<Trait>> GetDefaultStoryTraitsAsync()
        => await httpClient
            .GetFromJsonAsync<List<Trait>>("./story_traits.json")
            ?? [];

    private async Task<List<Trait>> GetDefaultTraitsAsync()
        => await httpClient
            .GetFromJsonAsync<List<Trait>>("./traits.json")
            ?? [];

    private async Task<NameSet> GetFilteredNameSetAsync(List<string[]>? characterEthnicities)
    {
        var nameSet = new NameSet();
        characterEthnicities ??= [DefaultEthnicityHierarchy];

        foreach (var ethnicityPath in characterEthnicities)
        {
            if (ethnicityPath.Length == 0)
            {
                continue;
            }

            Ethnicity? match = null;
            var i = 0;
            ICollection<Ethnicity>? collection = Data.Ethnicities;
            while (i < ethnicityPath.Length && collection is not null)
            {
                match = collection.FirstOrDefault(x => string.Equals(x.Type, ethnicityPath[i], StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    break;
                }
                collection = match.Types;
                i++;
            }
            if (match is null || i < ethnicityPath.Length)
            {
                continue;
            }

            await AddNamesAsync(nameSet, match);
        }

        if (nameSet.FemaleNames is null
            || nameSet.FemaleNames.Count == 0
            || nameSet.MaleNames is null
            || nameSet.MaleNames.Count == 0
            || nameSet.Surnames is null
            || nameSet.Surnames.Count == 0)
        {
            Ethnicity? match = null;
            var i = 0;
            ICollection<Ethnicity>? collection = Data.Ethnicities;
            while (i < DefaultEthnicityHierarchy.Length && collection is not null)
            {
                match = collection.FirstOrDefault(x => string.Equals(x.Type, DefaultEthnicityHierarchy[i], StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    break;
                }
                collection = match.Types;
                i++;
            }
            if (match is not null)
            {
                await AddNamesAsync(nameSet, match);
            }
        }

        return nameSet;
    }

    private async ValueTask<NameSet?> GetNameSetAsync(Ethnicity ethnicity)
    {
        if (ethnicity.Hierarchy is null)
        {
            return null;
        }

        if (!ethnicity.HasNames)
        {
            if (ethnicity.Parent is null)
            {
                return null;
            }
            else
            {
                return await GetNameSetAsync(ethnicity.Parent);
            }
        }

        if (ethnicity.IsDefault)
        {
            if (_defaultNameSet is not null)
            {
                return _defaultNameSet;
            }
        }
        else if (ethnicity == _nameSetEthnicity
            && _nameSet is not null)
        {
            return _nameSet;
        }

        var url = new StringBuilder("./names/")
            .AppendJoin('_', ethnicity.Hierarchy)
            .Replace(' ', '_')
            .Append(".json")
            .ToString();

        var nameSet = await httpClient.GetFromJsonAsync<NameSet>(url);

        if (ethnicity.IsDefault)
        {
            _defaultNameSet = nameSet;
        }
        else
        {
            _nameSet = nameSet;
            _nameSetEthnicity = ethnicity;
        }

        return nameSet;
    }

    private async Task LoadGDriveAsync()
    {
        _dotNetObjectRef ??= DotNetObjectReference.Create(this);
        await jsInterop.LoadDriveData(_dotNetObjectRef);
    }

    private async Task LoadInitialDataAsync()
    {
        if (Data.Ethnicities is null || Data.Ethnicities.Count == 0)
        {
            Data.Ethnicities = [.. await GetDefaultEthnicitiesAsync()];
        }

        if (Data.Genres is null || Data.Genres.Count == 0)
        {
            Data.Genres = await GetDefaultGenresAsync();
        }

        if (Data.Plots is null || Data.Plots.Count == 0)
        {
            Data.Plots = await GetDefaultPlotsAsync();
        }

        if (Data.RelationshipTypes is null || Data.RelationshipTypes.Count == 0)
        {
            Data.RelationshipTypes = await GetDefaultRelationshipTypesAsync();
        }

        if (Data.StoryTraits is null || Data.StoryTraits.Count == 0)
        {
            Data.StoryTraits = [.. await GetDefaultStoryTraitsAsync()];
        }

        if (Data.Traits is null || Data.Traits.Count == 0)
        {
            Data.Traits = [.. await GetDefaultTraitsAsync()];
        }
    }

    private async Task<bool> UpgradeAsync(ScopData? scopData)
    {
        if (scopData is null)
        {
            return false;
        }

        var v1 = scopData.Version < 2;

        if (!DataMigration.Upgrade(scopData))
        {
            return false;
        }

        Data = scopData;
        if (v1)
        {
            (Data.Ethnicities ??= []).AddRange([.. (await GetDefaultEthnicitiesAsync())
                .Where(x => !Data.Ethnicities.Contains(x))]);
            (Data.Genres ??= []).AddRange([.. (await GetDefaultGenresAsync())
                .Where(x => !Data.Genres.Contains(x))]);
            (Data.Plots ??= []).AddRange([.. (await GetDefaultPlotsAsync())
                .Where(x => !Data.Plots.Contains(x))]);
            (Data.RelationshipTypes ??= []).AddRange([.. (await GetDefaultRelationshipTypesAsync())
                .Where(x => !Data.RelationshipTypes.Contains(x))]);
            (Data.StoryTraits ??= []).AddRange([.. (await GetDefaultStoryTraitsAsync())
                .Where(x => !Data.StoryTraits.Contains(x))]);
            (Data.Traits ??= []).AddRange([.. (await GetDefaultTraitsAsync())
                .Where(x => !Data.Traits.Contains(x))]);
        }
        else
        {
            await LoadInitialDataAsync();
        }
        return true;
    }
}
