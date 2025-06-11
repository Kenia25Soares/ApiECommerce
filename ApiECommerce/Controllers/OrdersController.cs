using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiECommerce.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public OrdersController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] Order order)
        {
            order.OrderDate = DateTime.Now;

            var shoppingCartItems = await _appDbContext.ShoppingCartItems
                .Where(cart => cart.ClientId == order.UserId)
                .ToListAsync();

            if (shoppingCartItems.Count == 0)
            {
                return NotFound("Não existem items no carrinho para criar o pedido.");
            }

            using (var transaction = await _appDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    _appDbContext.Orders.Add(order);
                    await _appDbContext.SaveChangesAsync();

                    foreach (var item in shoppingCartItems)
                    {
                        var orderDetail = new OrderDetail()
                        {
                            Price = item.UnitPrice,
                            Total = item.Total,
                            Quantity = item.Quantity,
                            ProductId = item.ProductId,
                            OrderId = order.Id,
                        };
                        _appDbContext.OrderDetails.Add(orderDetail);
                    }

                    await _appDbContext.SaveChangesAsync();
                    _appDbContext.ShoppingCartItems.RemoveRange(shoppingCartItems);
                    await _appDbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new { OrderId = order.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Ocorreu um erro ao processar o pedido."); 
                }
            }  
        }


        // GET: api/Pedidos/PedidosPorUser/5
        // Obtêm todos os pedidos de um user específico com base no UserId.
        [HttpGet("[action]/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            /*var orders = await (from pedido in dbContext.Pedidos
                                 where pedido.UsuarioId == usuarioId
                                 orderby pedido.DataPedido descending
                                 select new
                                 {
                                     Id = pedido.Id,
                                     PedidoTotal = pedido.ValorTotal,
                                     DataPedido = pedido.DataPedido,
                                 }).ToListAsync();*/

            var orders = await _appDbContext.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    Id = o.Id,
                    Total = o.Total,

                    OrderDate = o.OrderDate,
                })
                .ToListAsync();

            if (orders == null || orders.Count == 0)
            {
                return NotFound("Não foram encontrados pedidos para o utilizador especificado.");
            }

            return Ok(orders);
        }
    }
}
