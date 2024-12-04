using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cafeteria.Data;
using Cafeteria.Models;

namespace Cafeteria.Controllers
{
    public class OrdersController : Controller
    {
        private readonly CafeteriaContext _context;

        public OrdersController(CafeteriaContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var products = await _context.Product.ToListAsync();
            return View(products);
        }

        // POST: Orders/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int productId, int quantity)
        {
            // Obtenha o produto do banco de dados
            var product = await _context.Product.FindAsync(productId);
            
            if (product == null)
            {
                return NotFound();
            }

            // Verifique se há estoque suficiente
            if (product.Quantity < quantity)
            {
                // Adicione uma mensagem de erro ao ViewBag
                ViewBag.ErrorMessage = "Estoque insuficiente.";
                return RedirectToAction(nameof(Index)); // Redireciona para a página de pedidos
            }

            // Subtraia a quantidade do estoque
            product.Quantity -= quantity;

            // Salve as alterações no banco de dados
            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index)); // Redireciona para a página de pedidos
        }
    }
}