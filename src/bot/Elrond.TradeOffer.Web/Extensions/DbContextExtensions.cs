using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Extensions;

public static class DbContextExtensions
{
    public static void AddOrUpdate(this DbContext ctx, object entity)
    {
        var entry = ctx.Entry(entity);
        switch (entry.State)
        {
            case EntityState.Detached:
                ctx.Add(entity);
                break;
            case EntityState.Modified:
                ctx.Update(entity);
                break;
            case EntityState.Added:
                ctx.Add(entity);
                break;
            case EntityState.Unchanged:
                //item already in db no need to do anything  
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}