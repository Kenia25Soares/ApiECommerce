using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartItemsController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public ShoppingCartItemsController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ShoppingCartItem shoppingCartItem)
        {
            try
            {
                var shoppingCart = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
                    s.ProductId == shoppingCartItem.ProductId &&
                    s.ClientId == shoppingCartItem.ClientId);

                if (shoppingCart != null)
                {
                    shoppingCart.Quantity += shoppingCartItem.Quantity;
                    shoppingCart.Total = shoppingCart.UnitPrice * shoppingCart.Quantity;
                }
                else
                {
                    var product = await _appDbContext.Products.FindAsync(shoppingCartItem.ProductId);

                    var cart = new ShoppingCartItem()
                    {
                        ClientId = shoppingCartItem.ClientId,
                        ProductId = shoppingCartItem.ProductId,
                        UnitPrice = shoppingCartItem.UnitPrice,
                        Quantity = shoppingCartItem.Quantity,
                        Total = (product!.Price) * (shoppingCartItem.Quantity)
                    };

                    _appDbContext.ShoppingCartItems.Add(cart);
                }

                await _appDbContext.SaveChangesAsync();
                return StatusCode(StatusCodes.Status201Created); // Created response
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao processar a solicitação");
            }
        }


        [Authorize]
        [HttpPut("[action]{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncreaseQuantity(int productId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var shoppingCartItem = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
            s.ClientId == user.Id && s.ProductId == productId);

            if (shoppingCartItem != null)
            {
                shoppingCartItem.Quantity += 1;
                shoppingCartItem.Total = shoppingCartItem.UnitPrice * shoppingCartItem.Quantity;
                _appDbContext.Update(shoppingCartItem);
                await _appDbContext.SaveChangesAsync();
                return Ok("Foi aumentada a quantidade com sucesso");
            }
            else
            {
                return NotFound("Nenhum item encontrado no carrinho");
            }
        }


        [Authorize]
        [HttpPut("[action]{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DecreaseQuantity(int productId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var shoppingCartItem = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
            s.ClientId == user.Id && s.ProductId == productId);

            if (shoppingCartItem != null)
            {
                if (shoppingCartItem.Quantity > 1)
                {
                    shoppingCartItem.Quantity -= 1;
                }
                else
                {
                    _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
                    await _appDbContext.SaveChangesAsync();
                    return Ok("Item removido com sucesso");
                }

                shoppingCartItem.Total = shoppingCartItem.UnitPrice * shoppingCartItem.Quantity;
                _appDbContext.Update(shoppingCartItem);
                await _appDbContext.SaveChangesAsync();
                return Ok("Foi diminuida a quantidade com sucesso");
            }
            else
            {
                return NotFound("Nenhum item encontrado no carrinho");
            }
        }


        [Authorize]
        [HttpDelete("{productId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int productId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            var shoppingCartItem = await _appDbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
            s.ClientId == user.Id && s.ProductId == productId);

            if (shoppingCartItem != null)
            {
                _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
                await _appDbContext.SaveChangesAsync(); 
                return Ok("Item removido com sucesso");
            }
            else
            {
                return NotFound("Nenhum item encontrado no carrinho");
            }
        }
    }
}
