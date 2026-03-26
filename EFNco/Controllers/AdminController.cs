using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EFNco.Models;

namespace EFNco.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Department = user.Department,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            return View(userList);
        }

        // GET: /Admin/UserDetails/{id}
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;
            return View(user);
        }

        // GET: /Admin/EditUser/{id}
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CurrentRole = roles.FirstOrDefault() ?? "",
                AvailableRoles = allRoles!
            };

            return View(model);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Department = model.Department;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;

            await _userManager.UpdateAsync(user);

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.SelectedRole))
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

            TempData["Success"] = $"User {user.FullName} updated successfully.";
            return RedirectToAction("Users");
        }

        // GET: /Admin/DeleteUser/{id}
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Admin/DeleteUser
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"User {user.FullName} deleted.";
            return RedirectToAction("Users");
        }

        // GET: /Admin/ResetUserPassword/{id}
        public async Task<IActionResult> ResetUserPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.UserName = user.FullName;
            ViewBag.UserId = user.Id;
            return View();
        }

        // POST: /Admin/ResetUserPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Password reset for {user.FullName}.";
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.UserName = user.FullName;
            ViewBag.UserId = user.Id;
            return View();
        }
    }
}
