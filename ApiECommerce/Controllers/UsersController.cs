﻿using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _config;

        public UsersController(AppDbContext appDbContext, IConfiguration config)
        {
            _appDbContext = appDbContext;
            _config = config;
        }


        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var checkUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkUser != null)
            {
                return BadRequest("Já existe um utilizador com este email.");
            }

            _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var currentUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email && u.Password == user.Password);

            if (currentUser == null)
            {
                return NotFound("O utilizador não existe");
            }

            //var key = _config["JWT:Key"] ?? throw new ArgumentNullException("JWT:Key", "JWT:Key cannot be null.");
            //var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email!)
            };

            var token = new JwtSecurityToken(
                issuer: _config["JWT:Issuer"],
                audience: _config["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(5),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new ObjectResult(new
            {
                accessToken = jwt,
                expiration = token.ValidTo,
                tokenType = "bearer",
                userId = currentUser.Id,
                userName = currentUser.Name,
                email = currentUser.Email,
                phone = currentUser.Phone
            });
        }

        [Authorize]
        [HttpPost("uploadimage")]
        public async Task<IActionResult> UploadUserPhoto(IFormFile image)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(U => U.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado");
            }

            if (image != null)
            {
                string uniqueFileName = $"{Guid.NewGuid().ToString()}_{image.FileName}";

                string filePath = Path.Combine("wwwroot/userimages", uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                user.UrlImage = $"/userimages/{uniqueFileName}";

                await _appDbContext.SaveChangesAsync();
                return Ok("Imagem enviada com sucesso");
            }

            return BadRequest("Nenhuma imagem enviada");
        }


        [Authorize]
        [HttpGet("userimage")]
        public async Task<IActionResult> GetUserImage()
        {
            //see if user is logged
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            //locate user
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado");
            }

            var userImage = await _appDbContext.Users
                .Where(x => x.Email == userEmail)
                .Select(x => new
                {
                    x.UrlImage,
                })
                .SingleOrDefaultAsync();

            return Ok(userImage);
        }
    }
}
