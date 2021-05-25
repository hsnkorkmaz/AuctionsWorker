using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AuctionsWorker
{
    public class MongoCRUD
    {

        private IMongoDatabase mDb;
        public MongoCRUD(string database, string connectionString = null)
        {
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            //settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            ////keep the connection alive for 3 minutes
            //settings.MaxConnectionIdleTime = new TimeSpan(0, 3, 0);
            //var client = new MongoClient(settings);
            var client = new MongoClient();
            mDb = client.GetDatabase(database);
        }

        public async Task InsertRecordAsync<T>(string table, T record)
        {
            try
            {
                var collection = mDb.GetCollection<T>(table);
                await collection.InsertOneAsync(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        public async Task InsertManyAsync<T>(string table, List<T> record)
        {
            try
            {
                var collection = mDb.GetCollection<T>(table);
                await collection.InsertManyAsync(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        public async Task<List<T>> LoadRecordsAsync<T>(string table)
        {
            var collection = mDb.GetCollection<T>(table);
            return await collection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task<T> LoadRecordByIdAsync<T>(string table, int id)
        {
            var collection = mDb.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await collection.Find(filter).FirstOrDefaultAsync();
        }


        public async Task<long> CountByIdAsync<T>(string table, int id)
        {
            var collection = mDb.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await collection.Find(filter).CountDocumentsAsync();
        }

        public async Task<T> LoadRecordByFilterAsync<T>(string table, FilterDefinition<T> filter)
        {
            var collection = mDb.GetCollection<T>(table);
            //var filter = Builders<T>.Filter.Eq("Id", 164);
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpsertRecord<T>(string table, int id, T record)
        {
            var collection = mDb.GetCollection<T>(table);
            await collection.ReplaceOneAsync(new BsonDocument("_id", id), record, new ReplaceOptions { IsUpsert = true });
        }

        public async Task DeleteRecord<T>(string table, int id)
        {
            var collection = mDb.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            await collection.DeleteOneAsync(filter);
        }

        public async Task DropCollection(string table)
        {
            await mDb.DropCollectionAsync(table);
        }
    }
}
