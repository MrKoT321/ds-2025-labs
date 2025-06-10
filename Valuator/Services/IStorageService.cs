namespace Valuator.Services;

public interface IStorageService
{
    public void SaveShardKey(string id, string shardKey);
    public void SaveById(string key, string value, string id);
    public void SaveByShardKey(string key, string value, string shardKey);
    public bool SaveContainsById(string key, string value, string id);
    public string? GetById(string id, string key);
    public string? GetValue(string id, string shardKey);
    public string? GetUserIdByTextId(string id);
}