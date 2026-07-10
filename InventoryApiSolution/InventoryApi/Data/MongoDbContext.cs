using InventoryApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InventoryApi.Data;

public class MongoDbContext
{
    public IMongoCollection<InventoryItemBase> InventoryItems { get; }
    public IMongoCollection<ActivityLog> ActivityLogs { get; }

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);

        InventoryItems = database.GetCollection<InventoryItemBase>(settings.Value.InventoryCollectionName);
        ActivityLogs = database.GetCollection<ActivityLog>("ActivityLogs");
    }
}
