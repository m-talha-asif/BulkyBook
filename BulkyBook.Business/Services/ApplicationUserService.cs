using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBookWeb.DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.Business.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly ApplicationDbContext _context;
        public ApplicationUserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            // Execute a single SQL query with LEFT JOINS on the database server
            var userRoleData = await (from user in _context.ApplicationUsers
                                      join userRole in _context.UserRoles on user.Id equals userRole.UserId into userRoleGroup
                                      from ur in userRoleGroup.DefaultIfEmpty() // Translates to LEFT JOIN
                                      join role in _context.Roles on ur.RoleId equals role.Id into roleGroup
                                      from r in roleGroup.DefaultIfEmpty()      // Translates to LEFT JOIN
                                      select new
                                      {
                                          User = user,
                                          RoleName = r.Name
                                      }).ToListAsync();

            // Extract the users and assign the role string to your [NotMapped] property
            var users = new List<ApplicationUser>();
            foreach (var item in userRoleData)
            {
                item.User.Role = item.RoleName;
                users.Add(item.User);
            }

            return users;
        }
    }
}
