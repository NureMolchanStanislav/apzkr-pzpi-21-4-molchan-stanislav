using System.Linq.Expressions;
using Domain.Common;
using MongoDB.Bson;
using MongoDB.Driver;
using Persistence.Database;
using Application.IRepositories.Base;

namespace Persistence.Repositories.Base;

public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : EntityBase
{
    protected MongoDbContext _db;

    protected IMongoCollection<TEntity> _collection;

    public BaseRepository(MongoDbContext db, string collectionName)
    {
        this._db = db;
        this._collection = _db.Db.GetCollection<TEntity>(collectionName);
    }

    public async Task<TEntity> GetOneAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await this._collection.Find(x => x.Id == id && x.IsDeleted == false).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity> GetOneAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await this._collection.Find(Builders<TEntity>.Filter.Where(predicate) & Builders<TEntity>.Filter.Where(x => !x.IsDeleted))
                                     .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await this._collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
        return entity;
    }
    
    public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await _collection.Find(Builders<TEntity>.Filter.Where(predicate) &
                                      Builders<TEntity>.Filter.Where(x => !x.IsDeleted)).ToListAsync(cancellationToken);
    }
    
    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _collection.Find(Builders<TEntity>.Filter.Where(x => !x.IsDeleted)).ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await this._collection.Find(Builders<TEntity>.Filter.Where(x => !x.IsDeleted))
                                     .Skip((pageNumber - 1) * pageSize)
                                     .Limit(pageSize)
                                     .ToListAsync(cancellationToken);
    }
    
    public async Task<List<TEntity>> GetPageAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await _collection
            .Find(Builders<TEntity>.Filter.Empty)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetPageAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await this._collection.Find(Builders<TEntity>.Filter.Where(predicate) & Builders<TEntity>.Filter.Where(x => !x.IsDeleted))
                                     .Skip((pageNumber - 1) * pageSize)
                                     .Limit(pageSize)
                                     .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync()
    {
        var filter = Builders<TEntity>.Filter.Eq("IsDeleted", false);
        return (int)(await this._collection.CountDocumentsAsync(x => !x.IsDeleted));
    }

    public async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return (int)(await this._collection.CountDocumentsAsync(Builders<TEntity>.Filter.Where(predicate) & Builders<TEntity>.Filter.Where(x => !x.IsDeleted), cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await this._collection.Find(Builders<TEntity>.Filter.Where(predicate) & Builders<TEntity>.Filter.Where(x => !x.IsDeleted)).AnyAsync(cancellationToken);
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var updateDefinition = Builders<TEntity>.Update
        .Set(e => e.IsDeleted, true)
        .Set(e => e.LastModifiedById, entity.LastModifiedById)
        .Set(e => e.LastModifiedDateUtc, entity.LastModifiedDateUtc);

        var options = new FindOneAndUpdateOptions<TEntity>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await this._collection.FindOneAndUpdateAsync(
            Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id), updateDefinition, options, cancellationToken);
    }
}