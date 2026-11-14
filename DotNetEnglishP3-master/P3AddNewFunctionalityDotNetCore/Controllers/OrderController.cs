using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using P3AddNewFunctionalityDotNetCore.Models;
using P3AddNewFunctionalityDotNetCore.Models.Exceptions;
using P3AddNewFunctionalityDotNetCore.Models.Services;
using P3AddNewFunctionalityDotNetCore.Models.ViewModels;

namespace P3AddNewFunctionalityDotNetCore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ICart _cart;
        private readonly IOrderService _orderService;
        private readonly IStringLocalizer<OrderController> _localizer;

        public OrderController(
            ICart cart,
            IOrderService service,
            IStringLocalizer<OrderController> localizer)
        {
            _cart = cart;
            _orderService = service;
            _localizer = localizer;
        }

        [HttpGet]
        public ViewResult Index()
        {
            return View(new OrderViewModel());
        }

        [HttpPost]
        public IActionResult Index(OrderViewModel order)
        {
            var cart = (Cart)_cart;

            if (!cart.Lines.Any())
            {
                ModelState.AddModelError("", _localizer["CartEmpty"]);
            }

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            order.Lines = cart.Lines.ToArray();

            try
            {
                _orderService.SaveOrder(order);
            }
            catch (InsufficientStockException)
            {
                ModelState.AddModelError(
                    "",
                    _localizer["InsufficientStock"]);

                return View(order);
            }

            return RedirectToAction(nameof(Completed));
        }

        public ViewResult Completed()
        {
            _cart.Clear();
            return View();
        }
    }
}