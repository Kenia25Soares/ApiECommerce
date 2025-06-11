using ApiECommerce.Context;
using ApiECommerce.Entities;
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
    }
}
