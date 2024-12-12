using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cafeteria.Data;
using Cafeteria.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cafeteria.Controllers
{
    public class OrdersController : Controller
    {
        private readonly CafeteriaContext _context;

        // Dicionário temporário para rastrear produtos no pedido atual
        private static Dictionary<int, int> _currentOrder = new Dictionary<int, int>();

        // Lista para armazenar o histórico de pedidos
        private static List<OrderHistoryViewModel> _orderHistory = new List<OrderHistoryViewModel>();

        public OrdersController(CafeteriaContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var products = await _context.Product.ToListAsync();

            // Calcula o total do pedido
            ViewBag.TotalPedido = _currentOrder.Sum(item => products.First(p => p.Id == item.Key).Price * item.Value);

            // Passa tanto os produtos quanto o pedido atual para a View
            ViewBag.CurrentOrder = _currentOrder;
            return View(products);
        }

        // POST: Orders/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int productId, string action)
        {
            var product = await _context.Product.FindAsync(productId);
            if (product == null) return NotFound();

            // Lógica para adicionar ou remover produto do pedido atual
            if (action == "add")
            {
                if (product.Quantity > 0)
                {
                    // Adiciona o produto ao pedido atual e reduz o estoque
                    _currentOrder[productId] = _currentOrder.GetValueOrDefault(productId, 0) + 1;
                    product.Quantity -= 1;
                }
                else
                {
                    ViewBag.ErrorMessage = "Estoque insuficiente para adicionar mais deste produto.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (action == "remove")
            {
                if (_currentOrder.ContainsKey(productId) && _currentOrder[productId] > 0)
                {
                    // Remove o produto do pedido atual e devolve ao estoque
                    _currentOrder[productId] -= 1;
                    product.Quantity += 1;

                    // Remove o produto do dicionário se a quantidade no pedido atual for zero
                    if (_currentOrder[productId] == 0)
                    {
                        _currentOrder.Remove(productId);
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "Nenhuma quantidade deste produto no pedido atual para remover.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Salva as alterações no banco de dados
            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Orders/CloseOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseOrder()
        {
            if (_currentOrder.Count == 0)
            {
                ViewBag.ErrorMessage = "Não há itens no pedido para finalizar.";
                return RedirectToAction(nameof(Index)); // Volta para a página do pedido, caso o carrinho esteja vazio
            }

            var products = await _context.Product.ToListAsync();

            // Calculando o total do pedido
            var totalAmount = _currentOrder.Sum(item =>
                products.First(p => p.Id == item.Key).Price * item.Value);

            // Criando o histórico do pedido
            var orderHistory = new OrderHistoryViewModel
            {
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Items = _currentOrder.Select(item => new OrderItemViewModel
                {
                    ProductName = products.First(p => p.Id == item.Key).Name,
                    Quantity = item.Value,
                    Price = products.First(p => p.Id == item.Key).Price
                }).ToList()
            };

            // Adicionando o pedido ao histórico de pedidos
            _orderHistory.Add(orderHistory);

            // Limpando o pedido atual
            _currentOrder.Clear();

            // Retorna para a tela de histórico
            return RedirectToAction(nameof(OrderHistory));
        }
        // POST: Orders/ClearOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearOrder()
        {
            _currentOrder.Clear(); // Limpa o pedido atual
            return RedirectToAction(nameof(Index)); // Retorna à página de pedidos
        }

        // GET: Orders/OrderHistory
        public IActionResult OrderHistory()
        {
            // Certifique-se de que o histórico de pedidos não seja nulo
            ViewBag.OrderHistory = _orderHistory ?? new List<OrderHistoryViewModel>(); // Se for nulo, passa uma lista vazia
            return View("History");
        }
    }

    // ViewModel para o histórico de pedidos
    public class OrderHistoryViewModel
    {
        public DateTime OrderDate { get; set; } // Data do pedido
        public decimal TotalAmount { get; set; } // Total do pedido
        public List<OrderItemViewModel> Items { get; set; } // Itens do pedido
    }

    // ViewModel para os itens do pedido
    public class OrderItemViewModel
    {
        public int ProductId { get; set; } // ID do produto
        public string ProductName { get; set; } // Nome do produto
        public int Quantity { get; set; } // Quantidade
        public decimal Price { get; set; } // Preço
    }
}
