using Microsoft.JSInterop;
using Scop.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tavenem.Blazor.IndexedDB;
using Tavenem.Randomize;

namespace Scop;

public class DataService(
    HttpClient httpClient,
    IndexedDbService indexedDb,
    ScopJsInterop jsInterop) : IDisposable
{
    private static string[] DefaultEthnicityHierarchy { get; } = ["caucasian", "american"];

    private NameSet? _defaultNameSet;
    private bool _disposedValue;
    private DotNetObjectReference<DataService>? _dotNetObjectRef;
    private bool _loaded = false;
    private Ethnicity? _nameSetEthnicity;
    private NameSet? _nameSet;

    public ScopData Data { get; set; } = new();

    public List<Ethnicity> Ethnicities { get; set; } = [];

    public bool GDriveSync { get; set; }

    public List<Genre> Genres { get; set; } = [];

    public DateTime LastGDriveSync { get; set; }

    public DateTime LastLocalSync { get; set; }

    public List<Plot> Plots { get; set; } = [];

    public List<Trait> StoryTraits { get; set; } = [];

    public List<Trait> Traits { get; set; } = [];

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
        value.UserDefined = true;

        Data.Ethnicities?.Remove(value);
        (Data.Ethnicities ??= []).Add(value);

        var index = Ethnicities.IndexOf(value);
        if (index >= 0)
        {
            Ethnicities.RemoveAt(index);
            Ethnicities.Insert(index, value);
        }
        else
        {
            Ethnicities.Add(value);
        }

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
            UserDefined = true,
        };

        (parent.Types ??= []).Add(newEthnicity);
        var top = parent;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }
        if (Data.Ethnicities?.Any(x => x == top) != true)
        {
            (Data.Ethnicities ??= []).Add(top);
        }
        await SaveAsync();
    }

    public async Task AddGenreAsync(Genre value)
    {
        value.UserDefined = true;

        Data.Genres?.Remove(value);
        (Data.Genres ??= []).Add(value);

        var index = Genres.IndexOf(value);
        if (index >= 0)
        {
            Genres.RemoveAt(index);
            Genres.Insert(index, value);
        }
        else
        {
            Genres.Add(value);
        }

        await SaveAsync();
    }

    public async Task AddPlotAsync(Plot value)
    {
        value.UserDefined = true;

        Data.Plots?.Remove(value);
        (Data.Plots ??= []).Add(value);

        var index = Plots.IndexOf(value);
        if (index >= 0)
        {
            Plots.RemoveAt(index);
            Plots.Insert(index, value);
        }
        else
        {
            Plots.Add(value);
        }

        await SaveAsync();
    }

    public async Task AddStoryTraitAsync(Trait value)
    {
        value.UserDefined = true;

        Data.StoryTraits?.Remove(value);
        (Data.StoryTraits ??= []).Add(value);

        var index = StoryTraits.IndexOf(value);
        if (index >= 0)
        {
            StoryTraits.RemoveAt(index);
            StoryTraits.Insert(index, value);
        }
        else
        {
            StoryTraits.Add(value);
        }

        await SaveAsync();
    }

    public async Task AddStoryTraitAsync(Trait parent, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        var top = GetNewTrait(parent, newValue);
        if (Data.StoryTraits?.Any(x => x == top) != true)
        {
            (Data.StoryTraits ??= []).Add(top);
        }
        await SaveAsync();
    }

    public async Task AddTraitAsync(Trait value)
    {
        value.UserDefined = true;

        Data.Traits?.Remove(value);
        (Data.Traits ??= []).Add(value);

        var index = Traits.IndexOf(value);
        if (index >= 0)
        {
            Traits.RemoveAt(index);
            Traits.Insert(index, value);
        }
        else
        {
            Traits.Add(value);
        }

        await SaveAsync();
    }

    public async Task AddTraitAsync(Trait parent, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        var top = GetNewTrait(parent, newValue);
        if (Data.Traits?.Any(x => x == top) != true)
        {
            (Data.Traits ??= []).Add(top);
        }
        await SaveAsync();
    }

    public async Task DeleteLocalAsync()
    {
        await indexedDb.ClearAsync();
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
    public Task DriveFileLoaded(string data)
    {
        var scopData = JsonSerializer.Deserialize<ScopData>(data);
        if (scopData is not null)
        {
            LastGDriveSync = scopData.LastSync;
            if (scopData.LastSync >= Data.LastSync)
            {
                Data = scopData;
                UpdateData();
            }
        }
        return Task.CompletedTask;
    }

    public async Task EditEthnicityAsync(Ethnicity ethnicity, string? newType = null)
    {
        newType = newType?.Trim();
        if (string.IsNullOrWhiteSpace(newType))
        {
            return;
        }
        ethnicity.Type = newType;

        ethnicity.UserDefined = true;

        var top = ethnicity;
        while (ethnicity.Parent is not null)
        {
            top = ethnicity.Parent;
        }

        Data.Ethnicities?.Remove(top);
        var index = Ethnicities.FindIndex(x => x.Equals(top));

        if (ethnicity.Hierarchy?.Length > 0)
        {
            ethnicity.Hierarchy[^1] = newType;
            ethnicity.InitializeChildren();
        }

        (Data.Ethnicities ??= []).Add(top);
        if (index >= 0)
        {
            Ethnicities.RemoveAt(index);
            Ethnicities.Insert(index, top);
        }
        else
        {
            Ethnicities.Add(top);
        }

        await SaveAsync();
    }

    public async Task EditGenreAsync(Genre genre)
    {
        genre.UserDefined = true;
        Data.Genres?.Remove(genre);
        (Data.Genres ??= []).Add(genre);
        await SaveAsync();
    }

    public async Task EditGenreAsync(Genre genre, string? newName)
    {
        newName = newName?.Trim();

        if (string.IsNullOrEmpty(newName)
            || Genres.Any(x => x.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) == true))
        {
            return;
        }

        genre.UserDefined = true;
        Data.Genres?.Remove(genre);
        genre.Name = newName;
        (Data.Genres ??= []).Add(genre);
        await SaveAsync();
    }

    public async Task EditPlotAsync(Plot plot)
    {
        plot.UserDefined = true;
        Data.Plots?.Remove(plot);
        (Data.Plots ??= []).Add(plot);
        await SaveAsync();
    }

    public async Task EditPlotAsync(Plot plot, string? newName)
    {
        newName = newName?.Trim();

        if (string.IsNullOrEmpty(newName)
            || Plots.Any(x => x.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) == true))
        {
            return;
        }

        plot.UserDefined = true;
        Data.Plots?.Remove(plot);
        plot.Name = newName;
        (Data.Plots ??= []).Add(plot);
        await SaveAsync();
    }

    public async Task EditStoryTraitAsync(Trait trait)
    {
        var trimmed = trait.Name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = trait.Hierarchy is null
                ? "unknown trait"
                : trait.Hierarchy[^1];
        }
        trait.Name = trimmed;

        trait.UserDefined = true;

        var top = trait;
        while (trait.Parent is not null)
        {
            top = trait.Parent;
        }

        Data.StoryTraits?.Remove(top);
        var index = Traits.FindIndex(x => x.Equals(top));

        if (trait.Hierarchy?.Length > 0)
        {
            trait.Hierarchy[^1] = trait.Name;
            trait.InitializeChildren();
        }

        (Data.StoryTraits ??= []).Add(top);
        if (index >= 0)
        {
            StoryTraits.RemoveAt(index);
            StoryTraits.Insert(index, top);
        }
        else
        {
            StoryTraits.Add(top);
        }

        await SaveAsync();
    }

    public async Task EditTraitAsync(Trait trait)
    {
        var trimmed = trait.Name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = trait.Hierarchy is null
                ? "unknown trait"
                : trait.Hierarchy[^1];
        }
        trait.Name = trimmed;

        trait.UserDefined = true;

        var top = trait;
        while (trait.Parent is not null)
        {
            top = trait.Parent;
        }

        Data.Traits?.Remove(top);
        var index = Traits.FindIndex(x => x.Equals(top));

        if (trait.Hierarchy?.Length > 0)
        {
            trait.Hierarchy[^1] = trait.Name;
            trait.InitializeChildren();
        }

        (Data.Traits ??= []).Add(top);
        if (index >= 0)
        {
            Traits.RemoveAt(index);
            Traits.Insert(index, top);
        }
        else
        {
            Traits.Add(top);
        }

        await SaveAsync();
    }

    public async ValueTask<List<NameData>> GetNameListAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        return set?.GetNamesForGender(gender) ?? [];
    }

    public async ValueTask<(string? given, string? surname)> GetRandomFullNameAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities);
        if (set is null)
        {
            return (null, null);
        }
        return (set.GetRandomName(gender), set.GetRandomSurname());
    }

    public List<string[]> GetRandomEthnicities() => Ethnicity.GetRandomEthnicities(Ethnicities);

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

        var localData = await indexedDb
            .GetItemAsync<LocalData>(LocalData.IdValue);
        if (localData?.Data is not null)
        {
            LastLocalSync = localData.Data.LastSync;
            if (localData.Data.LastSync > Data.LastSync)
            {
                Data = localData.Data;
                UpdateData();
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
        var updated = false;
        if (value.UserDefined)
        {
            updated = value.Parent?.Types?.Remove(value) == true;
        }

        var top = value;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        if (Data.Ethnicities is not null
            && !top.HasUserDefined())
        {
            updated |= Data.Ethnicities.Remove(top);
        }

        if (value.UserDefined
            && value.Parent is null
            && Ethnicities.Remove(value))
        {
            var defaultList = await GetDefaultEthnicitiesAsync();
            var index = defaultList.IndexOf(value);
            if (index != -1)
            {
                Ethnicities.Insert(index, defaultList[index]);
            }
        }

        if (updated)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveGenreAsync(Genre value)
    {
        var updated = Data.Genres?.Remove(value) == true;

        if (value.UserDefined && Genres.Remove(value))
        {
            var defaultList = await GetDefaultGenresAsync();
            var index = defaultList.IndexOf(value);
            if (index != -1)
            {
                Genres.Insert(index, defaultList[index]);
            }
        }

        if (updated)
        {
            await SaveAsync();
        }
    }

    public async Task RemovePlotAsync(Plot value)
    {
        var updated = Data.Plots?.Remove(value) == true;

        if (value.UserDefined && Plots.Remove(value))
        {
            var defaultList = await GetDefaultPlotsAsync();
            var index = defaultList.IndexOf(value);
            if (index != -1)
            {
                Plots.Insert(index, defaultList[index]);
            }
        }

        if (updated)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveStoryTraitAsync(Trait value)
    {
        var updated = false;
        if (value.UserDefined)
        {
            updated = Data.StoryTraits?.Remove(value) == true;
        }

        var top = value;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        if (Data.StoryTraits is not null
            && !top.HasUserDefined())
        {
            updated |= Data.StoryTraits.Remove(top);
        }

        if (value.UserDefined
            && value.Parent is null
            && StoryTraits.Remove(value))
        {
            var defaultList = await GetDefaultStoryTraitsAsync();
            var index = defaultList.IndexOf(value);
            if (index != -1)
            {
                StoryTraits.Insert(index, defaultList[index]);
            }
        }

        if (updated)
        {
            await SaveAsync();
        }
    }

    public async Task RemoveTraitAsync(Trait value)
    {
        var updated = false;
        if (value.UserDefined)
        {
            updated = Data.Traits?.Remove(value) == true;
        }

        var top = value;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        if (Data.Traits is not null
            && !top.HasUserDefined())
        {
            updated |= Data.Traits.Remove(top);
        }

        if (value.UserDefined
            && value.Parent is null
            && Traits.Remove(value))
        {
            var defaultList = await GetDefaultTraitsAsync();
            var index = defaultList.IndexOf(value);
            if (index != -1)
            {
                Traits.Insert(index, defaultList[index]);
            }
        }

        if (updated)
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
        await indexedDb.StoreItemAsync(localData);
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
            var data = JsonSerializer.Deserialize<ScopData>(json);
            if (data is not null)
            {
                Data = data;
                await SaveAsync();
            }
        }
        catch { }
    }

    private static Trait GetNewTrait(Trait parent, string newValue)
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
            UserDefined = true,
        };

        (parent.Children ??= []).Add(newTrait);
        var top = parent;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }
        return top;
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
            var collection = Ethnicities;
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
            var collection = Ethnicities;
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
        Ethnicities = await GetDefaultEthnicitiesAsync();
        Genres = await GetDefaultGenresAsync();
        Plots = await GetDefaultPlotsAsync();
        StoryTraits = await GetDefaultStoryTraitsAsync();
        Traits = await GetDefaultTraitsAsync();
    }

    private void UpdateData()
    {
        if (Data.Ethnicities?.Count > 0)
        {
            foreach (var ethnicity in Data.Ethnicities)
            {
                var index = Ethnicities.IndexOf(ethnicity);
                if (index >= 0)
                {
                    Ethnicities.RemoveAt(index);
                    Ethnicities.Insert(index, ethnicity);
                }
                else
                {
                    Ethnicities.Add(ethnicity);
                }
            }
        }

        if (Data.Genres is not null)
        {
            foreach (var genre in Data.Genres)
            {
                var index = Genres.IndexOf(genre);
                if (index >= 0)
                {
                    Genres.RemoveAt(index);
                    Genres.Insert(index, genre);
                }
                else
                {
                    Genres.Add(genre);
                }
            }
        }

        if (Data.Plots is not null)
        {
            foreach (var plot in Data.Plots)
            {
                var index = Plots.IndexOf(plot);
                if (index >= 0)
                {
                    Plots.RemoveAt(index);
                    Plots.Insert(index, plot);
                }
                else
                {
                    Plots.Add(plot);
                }
            }
        }

        if (Data.StoryTraits?.Count > 0)
        {
            foreach (var trait in Data.StoryTraits)
            {
                var index = StoryTraits.IndexOf(trait);
                if (index >= 0)
                {
                    StoryTraits.RemoveAt(index);
                    StoryTraits.Insert(index, trait);
                }
                else
                {
                    StoryTraits.Add(trait);
                }
            }
        }

        if (Data.Traits?.Count > 0)
        {
            foreach (var trait in Data.Traits)
            {
                var index = Traits.IndexOf(trait);
                if (index >= 0)
                {
                    Traits.RemoveAt(index);
                    Traits.Insert(index, trait);
                }
                else
                {
                    Traits.Add(trait);
                }
            }
        }

        DataLoaded?.Invoke(this, EventArgs.Empty);
    }
}
