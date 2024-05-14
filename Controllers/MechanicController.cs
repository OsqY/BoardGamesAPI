using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MechanicController : ControllerBase
    {
        private readonly ILogger<MechanicController> _logger;
        private readonly AppDbContext _context;

        public MechanicController(ILogger<MechanicController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<RestDTO<Mechanic[]>> Get([FromQuery] RequestDTO<MechanicDTO> input)
        {
            var query = _context.Mechanics.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(m => m.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();
            query = query.OrderBy($"{input.SortColumn} {input.SortOrder}")
              .Skip(input.PageIndex * input.PageSize).Take(input.PageSize);
            return new RestDTO<Mechanic[]>()
            {
                Data = await query.ToArrayAsync(),
                Links = new List<LinkDTO>() {
                     new LinkDTO(
                         Url.Action(null, "Mechanics", new {input.PageIndex, input.PageSize}, Request.Scheme)!
                         , "self", "GET")
                   }

            };
        }

        [HttpPost("UpdateMechanic")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            Mechanic? mechanic = await _context.Mechanics.Where(m => m.Id == model.Id).FirstOrDefaultAsync();
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
                Links = new List<LinkDTO>() {
                         new LinkDTO(Url.Action(null, "Mechanics", model.Id,Request.Scheme )!,
                             "self","POST")
                       }
            };
        }

        [HttpDelete("DeleteMechanic")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Mechanic?>> Delete(int id)
        {
            Mechanic? mechanic = await _context.Mechanics.Where(m => m.Id == id).FirstOrDefaultAsync();

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
                new LinkDTO(Url.Action(null, "Mechanics", id, Request.Scheme)!,
                    "self","DELETE")
              }
            };
        }
    }
}
