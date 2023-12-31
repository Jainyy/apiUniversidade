using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using apiUniversidade.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace apiUniversidade.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class AutorizaController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
       
       

        public AutorizaController(UserManager<IdentityUser> userManager,
                 SignInManager<IdentityUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<string> Get()
        {
            return "AutorizaController :: Acessado em: " + DateTime.Now.ToLongDateString();
        }
        [HttpPost("register")]
        public async Task<ActionResult> RegisterUser([FromBody] UsuarioDTO model)
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _signInManager.SignInAsync(user, false);
            //return Ok(GerarToken(model));
            return Ok();


        }

        [HttpPost("login")]
            public async Task<ActionResult> Login([FromBody]UsuarioDTO userInfo){
                var result = await _signInManager.PasswordSignInAsync(userInfo.Email, userInfo.Password,
                        isPersistent: false, lockoutOnFailure: false);

                if(result.Succeeded)
                    return Ok();
                else{
                    ModelState.AddModelError(string.Empty, "Login Inválido...");
                    return BadRequest(ModelState);
                }
            }
        private UsuarioToken GeraToken(UsuarioDTO userInfo){
            var claims = new[]{
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName, userInfo.Email),
                new Claim("IFRN", "TecInfo"),
                new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            //GERAR CHAVE ATRAVÉS DE UM ALGORITMO DE CHAVE SIMÉTRICA
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:key"]));
            
            //GERAR A ASSINATURA DIGITAL DO TOKEN UTILIZANDO
            // A CHAVE PRIVADA (KEY) E O ALGORITMO HMAC SHA 256
            var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            //TEMPO DE EXPIRAÇÃO DO TOKEN
            var expiracao = _configuration["TokenConfiguration:ExpireHours"];
            var expiration = DateTime.UtcNow.AddHours(double.Parse(expiracao));

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _configuration["TokenConfiguration:Issuer"],
                audience: _configuration["TokenConfiguration:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new UsuarioToken(){
                Authenticated = true,
                Expiration = expiration,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Message = "JWT Ok."
            };

        }
    }
}