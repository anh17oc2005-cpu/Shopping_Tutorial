using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shopping_Tutorial.Models;
using Shopping_Tutorial.Repository;
using System.Security.Claims;

namespace Shopping_Tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/User")]
    //[Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;



        public UserController(DataContext context,
            UserManager<AppUserModel> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {

            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = context;

        }

        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                        into UserRoles
                                        from ur in UserRoles.DefaultIfEmpty()
                                            // Include users without roles
                                        join r in _dataContext.Roles on u.RoleId equals r.Id into userRolesWithRoles
                                        from urr in userRolesWithRoles.DefaultIfEmpty()
                                        select new UserWithRoleName { User = u, RoleName = urr.Name })
                                         .ToListAsync();

            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the logged-in user's ID
            ViewBag.loggedInUserId = loggedInUserId;

            return View(usersWithRoles);
        }

        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AppUserModel());
        }
        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id, AppUserModel user)
        {
            var existingUser = await _userManager.FindByIdAsync(id);//lấy user dựa vao id
            if (existingUser == null)
            {

                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update other user properties (excluding password)
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.RoleId = Request.Form["RoleId"];

                var updateUserResult = await _userManager.UpdateAsync(existingUser); //thực hiện update

                if (updateUserResult.Succeeded)
                {
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    AddIdentityErrors(updateUserResult);
                    return View(existingUser);
                }
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            // Model validation failed
            TempData["error"] = "Model validation failed.";
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            string errorMessage = string.Join("\n", errors);

            return View(existingUser);
        }
        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AppUserModel user)
        {
            if (ModelState.IsValid)
            {
                var createUserResult = await _userManager.CreateAsync(user, user.PasswordHash); //tạo user
                if (createUserResult.Succeeded)
                {
                    var createUser = await _userManager.FindByEmailAsync(user.Email); //tìm user dựa vào email
                    var userId = createUser.Id; // lấy user Id
                    var role = _roleManager.FindByIdAsync(user.RoleId); //lấy RoleId
                                                                        //gán quyền
                    var addToRoleResult = await _userManager.AddToRoleAsync(createUser, role.Result.Name);
                    if (!addToRoleResult.Succeeded)
                    {
                        foreach (var error in createUserResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }

                    return RedirectToAction("Index", "User");
                }
                else
                {

                    foreach (var error in createUserResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(user);
                }

            }
            else
            {
                TempData["error"] = "Model có một vài thứ đang lỗi";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);

        }

        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }
            TempData["success"] = "User đã được xóa thành công";
            return RedirectToAction("Index");
        }
        //Add more warranty to products
        [Route("AddWarranty")]
        [HttpGet]
        public async Task<IActionResult> AddWarranty(string id)
        {
            // Validate user existence
            var userExists = await _dataContext.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return NotFound($"User with ID {id} not found.");
            }

            // Retrieve products
            var products = await _dataContext.Products.ToListAsync();
            // Fetch warranty products for the given UserId

            var warrantyProducts = await _dataContext.ProductWarranties
                .Where(wp => wp.UserId == id)
                .Include(wp => wp.Product) // Include the related product info
                .ToListAsync();

            if (warrantyProducts == null || !warrantyProducts.Any())
            {
                TempData["Message"] = "No warranty products found for this user.";
            }

            if (warrantyProducts == null || !warrantyProducts.Any())
            {
                TempData["Message"] = "No warranty products found for this user.";
            }
            // Compare DateCreated and DateExpired for each warranty product
            foreach (var warranty in warrantyProducts)
            {
                if (warranty.DateExpired < warranty.DateCreated)
                {
                    warranty.Status = 0; // Set status to expired if DateExpired is before DateCreated
                                         // You can update the database if needed
                    _dataContext.ProductWarranties.Update(warranty);
                }
                else
                {
                    warranty.Status = 1; // Set status to active if DateExpired is after DateCreated
                                         // You can update the database if needed
                    _dataContext.ProductWarranties.Update(warranty);
                }
            }
            // Pass data to the view
            ViewBag.ProductWarranty = warrantyProducts;
            ViewBag.Products = products;
            ViewBag.UserId = id;

            return View();
        }
        [Route("UpdateStatus/{id}")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            // Find the warranty by both Id and UserId
            var warranty = await _dataContext.ProductWarranties
                .FirstOrDefaultAsync(w => w.Id == id);

            // Check if the warranty exists and belongs to the logged-in user
            if (warranty == null)
            {
                TempData["Message"] = "Warranty not found or you don't have permission to modify it.";
                return RedirectToAction("Warranty");
            }

            // Toggle the status: if it's 1 (Active), set it to 0 (Inactive), and vice versa.
            warranty.Status = warranty.Status == 1 ? 0 : 1;

            // Save the changes to the database
            await _dataContext.SaveChangesAsync();

            // Redirect back to the warranty list page with a success message
            TempData["Message"] = "Warranty status updated successfully.";
            return RedirectToAction("AddWarranty", new { id = warranty.UserId });
        }

        [Route("StoreProductWarranty")]
        [HttpPost]
        public async Task<IActionResult> StoreProductWarranty(ProductWarrantyModel model)
        {
            // Validate the input model
            if (!ModelState.IsValid)
            {
                // If validation fails, re-fetch products to populate the dropdown and return to the form
                var products = await _dataContext.Products.ToListAsync();
                ViewBag.Products = products;
                ViewBag.Id = model.UserId; // Keep the user ID for re-rendering
                return View("AddWarranty", model);
            }

            // Check if the selected product exists
            var productExists = await _dataContext.Products.AnyAsync(p => p.Id == model.ProductId);
            if (!productExists)
            {
                ModelState.AddModelError("ProductId", "The selected product does not exist.");
                var products = await _dataContext.Products.ToListAsync();
                ViewBag.Products = products;
                ViewBag.Id = model.UserId; // Keep the user ID for re-rendering
                return View("AddWarranty", model);
            }

            // Create a new ProductWarranty record
            var productWarranty = new ProductWarrantyModel
            {
                UserId = model.UserId,
                ProductId = model.ProductId,
                Description = model.Description,
                DateStart = model.DateStart,
                DateExpired = model.DateExpired,
                Status = model.Status,
                DateCreated = DateTime.Now
            };

            // Add the record to the database
            _dataContext.ProductWarranties.Add(productWarranty);
            await _dataContext.SaveChangesAsync();


            return RedirectToAction("AddWarranty", new { id = model.UserId });

        }
        [Route("DeleteWarranty/{id}")]
        [HttpPost]
        public async Task<IActionResult> DeleteWarranty(int id)
        {
            // Find the warranty product by ID
            var warranty = await _dataContext.ProductWarranties
                                              .Include(wp => wp.Product) // Optionally, include related product data
                                              .FirstOrDefaultAsync(wp => wp.Id == id);

            if (warranty == null)
            {
                // If the warranty doesn't exist, return NotFound
                return NotFound($"Warranty with ID {id} not found.");
            }

            // Remove the warranty product from the database
            _dataContext.ProductWarranties.Remove(warranty);
            await _dataContext.SaveChangesAsync();

            // Optionally, use TempData to show a success message
            TempData["Message"] = "Warranty deleted successfully.";

            // Redirect back to the previous page or to the list of warranty products
            return RedirectToAction("AddWarranty", new { id = warranty.UserId });
        }




    }
}
