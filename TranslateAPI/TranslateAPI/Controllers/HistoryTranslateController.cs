using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TranslateAPI.Domains;
using TranslateAPI.Services;

namespace TranslateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryTranslateController : ControllerBase
    {
        private readonly IMongoCollection<HistoryTranslate> _historyTranslate;

        public HistoryTranslateController(MongoDbService mongoDbService)
        {
            _historyTranslate = mongoDbService.GetDatabase.GetCollection<HistoryTranslate>("historyTranslate");
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<HistoryTranslate>>> Get()
        {
            try
            {
                var translaties = await _historyTranslate.Find(FilterDefinition<HistoryTranslate>.Empty).ToListAsync();
                return Ok(translaties);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<HistoryTranslate>> Create([FromBody] HistoryTranslate newHistoryTranslate)
        {
            try
            {
                await _historyTranslate.InsertOneAsync(newHistoryTranslate);

                return StatusCode(201, newHistoryTranslate);

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpGet("idUser")]
        public async Task<ActionResult<List<HistoryTranslate>>> GetByIdUser(string id)
        {
            try
            {
                var userHistoryTranslate = await _historyTranslate.Find(u => u.idUser == id).ToListAsync();
                if (userHistoryTranslate == null)
                {
                    return NotFound();
                }
                return Ok(userHistoryTranslate);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpDelete("id")]
        public async Task<ActionResult<HistoryTranslate>> Delete(string id)
        {
            try
            {
                var historyTranslate = await _historyTranslate.FindOneAndDeleteAsync(u => u.Id == id);

                if (historyTranslate == null)
                {
                    return NotFound();
                }

                return StatusCode(201);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

    }
}
