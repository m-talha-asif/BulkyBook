using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using BulkyBookWeb.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _db.OrderHeaders.ToListAsync();
            var productCount = await _db.Products.CountAsync();
            var userCount = await _db.ApplicationUsers.CountAsync();

            DashboardVM dashboardVM = new DashboardVM()
            {
                TotalOrders = orders.Count,
                TotalProducts = productCount,
                TotalUsers = userCount,
                TotalRevenue = orders.Where(o => o.OrderStatus == SD.StatusApproved || o.OrderStatus == SD.StatusShipped).Sum(o => o.OrderTotal)
            };

            return View(dashboardVM);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            var orders = await _db.OrderHeaders.ToListAsync();
            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            var categories = await _db.Categories.ToListAsync();

            var now = DateTime.UtcNow;
            var sixMonthAgo = now.AddMonths(-5);
            var monthlyRevenue = Enumerable.Range(0, 6).Select(i =>
            {
                var month = sixMonthAgo.AddMonths(i);
                var revenue = orders.Where(o => o.OrderDate.Year == month.Year
                && o.OrderDate.Month == month.Month
                && (o.OrderStatus == SD.StatusApproved || o.OrderStatus == SD.StatusShipped))
                .Sum(o => o.OrderTotal);

                return new { Label = month.ToString("MMM yyyy"), Revenue = revenue };
            }).ToList();

            var monthlyOrders = Enumerable.Range(0, 6).Select(i =>
            {
                var month = sixMonthAgo.AddMonths(i);
                var count = orders.Count(o => o.OrderDate.Year == month.Year
                && o.OrderDate.Month == month.Month);

                return new { Label = month.ToString("MMM yyyy"), Count = count };
            }).ToList();

            var statusBreakdown = orders
                .GroupBy(o => o.OrderStatus ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var productsPerCategory = categories.Select(c => new
            {
                Category = c.Name,
                Count = products.Count(p => p.CategoryId == c.Id)
            }).ToList();

            return Json(new
            {
                monthlyRevenue,
                monthlyOrders,
                statusBreakdown,
                productsPerCategory
            });
        }
    }

}
