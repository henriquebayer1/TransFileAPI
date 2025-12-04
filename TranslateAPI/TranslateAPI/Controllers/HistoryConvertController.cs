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
    public class HistoryConvertController : ControllerBase
    {
        private readonly IMongoCollection<HistoryConvert> _historyConvert;

        public HistoryConvertController(MongoDbService mongoDbService)
        {
            _historyConvert = mongoDbService.GetDatabase.GetCollection<HistoryConvert>("historyConvert");
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<HistoryConvert>>> Get()
        {
            try
            {
                var converts = await _historyConvert.Find(FilterDefinition<HistoryConvert>.Empty).ToListAsync();
                return Ok(converts);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<HistoryConvert>> Create([FromBody] HistoryConvert newHistoryConvert)
        {
            try
            {
                await _historyConvert.InsertOneAsync(newHistoryConvert);

                return StatusCode(201, newHistoryConvert);

            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpGet("idUser")]
        public async Task<ActionResult<List<HistoryConvert>>> GetByIdUser(string id)
        {
            try
            {
                var userHistoryConvert = await _historyConvert.Find(u => u.idUser == id).ToListAsync();
                if (userHistoryConvert == null)
                {
                    return NotFound();
                }
                return Ok(userHistoryConvert);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpDelete("id")]
        public async Task<ActionResult<HistoryConvert>> Delete(string id)
        {
            try
            {
                var historyConvert = await _historyConvert.FindOneAndDeleteAsync(u => u.Id == id);

                if (historyConvert == null)
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
