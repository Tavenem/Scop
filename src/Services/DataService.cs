using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tavenem.Blazor.IndexedDB;
using Tavenem.Randomize;

namespace Scop;

public class DataService : IDisposable
{
    private static string[] DefaultEthnicityHierarchy { get; } = new[] { "caucasian", "american" };

    private readonly HttpClient _httpClient;
    private readonly IndexedDbService _indexedDb;
    private readonly ScopJsInterop _jsInterop;

    private NameSet? _defaultNameSet;
    private bool _disposedValue;
    private DotNetObjectReference<DataService>? _dotNetObjectRef;
    private bool _loaded = false;
    private Ethnicity? _nameSetEthnicity;
    private NameSet? _nameSet;

    public List<Trait> Traits { get; set; } = new();

    public ScopData Data { get; set; } = new();

    public bool GDriveSync { get; set; }

    public string? GDriveUserName { get; set; }

    public DateTime LastGDriveSync { get; set; }

    public DateTime LastLocalSync { get; set; }

    public List<Ethnicity> Ethnicities { get; set; } = new();

    public event EventHandler? DataLoaded;

    public DataService(
        HttpClient httpClient,
        IndexedDbService indexedDb,
        ScopJsInterop jsInterop)
    {
        _httpClient = httpClient;
        _indexedDb = indexedDb;
        _jsInterop = jsInterop;
    }

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

    public void DeleteLocal()
    {
        _indexedDb.ClearAsync();
        LastLocalSync = DateTime.MinValue;
    }

    /// <summary>
    /// <para>
    /// Invoked when a Google Drive file is finished loading.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
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

    public async ValueTask<List<NameData>> GetNameListAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities)
            .ConfigureAwait(false);
        return set?.GetNamesForGender(gender) ?? new();
    }

    public async ValueTask<(string? given, string? surname)> GetRandomFullNameAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities)
            .ConfigureAwait(false);
        if (set is null)
        {
            return (null, null);
        }
        return (set.GetRandomName(gender), set.GetRandomSurname());
    }

    public List<string[]> GetRandomEthnicities() => Ethnicity.GetRandomEthnicities(Ethnicities);

    public async ValueTask<string?> GetRandomNameAsync(NameGender gender, List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities)
            .ConfigureAwait(false);
        return set?.GetRandomName(gender);
    }

    public async ValueTask<string?> GetRandomSurnameAsync(List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities)
            .ConfigureAwait(false);
        return set?.GetRandomSurname();
    }

    public async ValueTask<List<NameData>> GetSurnameListAsync(List<string[]>? ethnicities)
    {
        var set = await GetFilteredNameSetAsync(ethnicities)
            .ConfigureAwait(false);
        return set?.Surnames ?? new();
    }

    public async ValueTask LoadAsync()
    {
        if (_loaded)
        {
            return;
        }

        Ethnicities = await _httpClient
            .GetFromJsonAsync<List<Ethnicity>>("./ethnicities.json")
            .ConfigureAwait(false)
            ?? new();

        Traits = await _httpClient
            .GetFromJsonAsync<List<Trait>>("./traits.json")
            .ConfigureAwait(false)
            ?? new();

        var localData = await _indexedDb
            .GetItemAsync<LocalData>(LocalData.IdValue)
            .ConfigureAwait(false);
        if (localData?.Data is not null)
        {
            var data = JsonSerializer.Deserialize<ScopData>(localData.Data);
            if (data is not null)
            {
                LastLocalSync = data.LastSync;
                if (data.LastSync > Data.LastSync)
                {
                    Data = data;
                    UpdateData();
                }
            }
        }

        if (!GDriveSync)
        {
            GDriveSync = await _jsInterop.GetDriveSignedIn();
        }
        if (GDriveSync)
        {
            await LoadGDriveAsync()
                .ConfigureAwait(false);
        }

        _loaded = true;
    }

    public async Task SaveAsync()
    {
        Data.LastSync = DateTime.UtcNow;

        var serializedData = JsonSerializer.Serialize(Data);

        var localData = new LocalData { Data = serializedData };
        await _indexedDb
            .StoreItemAsync(localData)
            .ConfigureAwait(false);
        LastLocalSync = Data.LastSync;

        if (GDriveSync)
        {
            await SaveGDriveAsync(serializedData)
                .ConfigureAwait(false);
        }
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
                await SaveAsync().ConfigureAwait(false);
            }
        }
        catch { }
    }

    private async ValueTask AddNamesAsync(NameSet names, Ethnicity ethnicity)
    {
        var nameSet = await GetNameSetAsync(ethnicity)
            .ConfigureAwait(false);
        if (nameSet is not null)
        {
            if (nameSet.FemaleNames is not null)
            {
                (names.FemaleNames ??= new()).AddRange(nameSet.FemaleNames);
            }
            if (nameSet.MaleNames is not null)
            {
                (names.MaleNames ??= new()).AddRange(nameSet.MaleNames);
            }
            if (nameSet.Surnames is not null)
            {
                (names.Surnames ??= new()).AddRange(nameSet.Surnames);
            }
        }
        else if (ethnicity.Types?.Count > 0)
        {
            var child = Randomizer.Instance.Next(ethnicity.Types)!;
            await AddNamesAsync(names, child)
                .ConfigureAwait(false);
        }
    }

    private async Task<NameSet> GetFilteredNameSetAsync(List<string[]>? characterEthnicities)
    {
        var nameSet = new NameSet();
        characterEthnicities ??= new() { DefaultEthnicityHierarchy };

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

            await AddNamesAsync(nameSet, match)
                .ConfigureAwait(false);
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
                await AddNamesAsync(nameSet, match)
                    .ConfigureAwait(false);
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
                return await GetNameSetAsync(ethnicity.Parent)
                    .ConfigureAwait(false);
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

        var nameSet = await _httpClient
            .GetFromJsonAsync<NameSet>(url)
            .ConfigureAwait(false);

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
        if (_jsInterop is null)
        {
            return;
        }

        _dotNetObjectRef ??= DotNetObjectReference.Create(this);

        await _jsInterop
            .LoadDriveData(_dotNetObjectRef)
            .ConfigureAwait(false);
    }

    private async Task SaveGDriveAsync(string data)
    {
        if (_jsInterop is null)
        {
            return;
        }

        await _jsInterop
            .SaveDriveData(data)
            .ConfigureAwait(false);

        LastGDriveSync = Data.LastSync;
    }

    private void UpdateData()
    {
        if (Data.Ethnicities?.Count > 0)
        {
            foreach (var ethnicity in Data.Ethnicities)
            {
                if (Ethnicities.Contains(ethnicity))
                {
                    Ethnicities.Remove(ethnicity);
                }
                Ethnicities.Add(ethnicity);
            }
        }

        if (Data.Traits?.Count > 0)
        {
            foreach (var trait in Data.Traits)
            {
                var index = Traits.FindIndex(x => x == trait);
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
