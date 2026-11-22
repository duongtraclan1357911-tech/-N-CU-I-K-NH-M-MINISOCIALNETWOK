
using MiniSocialMedia.Models;
using MiniSocialMedia.Models.ViewModle;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace MiniSocialMedia.Controllers
{
    public class AccountController : Controller
    {
        private MyDbContext db = new MyDbContext();

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Newsfeed", "Post");
            }

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            // Kiểm tra dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Chuẩn hóa dữ liệu
            model.Username = model.Username.Trim();
            model.FullName = model.FullName.Trim();
            model.Email = model.Email.Trim();

            // Kiểm tra Username đã tồn tại chưa
            bool usernameExists = db.Users.Any(u =>
                u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại!");

                return View(model);
            }

            // Kiểm tra Email đã tồn tại chưa
            bool emailExists = db.Users.Any(u =>
                u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email đã được sử dụng!");

                return View(model);
            }

            // Tạo tài khoản mới
            User user = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                AvatarUrl = null,
                Bio = null,
                CreatedAt = DateTime.Now
            };

            // Lưu vào database
            db.Users.Add(user);
            db.SaveChanges();

            TempData["Message"] =
                "Đăng ký tài khoản thành công! Hãy đăng nhập.";

            return RedirectToAction("Login");
        }

        // Hash mật khẩu bằng PBKDF2
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            const int iterations = 100000;

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                return iterations + ":" +
                    Convert.ToBase64String(salt) + ":" +
                    Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split(':');

            if (parts.Length != 3)
            {
                return false;
            }

            int iterations;

            if (!int.TryParse(parts[0], out iterations) ||
                iterations < 100000)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expectedHash = Convert.FromBase64String(parts[2]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations))
                {
                    byte[] actualHash = pbkdf2.GetBytes(32);

                    if (actualHash.Length != expectedHash.Length)
                    {
                        return false;
                    }

                    int difference = 0;

                    for (int i = 0; i < actualHash.Length; i++)
                    {
                        difference |= actualHash[i] ^ expectedHash[i];
                    }

                    return difference == 0;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }


        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Newsfeed", "Post");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.Username = model.Username.Trim();

            // Tìm tài khoản theo Username
            var user = db.Users.FirstOrDefault(u =>
                u.Username == model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng!");
                return View(model);
            }

            // Kiểm tra mật khẩu
            bool isPasswordCorrect = VerifyPassword(
                model.Password,
                user.PasswordHash
            );

            if (!isPasswordCorrect)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng!");
                return View(model);
            }

            // Đăng nhập thành công
            Session["UserId"] = user.UserId;
            Session["Username"] = user.Username;
            Session["Role"] = user.Role;

            TempData["Message"] = "Đăng nhập thành công!";

            return RedirectToAction("Newsfeed", "Post");
        }


        // Đăng xuất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }
    }
}
