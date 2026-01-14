using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Shopping_Tutorial.Areas.Admin.Repository;
using Shopping_Tutorial.Models;
using Shopping_Tutorial.Models.ViewModels;
using Shopping_Tutorial.Repository;
using System.Security.Claims;

namespace Shopping_Tutorial.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUserModel> _userManage;
        private SignInManager<AppUserModel> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        public AccountController(IEmailSender emailSender,
            UserManager<AppUserModel> userManage,
            SignInManager<AppUserModel> signInManager,
            DataContext context)
        {
            _dataContext = context;
            _userManage = userManage;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }
        public IActionResult Login(string returnUrl)
        {

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
        public async Task<IActionResult> UpdateAccount()
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                // User is not logged in, redirect to login
                return RedirectToAction("Login", "Account"); // Replace "Account" with your controller name
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            //get user by user email

            var user = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateInfoAccount(AppUserModel user)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userEmail = User.FindFirstValue(ClaimTypes.Email);
            //get user by user email

            var userById = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userById == null)
            {
                return NotFound();
            }
            else
            {

                // Hash the new password
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(userById, user.PasswordHash);
                userById.PasswordHash = passwordHash;
                // -- Hash the new password
                _dataContext.Update(userById);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Update Account Information Successfully";

            }
            return RedirectToAction("UpdateAccount", "Account");
        }
        public async Task<IActionResult> NewPass(AppUserModel user, string token)
        {
            var checkuser = await _userManage.Users
                .Where(u => u.Email == user.Email)
                .Where(u => u.Token == user.Token).FirstOrDefaultAsync();

            if (checkuser != null)
            {
                ViewBag.Email = checkuser.Email;
                ViewBag.Token = token;
            }
            else
            {
                TempData["error"] = "Email not found or token is not right";
                return RedirectToAction("ForgetPass", "Account");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateNewPassword(AppUserModel user, string token)
        {
            var checkuser = await _userManage.Users
                .Where(u => u.Email == user.Email)
                .Where(u => u.Token == user.Token).FirstOrDefaultAsync();

            if (checkuser != null)
            {
                //update user with new password and token
                string newtoken = Guid.NewGuid().ToString();
                // Hash the new password
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(checkuser, user.PasswordHash);

                checkuser.PasswordHash = passwordHash;
                // -- Hash the new password
                checkuser.Token = newtoken;

                await _userManage.UpdateAsync(checkuser);
                TempData["success"] = "Password updated successfully.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                TempData["error"] = "Email not found or token is not right";
                return RedirectToAction("ForgetPass", "Account");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendMailForgotPass(AppUserModel user)
        {
            var checkMail = await _userManage.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (checkMail == null)
            { //ko có email trả về
                TempData["error"] = "Email not found";
                return RedirectToAction("ForgetPass", "Account");
            }
            else //có
            {
                string token = Guid.NewGuid().ToString(); //242682e8 - c293 - 4121 - a05b - e441a9c65d48
                                                          //update token to user
                checkMail.Token = token;
                _dataContext.Update(checkMail);
                await _dataContext.SaveChangesAsync();
                //--update token to user
                var receiver = checkMail.Email;
                var subject = "Change password for user " + checkMail.Email; //Change password for user nguyenducan1526@gmail.com
                var message = "Click on link to change password " +
                    "<a href='" + $"{Request.Scheme}://{Request.Host}/Account/NewPass?email=" + checkMail.Email + "&token=" + token + "'>";

                //await _emailSender.SendEmailAsync(receiver, subject, message);
            }


            TempData["success"] = "An email has been sent to your registered email address with password reset instructions.";
            return RedirectToAction("ForgetPass", "Account");
        }
        public async Task<IActionResult> ForgetPass(string returnUrl)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid)
            {
                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginVM.Username, loginVM.Password, false, false);
                if (result.Succeeded)
                {
                    TempData["success"] = "Đăng nhập thành công";
                    var receiver = "demologin979@gmail.com";
                    var subject = "Đăng nhập trên thiết bị thành công.";
                    var message = "Đăng nhập thành công, trải nghiệm dịch vụ nhé.";

                    //await _emailSender.SendEmailAsync(receiver, subject, message);
                    return Redirect(loginVM.ReturnUrl ?? "/");
                }
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
            }
            return View(loginVM);
        }

        public IActionResult Create()
        {
            return View();
        }
        public async Task<IActionResult> History()
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                // User is not logged in, redirect to login
                return RedirectToAction("Login", "Account"); // Replace "Account" with your controller name
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var Orders = await _dataContext.Orders
               .Where(od => od.UserName == userEmail).OrderByDescending(od => od.Id).ToListAsync();

            ViewBag.UserEmail = userEmail;
            return View(Orders);
        }
        public async Task<IActionResult> Warranty()
        {
            // Check if the user is authenticated
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                // If not authenticated, redirect to login page
                return RedirectToAction("Login", "Account"); // Replace "Account" with your controller name
            }

            // Get the user ID and email from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Fetch warranty products for the given UserId

            var warrantyProducts = await _dataContext.ProductWarranties
                .Where(wp => wp.UserId == userId)
                .Include(wp => wp.Product) // Include the related product info
                .ToListAsync();

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

            // Pass the user's email and warranties to the View
            ViewBag.UserEmail = userEmail;
            return View();
        }

        public async Task<IActionResult> CancelOrder(string ordercode)
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                // User is not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = await _dataContext.Orders.Where(o => o.OrderCode == ordercode).FirstAsync();
                order.Status = 3;
                _dataContext.Update(order);
                await _dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                return BadRequest("An error occurred while canceling the order.");
            }


            return RedirectToAction("History", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (ModelState.IsValid)
            {
                AppUserModel newUser = new AppUserModel { UserName = user.Username, Email = user.Email };
                IdentityResult result = await _userManage.CreateAsync(newUser, user.Password);
                if (result.Succeeded)
                {
                    // Assign the "Admin" role to the new user
                    var roleAssignResult = await _userManage.AddToRoleAsync(newUser, "Admin");

                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, add errors to the ModelState
                        foreach (IdentityError error in roleAssignResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(user);
                    }

                    TempData["success"] = "Tạo khách hàng thành công và đã gán quyền Admin.";

                    return Redirect("/account/login");
                }
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(user);
        }

        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }
        public async Task LoginByGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse")
                });
        }
        public async Task<IActionResult> GoogleResponse()
        {
            // Authenticate using Google scheme
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                //Nếu xác thực ko thành công quay về trang Login
                return RedirectToAction("Login");
            }
            var claims = result.Principal.Identities.FirstOrDefault().Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });
            //return Json(claims);
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            //var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            string UserName = email.Split('@')[0]; //nguyenducan1526[0] @ gmail.com[1]
                                                   // Check user có tồn tại không
            var existingUser = await _userManage.FindByEmailAsync(email);
            if (existingUser == null)
            {
                // If user doesn't exist in the DB, create a new user with a hashed default password
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var hashedPassword = passwordHasher.HashPassword(null, "Ta!123");

                // Declare and create a new user
                var newUser = new AppUserModel { UserName = UserName, Email = email };
                newUser.PasswordHash = hashedPassword;

                // Create the user
                var createUserResult = await _userManage.CreateAsync(newUser);

                if (!createUserResult.Succeeded)
                {
                    TempData["error"] = "Đăng ký tài khoản thất bại. Vui lòng thử lại sau.";
                    return RedirectToAction("Login", "Account"); // Return to the registration page if fail
                }
                else
                {
                    // Assign the "Admin" role to the new user
                    var roleExist = await _userManage.IsInRoleAsync(newUser, "Admin");
                    if (!roleExist)
                    {
                        var roleAssignResult = await _userManage.AddToRoleAsync(newUser, "Admin");

                        if (!roleAssignResult.Succeeded)
                        {
                            TempData["error"] = "Không thể gán quyền Admin cho người dùng. Vui lòng thử lại sau.";
                            return RedirectToAction("Login", "Account");
                        }
                    }

                    // If user creation is successful, sign in the user
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    TempData["success"] = "Đăng ký tài khoản thành công.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // If user already exists, sign in the existing user
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
            }
            return RedirectToAction("Login", "Account");
        }


    }
}
