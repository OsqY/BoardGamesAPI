using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MyBGList.Attributes;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{

    [Route("[controller]")]
    [ApiController]
    public class DomainController : ControllerBase
    {
        private readonly ILogger<DomainController> _logger;
        private readonly AppDbContext _context;

        public DomainController(ILogger<DomainController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        [ManualValidationFilter]
        public async Task<ActionResult<RestDTO<Domain[]>>> Get([FromQuery] RequestDTO<DomainDTO> input)
        {
            if (!ModelState.IsValid)
            {
                var details = new ValidationProblemDetails(ModelState);
                details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id
                  ?? HttpContext.TraceIdentifier;
                if (ModelState.Keys.Any(k => k == "PageSize"))
                {
                    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2";
                    details.Status = StatusCodes.Status501NotImplemented;
                    return new ObjectResult(details)
                    {
                        StatusCode = StatusCodes.Status501NotImplemented
                    };
                }
                else
                {
                    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    details.Status = StatusCodes.Status400BadRequest;
                    return new BadRequestObjectResult(details);
                }
            }
            var query = _context.Domains.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(d => d.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();
            query = query.OrderBy($"{input.SortColumn} {input.SortOrder}")
              .Skip(input.PageIndex * input.PageSize).Take(input.PageSize);

            return new RestDTO<Domain[]>()
            {
                Data = await query.ToArrayAsync(),
                Links = new List<LinkDTO>() {
                     new LinkDTO(Url.Action(null, "Domains", new {input.PageIndex, input.PageSize}, Request.Scheme)!,
                         "self", "GET")
                   }
            };
        }

        [HttpPost(Name = "UpdateDomain")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Domain?>> Post(DomainDTO model)
        {
            Domain? domain = await _context.Domains.Where(d => d.Id == model.Id).FirstOrDefaultAsync();

            if (domain != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    domain.Name = model.Name;
                _context.Domains.Update(domain);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Domain?>()
            {
                Data = domain,
                Links = new List<LinkDTO>() {
                     new LinkDTO(Url.Action(null, "Domains", model.Id, Request.Scheme)!,
                         "self", "POST"
                         )
                   }
            };
        }

        [HttpDelete(Name = "DeleteDomain")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<Domain?>> Delete(int id)
        {
            Domain? domain = await _context.Domains.Where(d => d.Id == id).FirstOrDefaultAsync();

            if (domain != null)
            {
                _context.Domains.Remove(domain);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Domain?>()
            {
                Data = domain,
                Links = new List<LinkDTO>() {
                   new LinkDTO(Url.Action(null, "Domains", id, Request.Scheme)!,
                       "self", "DELETE")
                 }
            };
        }
    }
}
