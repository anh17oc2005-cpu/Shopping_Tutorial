using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shopping_Tutorial.Areas.Admin.Repository;
using Shopping_Tutorial.Models;
using Shopping_Tutorial.Models.Order;
using Shopping_Tutorial.Repository;
using Shopping_Tutorial.Services.Momo;
using Shopping_Tutorial.Services.Vnpay;
using System.Security.Claims;

namespace Shopping_Tutorial.Controllers
{
    public class CheckoutController : Controller
    {

        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        private static readonly HttpClient client = new HttpClient();
        private IMomoService _momoService;
        private readonly IVnPayService _vnPayService;

        public CheckoutController(IVnPayService vnPayService, IEmailSender emailSender, DataContext context, IMomoService momoService)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _momoService = momoService;
            _vnPayService = vnPayService;

        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                var ordercode = Guid.NewGuid().ToString();
                var orderItem = new OrderModel();
                orderItem.OrderCode = ordercode;
                // Nhận shipping giá từ cookie
                var shippingPriceCookie = Request.Cookies["ShippingPrice"];
                decimal shippingPrice = 0;
                //Nhận Coupon code từ cookie
                var coupon_code = Request.Cookies["CouponTitle"];

                if (shippingPriceCookie != null)
                {
                    var shippingPriceJson = shippingPriceCookie;
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
                }
                else
                {
                    shippingPrice = 0;
                }
                orderItem.ShippingCost = shippingPrice;
                orderItem.CouponCode = coupon_code;
                orderItem.UserName = userEmail;
                orderItem.PaymentMethod = PaymentMethod + " " + PaymentId;


                orderItem.Status = 1;
                orderItem.CreatedDate = DateTime.Now;
                _dataContext.Add(orderItem);
                _dataContext.SaveChanges();
                //tạo order detail
                List<CartItemModel> cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
                foreach (var cart in cartItems)
                {
                    var orderdetail = new OrderDetail();
                    orderdetail.UserName = userEmail;
                    orderdetail.OrderCode = ordercode;
                    orderdetail.ProductId = cart.ProductId;
                    orderdetail.Price = cart.Price;
                    orderdetail.Quantity = cart.Quantity;
                    //update product quantity
                    var product = await _dataContext.Products.Where(p => p.Id == cart.ProductId).FirstAsync();
                    product.Quantity -= cart.Quantity;
                    product.Sold += cart.Quantity;
                    _dataContext.Update(product);
                    //++update product quantity
                    _dataContext.Add(orderdetail);
                    _dataContext.SaveChanges();

                }
                HttpContext.Session.Remove("Cart");
                //Send mail order when success
                var receiver = userEmail;
                var subject = "Đặt hàng thành công";
                var message = "Đặt hàng thành công, trải nghiệm dịch vụ nhé.";

                //await _emailSender.SendEmailAsync(receiver, subject, message);

                TempData["success"] = "Đơn hàng đã được tạo,vui lòng chờ duyệt đơn hàng nhé.";

            }

            return RedirectToAction("History", "Account");
        }
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack(MomoInfoModel model)
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            var requestQuery = HttpContext.Request.Query;

            if (requestQuery["resultCode"] != 0) //giao dịch ko thành công lưu db
            {
                var newMomoInsert = new MomoInfoModel
                {
                    OrderId = requestQuery["orderId"],
                    FullName = User.FindFirstValue(ClaimTypes.Email),
                    Amount = decimal.Parse(requestQuery["Amount"]),
                    OrderInfo = requestQuery["orderInfo"],
                    DatePaid = DateTime.Now
                };
                _dataContext.Add(newMomoInsert);
                await _dataContext.SaveChangesAsync();
                var PaymentMethod = "MOMO";
                //Tiến hành đặt đơn hàng khi thanh toán momo thành công
                await Checkout(PaymentMethod, requestQuery["orderId"]);
            }
            else
            {
                TempData["success"] = "Giao dịch Momo ko thành công.";
                return RedirectToAction("Index", "Cart");
            }
            return View(response);

        }
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response.VnPayResponseCode == "00") //giao dịch thành công lưu db
            {
                var newVnpayInsert = new VnpayModel
                {
                    OrderId = response.OrderId,
                    PaymentMethod = response.PaymentMethod,
                    OrderDescription = response.OrderDescription,
                    TransactionId = response.TransactionId,
                    PaymentId = response.PaymentId,
                    DateCreated = DateTime.Now

                };
                _dataContext.Add(newVnpayInsert);
                await _dataContext.SaveChangesAsync();
                //Tiến hành đặt đơn hàng khi thanh toán momo thành công
                var PaymentMethod = response.PaymentMethod;
                var PaymentId = response.PaymentId;
                await Checkout(PaymentMethod, PaymentId);
            }
            else
            {
                TempData["success"] = "Giao dịch Vnpay thành công.";
                return RedirectToAction("Index", "Cart");
            }
            //return Json(response);
            return View(response);
        }

    }
}
