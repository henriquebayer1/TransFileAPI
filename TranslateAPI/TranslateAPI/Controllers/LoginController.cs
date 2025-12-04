using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using TranslateAPI.Domains;
using TranslateAPI.Services;
using TranslateAPI.ViewModels;

namespace TranslateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly HttpClient _httpClient;

        public LoginController(IHttpClientFactory httpClientFactory, MongoDbService mongoDbService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _users = mongoDbService.GetDatabase.GetCollection<User>("user");
        }

        [HttpPost]
        public async Task<ActionResult<User>> Login(Login user)
        {
            try
            {
                //busca usuário por email e senha 
                User userSearch = await _users.Find(p => p.Email == user.Email).FirstOrDefaultAsync();

                //caso não encontre
                if (userSearch == null || !Criptografia.HashComparer(user.Password!, userSearch.Password!))
                {
                    //retorna 401 - sem autorização
                    return StatusCode(401, "Email ou senha inválidos!");
                }


                //caso encontre, prossegue para a criação do token

                //informações que serão fornecidas no token
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Email, userSearch.Email!),
                    new Claim(JwtRegisteredClaimNames.Name,userSearch.Name!),
                    new Claim(JwtRegisteredClaimNames.Jti, userSearch.Id!),
                };

                //chave de segurança
                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("transfile-webapi-chave-symmetricsecuritykey"));

                //credenciais
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                //token
                var myToken = new JwtSecurityToken(
                        issuer: "TransFile-WebAPI",
                        audience: "TransFile-WebAPI",
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(30),
                        signingCredentials: creds
                    );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(myToken)
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(string acessToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={acessToken}");

                if (!response.IsSuccessStatusCode)
                    return BadRequest("Failed to fetch user info from Google.");

                var userInfo = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Buscar ou criar o usuário
                var user = await _users.Find(u => u.GoogleId == userInfo.GetProperty("sub").GetString()).FirstOrDefaultAsync();
                if (user == null)
                {
                    user = new User
                    {
                        GoogleId = userInfo.GetProperty("sub").GetString(),
                        Email = userInfo.GetProperty("email").GetString(),
                        Name = userInfo.GetProperty("name").GetString(),
                        Photo = userInfo.GetProperty("picture").GetString()
                    };
                    await _users.InsertOneAsync(user);
                }

                // Geração do token JWT diretamente no método
                var claims = new[]
                {
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Name, user.Name!),
                new Claim(JwtRegisteredClaimNames.Jti, user.Id!),
            };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("transfile-webapi-chave-symmetricsecuritykey"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "TransFile-WebAPI",
                    audience: "TransFile-WebAPI",
                    claims: claims,
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: creds
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenString = tokenHandler.WriteToken(token);


                // Retorna o token JWT
                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
