using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moviequest_proj.Data;
using Moviequest_proj.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moviequest_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GenresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            var genres = await _context.Genres.ToListAsync();
            return Ok(genres);
        }
    }
}
