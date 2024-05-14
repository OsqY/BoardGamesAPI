using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using MyBGList.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MyBGListController : ControllerBase
    {
        private readonly ILogger<MyBGListController> _logger;
        private readonly AppDbContext _context;

        public MyBGListController(ILogger<MyBGListController> logger, AppDbContext appDb)
        {
            _logger = logger;
            _context = appDb;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<RestDTO<BoardGame[]>> Get([FromQuery] RequestDTO<BoardGameDTO> input)
        {
            var query = _context.BoardGames.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(b => b.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();
            query = query
              .OrderBy($"{input.SortColumn} {input.SortOrder}")
              .Skip(input.PageIndex * input.PageSize).Take(input.PageSize);

            return new RestDTO<BoardGame[]>()
            {
                Data = await query.ToArrayAsync(),
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO>() {
                 new LinkDTO(Url.Action(null, "BoardGames",
                       new{input.PageIndex, input.PageSize}, Request.Scheme)!,
                     "self", "GET"),
               }
            };
        }

        [HttpPost(Name = "UpdateBoardGame")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
        {
            BoardGame? boardGame = await _context.BoardGames.Where(b => b.Id == model.Id).FirstOrDefaultAsync();
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
                Links = new List<LinkDTO>() {
                         new LinkDTO (
                           Url.Action(null, "BoardGames",
                               model,Request.Scheme)!,
                           "self", "POST")
                       }
            };
        }

        [HttpDelete(Name = "DeleteBoardGame")]
        [ResponseCache(NoStore = true)]
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
                Links = new List<LinkDTO>() {
                     new LinkDTO(
                         Url.Action(null, "BoardGames", id, Request.Scheme)!,
                         "self","DELETE"
                         )
                   }
            };
        }
    }
}
