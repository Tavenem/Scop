using Scop.Models;
using Tavenem.Blazor.IndexedDB;

namespace Scop.Services;

#pragma warning disable CS0618 // Type or member is obsolete; migration code
public class DataMigration(
    [FromKeyedServices("scop_v1")] IndexedDb v1,
    [FromKeyedServices("scop")] IndexedDb vCurrent)
{
    private readonly IndexedDb v2 = vCurrent;

    public Task UpgradeAsync() => UpgradeV1V2Async();

    private async Task<bool> UpgradeV1V2Async()
    {
        var v1Store = v1[DataService.ObjectStoreName];
        if (v1Store is null)
        {
            return true;
        }

        var v1LocalData = await v1Store.GetItemAsync<LocalData>(LocalData.IdValue);
        if (v1LocalData?.Data is null)
        {
            return true;
        }

        var v2Store = v2[DataService.ObjectStoreName];
        if (v2Store is null)
        {
            return false;
        }

        var v2LocalData = await v2Store.GetItemAsync<LocalData>(LocalData.IdValue);
        if (v2LocalData is not null)
        {
            return true;
        }

        if (!UpgradeV1V2(v1LocalData.Data))
        {
            return false;
        }

        v2LocalData = new() { Data = v1LocalData.Data };
        if (await v2Store.StoreItemAsync(v2LocalData))
        {
            await v1.DeleteDatabaseAsync();
            return true;
        }

        return false;
    }

    public static bool Upgrade(ScopData data)
        => data.Version >= 2 || UpgradeV1V2(data);

    private static bool UpgradeV1V2(ScopData data)
    {
        if (data.Version > 1)
        {
            return true;
        }

        data.Stories.ForEach(story =>
        {
            foreach (var character in story.AllCharacters())
            {
                if (character.HyphenatedSurname
                    || character.Names?.Count > 0
                    || character.Surnames?.Count > 0
                    || !string.IsNullOrEmpty(character.Suffix)
                    || !string.IsNullOrEmpty(character.Title))
                {
                    character.CharacterName = new()
                    {
                        GivenNames = character.Names?.Count > 0
                            ? [character.Names.First()]
                            : null,
                        HasHyphenatedSurname = character.HyphenatedSurname,
                        MiddleNames = character.Names?.Count > 1
                            ? [.. character.Names.Skip(1)]
                            : null,
                        Suffixes = [character.Suffix],
                        Surnames = character.Surnames?.ConvertAll(x => new Surname(x)),
                        Title = character.Title,
                    };
                }

                if (character.Relationships is null)
                {
                    continue;
                }

                foreach (var relationship in character.Relationships)
                {
                    if (!string.IsNullOrEmpty(relationship.Id))
                    {
                        if (string.IsNullOrEmpty(relationship.RelativeId))
                        {
                            relationship.RelativeId = relationship.Id;
                        }
                        relationship.Id = null;
                    }

                    if (!string.IsNullOrEmpty(relationship.RelativeName))
                    {
                        if (string.IsNullOrEmpty(relationship.RelativeId))
                        {
                            relationship.RelativeId = relationship.RelativeName;
                        }
                        relationship.RelativeName = null;
                    }

                    if (!string.IsNullOrEmpty(relationship.RelationshipName))
                    {
                        if (string.IsNullOrEmpty(relationship.Type))
                        {
                            relationship.Type = relationship.RelationshipName;
                        }
                        relationship.RelationshipName = null;
                    }
                }
            }
        });

        data.Version = 2;

        return true;
    }
}
