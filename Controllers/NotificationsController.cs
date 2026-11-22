using MiniSocialMedia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace ĐỀ_ÁN_CUỐI_KỲ_NHÓM_MINISOCIALNETWOK.Controllers
{
    public class NotificationsController : Controller
    {
        private MyDbContext db = new MyDbContext();

        // Hiển thị danh sách thông báo
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = (int)Session["UserId"];

            // Lấy thông báo của người đang đăng nhập
            var notifications = db.Notifications
    .AsNoTracking()
    .Include(n => n.Sender)
    .Where(n => n.UserId == currentUserId)
    .OrderByDescending(n => n.CreatedAt)
    .ToList();

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int id)
        {
            // Kiểm tra đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = (int)Session["UserId"];

            // Tìm thông báo thuộc về người đang đăng nhập
            var notification = db.Notifications.FirstOrDefault(n =>
                n.NotificationId == id &&
                n.UserId == currentUserId);

            if (notification == null)
            {
                return HttpNotFound();
            }

            // Đánh dấu đã đọc
            notification.IsRead = true;

            db.SaveChanges();

            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}