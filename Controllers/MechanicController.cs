using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Extensions;
using MyBGList.Models;

namespace MyBGList.Controllers
{
    [Authorize(Roles = RoleNames.Moderator)]
    [Route("[controller]")]
    [ApiController]
    public class MechanicController : ControllerBase
    {
        private readonly ILogger<MechanicController> _logger;
        private readonly AppDbContext _context;
        readonly IDistributedCache _distributedCache;

        public MechanicController(
            ILogger<MechanicController> logger,
            AppDbContext context,
            IDistributedCache distributedCache
        )
        {
            _logger = logger;
            _context = context;
            _distributedCache = distributedCache;
        }

        [HttpGet]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<Mechanic[]>> Get([FromQuery] RequestDTO<MechanicDTO> input)
        {
            var query = _context.Mechanics.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(m => m.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();
            Mechanic[]? result = null;
            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

            if (!_distributedCache.TryGetValue<Mechanic[]>(cacheKey, out result))
            {
                query = query
                    .Skip(input.PageIndex * input.PageSize)
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _distributedCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }
            return new RestDTO<Mechanic[]>()
            {
                Data = result,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "Mechanics",
                            new { input.PageIndex, input.PageSize },
                            Request.Scheme
                        )!,
                        "self",
                        "GET"
                    )
                }
            };
        }

        [HttpPost("UpdateMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            Mechanic? mechanic = await _context
                .Mechanics.Where(m => m.Id == model.Id)
                .FirstOrDefaultAsync();
            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    mechanic.Name = model.Name;
                mechanic.LastModifiedDate = DateTime.Now;
                _context.Update(mechanic);
                await _context.SaveChangesAsync();
            }
            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(null, "Mechanics", model.Id, Request.Scheme)!,
                        "self",
                        "POST"
                    )
                }
            };
        }

        [Authorize(Roles = RoleNames.Administrator)]
        [HttpDelete("DeleteMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Delete(int id)
        {
            Mechanic? mechanic = await _context
                .Mechanics.Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            if (mechanic != null)
            {
                _context.Remove(mechanic);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>()
                {
                    new LinkDTO(
                        Url.Action(null, "Mechanics", id, Request.Scheme)!,
                        "self",
                        "DELETE"
                    )
                }
            };
        }
    }
}
