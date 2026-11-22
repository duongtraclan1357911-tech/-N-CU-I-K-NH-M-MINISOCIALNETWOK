using MiniSocialMedia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MiniSocialMedia.Models.ViewModle;

namespace ĐỀ_ÁN_CUỐI_KỲ_NHÓM_MINISOCIALNETWOK.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        private MyDbContext db = new MyDbContext();
        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                return 0;
            }

            return (int)Session["UserId"];
        }

        // Hiển thị danh sách người dùng
        public ActionResult Index(string searchString)
        {
            var users = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                users = users.Where(u =>
                    u.Username.Contains(searchString) ||
                    u.FullName.Contains(searchString));
            }

            int currentUserId = GetCurrentUserId();

            // Lấy danh sách ID người mà mình đang theo dõi
            ViewBag.FollowingIds = db.Follows
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId)
                .ToList();

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.SearchString = searchString;

            return View(users.ToList());
        }
        // Hiển thị trang chỉnh sửa

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = GetCurrentUserId();

            bool isAdmin = string.Equals(
                Session["Role"] as string,
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );

            // Customer chỉ được sửa hồ sơ của mình
            // Admin được sửa hồ sơ của mọi người
            if (id != currentUserId && !isAdmin)
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền chỉnh sửa hồ sơ này!");
            }

            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }


        // Lưu thông tin sau khi chỉnh sửa

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User model)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = GetCurrentUserId();

            bool isAdmin = string.Equals(
                Session["Role"] as string,
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );

            // Customer không được sửa hồ sơ người khác
            if (model.UserId != currentUserId && !isAdmin)
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền chỉnh sửa hồ sơ này!");
            }

            var user = db.Users.Find(model.UserId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Không cho phép thay đổi Role thông qua form chỉnh sửa hồ sơ
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            string newUsername = (model.Username ?? "").Trim();
            string newFullName = (model.FullName ?? "").Trim();
            string newEmail = (model.Email ?? "").Trim();

            bool usernameExists = db.Users.Any(u =>
                u.Username == newUsername &&
                u.UserId != model.UserId);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã được sử dụng!");

                return View(user);
            }

            bool emailExists = db.Users.Any(u =>
                u.Email == newEmail &&
                u.UserId != model.UserId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email đã được sử dụng!");

                return View(user);
            }

            user.Username = newUsername;
            user.FullName = newFullName;
            user.Email = newEmail;
            user.AvatarUrl = model.AvatarUrl;
            user.Bio = model.Bio;

            db.SaveChanges();

            TempData["Message"] = "Cập nhật thông tin thành công!";

            return RedirectToAction("UserProfile",
                new { id = user.UserId });
        }

        // Theo dõi hoặc bỏ theo dõi người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleFollow(int followingId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = GetCurrentUserId();

            // Không cho phép tự theo dõi chính mình
            if (currentUserId == followingId)
            {
                TempData["Message"] = "Bạn không thể tự theo dõi chính mình!";
                return RedirectToAction("Index");
            }

            // Kiểm tra người được theo dõi có tồn tại không
            var user = db.Users.Find(followingId);

            if (user == null)
                return HttpNotFound();

            // Kiểm tra đã theo dõi hay chưa
            var existingFollow = db.Follows.FirstOrDefault(f =>
                f.FollowerId == currentUserId &&
                f.FollowingId == followingId);

            if (existingFollow != null)
            {
                // Đã theo dõi thì bỏ theo dõi
                db.Follows.Remove(existingFollow);
                TempData["Message"] = "Đã bỏ theo dõi!";
            }
            else
            {
                // Chưa theo dõi thì thêm mới
                db.Follows.Add(new Follow
                {
                    FollowerId = currentUserId,
                    FollowingId = followingId,
                    CreatedAt = DateTime.Now
                });

                TempData["Message"] = "Đã theo dõi người dùng!";
            }

            db.SaveChanges();

            return RedirectToAction("Index");
        }
        // Hiển thị trang cá nhân
        public ActionResult UserProfile(int id)
        {
            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            int currentUserId = GetCurrentUserId();

            // Lấy bài viết của người dùng
            var posts = db.Posts
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            // Kiểm tra trạng thái theo dõi
            bool isFollowing = db.Follows.Any(f =>
                f.FollowerId == currentUserId &&
                f.FollowingId == id);

            // Đếm số người theo dõi
            int followersCount = db.Follows.Count(f =>
                f.FollowingId == id);

            // Đếm số người đang theo dõi
            int followingCount = db.Follows.Count(f =>
                f.FollowerId == id);
            // Danh sách người đang theo dõi tài khoản này
            var followers = db.Follows
                .Where(f => f.FollowingId == id)
                .Select(f => f.FollowerId)
                .ToList();

            var followerUsers = db.Users
                .Where(u => followers.Contains(u.UserId))
                .ToList();

            // Danh sách tài khoản mà người này đang theo dõi
            var following = db.Follows
                .Where(f => f.FollowerId == id)
                .Select(f => f.FollowingId)
                .ToList();

            var followingUsers = db.Users
                .Where(u => following.Contains(u.UserId))
                .ToList();

            var model = new ProfileViewModel
            {
                User = user,
                Posts = posts,
                CurrentUserId = currentUserId,
                IsFollowing = isFollowing,

                PostsCount = posts.Count,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                Followers = followerUsers,
                Following = followingUsers
            };

            return View(model);
        }
    }
}