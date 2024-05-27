using MyBGList.Models;

namespace MyBGList.GraphQL
{
    public class Query
    {
        [Serial]
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<BoardGame> GetBoardGames([Service] AppDbContext context)
          => context.BoardGames;

        [Serial]
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Domain> GetDomains([Service] AppDbContext context)
          => context.Domains;

        [Serial]
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Mechanic> GetMechanics([Service] AppDbContext context)
          => context.Mechanics;
    }
}
