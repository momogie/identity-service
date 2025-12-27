using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Data.Common;

namespace Shared;

public static class EFExtensions
{
    public static List<TEntity> RemoveRange<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        var c = dbSet.Where(predicate);
        dbSet.RemoveRange(c);
        return [.. c];
    }

    public static int Remove<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        return dbSet.Where(predicate).ExecuteDelete();
    }

    public static DbTransaction GetEfDbTransaction(this IDbContextTransaction source)
    {
        return (source as IInfrastructure<DbTransaction>).Instance;
    }
}
