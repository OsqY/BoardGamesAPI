using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;

namespace MyBGList.GraphQL
{
    public class Mutation
    {
        [Serial]
        [Authorize(Roles = new[] { RoleNames.Moderator })]
        public async Task<BoardGame?> UpdateBoardGame([Service] AppDbContext context, BoardGameDTO model)
        {
            BoardGame? boardGame = await context.BoardGames.Where(b => b.Id == model.Id).FirstOrDefaultAsync();

            if (boardGame != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    boardGame.Name = model.Name;
                if (model.Year.HasValue && model.Year > 0)
                    boardGame.Year = model.Year.Value;

                boardGame.LastModifiedDate = DateTime.Now;
                context.BoardGames.Update(boardGame);
                await context.SaveChangesAsync();
            }

            return boardGame;
        }

        [Serial]
        [Authorize(Roles = new[] { RoleNames.Administrator })]
        public async Task DeleteBoardGame([Service] AppDbContext context, int id)
        {
            BoardGame? boardGame = await context.BoardGames.Where(b => b.Id == id).FirstOrDefaultAsync();

            if (boardGame != null)
            {
                context.BoardGames.Remove(boardGame);
                await context.SaveChangesAsync();
            }

        }

        [Serial]
        [Authorize(Roles = new[] { RoleNames.Moderator })]
        public async Task<Domain?> UpdateDomain([Service] AppDbContext context, DomainDTO model)
        {
            Domain? domain = await context.Domains.Where(d => d.Id == model.Id).FirstOrDefaultAsync();

            if (domain != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    domain.Name = model.Name;

                domain.LastModifiedDate = DateTime.Now;
                context.Domains.Update(domain);
                await context.SaveChangesAsync();
            }
            return domain;
        }

        [Serial]
        [Authorize(Roles = new[] { RoleNames.Administrator })]
        public async Task DeleteDomain([Service] AppDbContext context, int id)
        {
            Domain? domain = await context.Domains.Where(d => d.Id == id).FirstOrDefaultAsync();

            if (domain != null)
            {
                context.Domains.Remove(domain);
                await context.SaveChangesAsync();
            }

        }

        [Serial]
        [Authorize(Roles = new[] { RoleNames.Moderator })]
        public async Task<Mechanic?> UpdateMechanic([Service] AppDbContext context, MechanicDTO model)
        {
            Mechanic? mechanic = await context.Mechanics.Where(m => m.Id == model.Id).FirstOrDefaultAsync();

            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    mechanic.Name = model.Name;

                mechanic.LastModifiedDate = DateTime.Now;
                context.Mechanics.Update(mechanic);
                await context.SaveChangesAsync();
            }

            return mechanic;
        }

        [Serial]
        [Authorize(Roles = new[] { RoleNames.Administrator })]
        public async Task DeleteMechanic([Service] AppDbContext context, int id)
        {
            Mechanic? mechanic = await context.Mechanics.Where(m => m.Id == id).FirstOrDefaultAsync();

            if (mechanic != null)
            {
                context.Mechanics.Remove(mechanic);
                await context.SaveChangesAsync();
            }

        }

    }
}
