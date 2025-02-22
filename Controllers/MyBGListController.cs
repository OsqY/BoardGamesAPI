using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace MyBGList.Controllers
{
    [Authorize(Roles = RoleNames.Moderator)]
    [Route("[controller]")]
    [ApiController]
    public class MyBGListController : ControllerBase
    {
        private readonly ILogger<MyBGListController> _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public MyBGListController(
            ILogger<MyBGListController> logger,
            AppDbContext appDb,
            IMemoryCache memoryCache
        )
        {
            _logger = logger;
            _context = appDb;
            _memoryCache = memoryCache;
        }

        [AllowAnonymous]
        [HttpGet]
        [ResponseCache(CacheProfileName = "Any-60")]
        [SwaggerOperation(
            Summary = "Get a list of board games.",
            Description = "Retrieves a list of board games with custom paging, sorting and filtering rules."
        )]
        public async Task<RestDTO<BoardGame[]>> Get(
            [FromQuery]
            [SwaggerParameter(
                "A DTO object that can be used to customize data retrieval parameters."
            )]
                RequestDTO<BoardGameDTO> input
        )
        {
            _logger.LogInformation(CustomLogEvents.MyBGListController_Get, "Method started");

            (int recordCount, BoardGame[]? result) dataTuple = (0, null);
            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

            if (!_memoryCache.TryGetValue(cacheKey, out dataTuple))
            {
                System.Console.WriteLine("nothing in cache");

                var query = _context.BoardGames.AsQueryable();
                if (!string.IsNullOrEmpty(input.FilterQuery))
                    query = query.Where(b => b.Name.Contains(input.FilterQuery));

                dataTuple.recordCount = await query.CountAsync();

                query = query
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Skip(input.PageIndex * input.PageSize)
                    .Take(input.PageSize);

                dataTuple.result = await query.ToArrayAsync();
                _memoryCache.Set(cacheKey, dataTuple, new TimeSpan(0, 0, 30));
            }

            return new RestDTO<BoardGame[]>()
            {
                Data = dataTuple.result,
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = dataTuple.recordCount,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "BoardGames",
                            new { input.PageIndex, input.PageSize },
                            Request.Scheme
                        )!,
                        "self",
                        "GET"
                    ),
                }
            };
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ResponseCache(CacheProfileName = "Any-60")]
        [SwaggerOperation(
            Summary = "Get a single board game.",
            Description = "Retrieves a single board game by the given ID."
        )]
        public async Task<RestDTO<BoardGame?>> GetBoardGame(int id)
        {
            _logger.LogInformation(
                CustomLogEvents.MyBGListController_Get,
                "GET BoardGame method started"
            );

            BoardGame? result = null;

            var cacheKey = $"GetBoardGame-{id}";
            if (!_memoryCache.TryGetValue<BoardGame>(cacheKey, out result))
            {
                result = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
                _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }

            return new RestDTO<BoardGame?>()
            {
                Data = result,
                PageIndex = 0,
                PageSize = 1,
                RecordCount = result != null ? 1 : 0,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(null, "BoardGames", new { id }, Request.Scheme)!,
                        "self",
                        "GET"
                    )
                }
            };
        }

        [HttpPost(Name = "UpdateBoardGame")]
        [ResponseCache(CacheProfileName = "NoCache")]
        [SwaggerOperation(
            Summary = "Updates a single board game.",
            Description = "Updates the board game's data."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Authorized")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
        public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
        {
            BoardGame? boardGame = await _context
                .BoardGames.Where(b => b.Id == model.Id)
                .FirstOrDefaultAsync();
            if (boardGame != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    boardGame.Name = model.Name;
                if (model.Year.HasValue && model.Year.Value > 0)
                    boardGame.Year = model.Year.Value;

                boardGame.LastModifiedDate = DateTime.Now;
                _context.BoardGames.Update(boardGame);
                await _context.SaveChangesAsync();
            }
            return new RestDTO<BoardGame?>()
            {
                Data = boardGame,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(null, "BoardGames", model, Request.Scheme)!,
                        "self",
                        "POST"
                    )
                }
            };
        }

        [Authorize(Roles = RoleNames.Administrator)]
        [HttpDelete(Name = "DeleteBoardGame")]
        [ResponseCache(CacheProfileName = "NoCache")]
        [SwaggerOperation(
            Summary = "Deletes a single board game.",
            Description = "Deletes a board game from the database."
        )]
        public async Task<RestDTO<BoardGame?>> Delete(int id)
        {
            var boardGame = await _context.BoardGames.Where(b => b.Id == id).FirstOrDefaultAsync();

            if (boardGame != null)
            {
                _context.BoardGames.Remove(boardGame);
                await _context.SaveChangesAsync();
            }
            return new RestDTO<BoardGame?>()
            {
                Data = boardGame,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(null, "BoardGames", id, Request.Scheme)!,
                        "self",
                        "DELETE"
                    )
                }
            };
        }
    }
}
