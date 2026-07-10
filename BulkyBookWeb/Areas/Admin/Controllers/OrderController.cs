using BulkyBook.Business.Services;
using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using BulkyBookWeb.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]

    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        [BindProperty]
        public OrderHeader OrderHeader { get; set; }

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int orderId)
        {
            OrderHeader = await _orderService.GetOrderByIdAsync(orderId, includeDetails: true, includeUser: true);
            return View(OrderHeader);
        }
        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderDetails()
        {
            var orderHeaderFromDb = await _orderService.GetOrderByIdAsync(OrderHeader.Id, includeDetails: false, includeUser: false);

            orderHeaderFromDb.Name = OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderHeader.City;
            orderHeaderFromDb.State = OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderHeader.Carrier) && orderHeaderFromDb.OrderStatus == SD.StatusShipped)
            {
                orderHeaderFromDb.Carrier = OrderHeader.Carrier;
            }
            if(!string.IsNullOrEmpty(OrderHeader.TrackingNumber) && orderHeaderFromDb.OrderStatus == SD.StatusShipped)
            {
                orderHeaderFromDb.TrackingNumber = OrderHeader.TrackingNumber;
            }

            await _orderService.UpdatOrderAsync(orderHeaderFromDb);
            TempData["success"] = "Order details updated successfully!";
            return RedirectToAction("Details", new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderStatus(string status)
        {
            var orderHeader = await _orderService.GetOrderByIdAsync(OrderHeader.Id);

            if (orderHeader == null)
            {
                TempData["error"] = "Order not found!";
                return RedirectToAction(nameof(Index));
            }

            string successMessage;

            switch (status)
            {
                case SD.StatusProcessing:
                    await _orderService.UpdateOrderStatusAsync(orderHeader.Id, status);
                    successMessage = "Order processing started successfully.";
                break;

                case SD.StatusShipped:

                    if(string.IsNullOrEmpty(OrderHeader.Carrier) || string.IsNullOrEmpty(OrderHeader.TrackingNumber))
                    {
                        TempData["error"] = "Carrier and Tracking Number are required to mark the order as shipped.";
                        return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
                    }

                    await _orderService.UpdateOrderStatusAsync(
                        OrderHeader.Id, SD.StatusShipped, OrderHeader.Carrier, OrderHeader.TrackingNumber);

                    successMessage = "Order shipped successfully.";
                break;

                case SD.StatusCancelled:
                    await _orderService.UpdateOrderStatusAsync(orderHeader.Id, status);
                    successMessage = "Order cancelled successfully.";
                break;

                case SD.StatusRefunded:
                    await _orderService.UpdateOrderStatusAsync(orderHeader.Id, status);
                    successMessage = "Order refunded successfully.";
                break;

                default:
                    TempData["error"] = "Invalid order status.";
                    return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
            }

            TempData["success"] = successMessage;
            return RedirectToAction("Details", new { orderId = OrderHeader.Id });
        }

        #region API CALLS
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(string status)
        {
            string? userId = null;

            if(!User.IsInRole(SD.RoleAdmin) && !User.IsInRole(SD.RoleEmployee))
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }
            }

            var orders = await _orderService.GetAllOrderAsync(userId, status);
            return Json(new { data = orders });
        }
        #endregion
    }
}