
using MiniSocialMedia.Models;
using MiniSocialMedia.Models.ViewModle;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace MiniSocialMedia.Controllers
{
    public class PostController : Controller
    {
        private MyDbContext db = new MyDbContext();
        // Lấy ID người dùng đang đăng nhập từ Session
        private int? GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                return null;
            }

            return (int)Session["UserId"];
        }

        // Kiểm tra tài khoản hiện tại có phải Admin không
        private bool IsAdmin()
        {
            return string.Equals(
                Session["Role"] as string,
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );
        }

        // GET: Post
        public ActionResult Newsfeed(string searchString, int? page)
        {
            int? userId = GetCurrentUserId();

            // Nếu chưa đăng nhập thì chuyển đến trang Login
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy UserId của tài khoản đang đăng nhập
            int currentUserId = userId.Value;

            int pageSize = 5;
            int pageNumber = page ?? 1;

            var followingIds = db.Follows
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId)
                .ToList();

            var postsQuery = db.Posts.AsQueryable();

            // Admin xem được tất cả bài viết
            if (!IsAdmin())
            {
                // Customer chỉ xem bài viết của mình
                // và những người mà mình đang theo dõi
                postsQuery = postsQuery.Where(p =>
                    p.UserId == currentUserId ||
                    followingIds.Contains(p.UserId));
            }

            // Tìm kiếm theo nội dung bài viết
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                postsQuery = postsQuery.Where(p =>
                    p.Content.Contains(searchString));
            }

            // Sắp xếp bài viết mới nhất
            postsQuery = postsQuery
                .OrderByDescending(p => p.CreatedAt);

            var viewModel = new NewsfeedVM
            {
                Posts = postsQuery.ToPagedList(pageNumber, pageSize),
                CurrentUserId = currentUserId
            };

            // Lấy ID các bài viết đang hiển thị
            var postIds = viewModel.Posts
                .Select(p => p.PostId)
                .ToList();

            // Đếm số lượng từng loại biểu cảm trên từng bài viết
            ViewBag.ReactionCounts = db.Likes
                .Where(l => postIds.Contains(l.PostId))
                .GroupBy(l => new { l.PostId, l.ReactionType })
                .ToDictionary(
                    g => g.Key.PostId + "_" + g.Key.ReactionType,
                    g => g.Count()
                );

            // Lấy biểu cảm của người dùng hiện tại trên từng bài viết
            ViewBag.UserReactions = db.Likes
                .Where(l => postIds.Contains(l.PostId)
                         && l.UserId == currentUserId)
                .ToDictionary(l => l.PostId, l => l.ReactionType);

            ViewBag.SearchString = searchString;

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult CreatePost(string Content, HttpPostedFileBase ImageFile)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra người dùng đã đăng nhập chưa
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy ID người dùng đang đăng nhập
            int currentUserId = userId.Value;

            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["Message"] = "Nội dung bài viết không được để trống!";
                return RedirectToAction("Newsfeed");
            }

            if (Content.Trim().Length > 2000)
            {
                TempData["Message"] = "Nội dung không được vượt quá 2000 ký tự!";
                return RedirectToAction("Newsfeed");
            }

            var post = new Post
            {
                UserId = currentUserId,
                Content = Content.Trim(),
                CreatedAt = DateTime.Now
            };

            db.Posts.Add(post);
            db.SaveChanges();

            TempData["Message"] = "Đăng bài viết thành công!";

            return RedirectToAction("Newsfeed");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateComment(int postId, string Content)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;

            // Kiểm tra bài viết có tồn tại không
            var post = db.Posts.Find(postId);

            if (post == null)
            {
                TempData["Message"] = "Bài viết không tồn tại!";
                return RedirectToAction("Newsfeed");
            }

            // Kiểm tra nội dung
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["Message"] = "Nội dung bình luận không được để trống!";
                return RedirectToAction("Newsfeed");
            }

            if (Content.Trim().Length > 1000)
            {
                TempData["Message"] = "Bình luận tối đa 1000 ký tự!";
                return RedirectToAction("Newsfeed");
            }

            // Tạo bình luận mới
            var comment = new Comment
            {
                PostId = postId,
                UserId = currentUserId,
                Content = Content.Trim(),
                CreatedAt = DateTime.Now
            };

            db.Comments.Add(comment);
            db.SaveChanges();

            TempData["Message"] = "Bình luận thành công!";

            return RedirectToAction("Newsfeed");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComment(int id)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;

            var comment = db.Comments.Find(id);

            if (comment == null)
            {
                TempData["Message"] = "Bình luận không tồn tại!";
                return RedirectToAction("Newsfeed");
            }

            if (comment.UserId != currentUserId)
            {
                TempData["Message"] = "Bạn không có quyền xóa bình luận này!";
                return RedirectToAction("Newsfeed");
            }

            db.Comments.Remove(comment);
            db.SaveChanges();

            TempData["Message"] = "Xóa bình luận thành công!";

            return RedirectToAction("Newsfeed");
        }
        // SỬA BÌNH LUẬN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditComment(int id, string Content)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;

            // Tìm bình luận cần sửa
            var comment = db.Comments.Find(id);

            if (comment == null)
            {
                TempData["Message"] = "Bình luận không tồn tại!";
                return RedirectToAction("Newsfeed");
            }

            // Chỉ người tạo bình luận mới được sửa
            if (comment.UserId != currentUserId)
            {
                TempData["Message"] = "Bạn không có quyền sửa bình luận này!";
                return RedirectToAction("Newsfeed");
            }

            // Kiểm tra nội dung
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["Message"] = "Nội dung bình luận không được để trống!";
                return RedirectToAction("Newsfeed");
            }

            if (Content.Trim().Length > 1000)
            {
                TempData["Message"] = "Bình luận tối đa 1000 ký tự!";
                return RedirectToAction("Newsfeed");
            }

            // Cập nhật nội dung bình luận
            comment.Content = Content.Trim();

            db.SaveChanges();

            TempData["Message"] = "Sửa bình luận thành công!";

            return RedirectToAction("Newsfeed");
        }

        // 3. XÓA BÀI ĐĂNG (Ràng buộc nghiệp vụ: Chỉ xóa bài của chính mình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePost(int id)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;

            // Tìm bài viết
            var post = db.Posts.Find(id);

            if (post == null)
            {
                TempData["Message"] = "Bài viết không tồn tại!";
                return RedirectToAction("Newsfeed");
            }

            // Chỉ chủ bài viết mới được xóa
            if (post.UserId != currentUserId && !IsAdmin())
            {
                TempData["Message"] = "Bạn không có quyền xóa bài viết này!";
                return RedirectToAction("Newsfeed");
            }

            // Giữ thông báo nhưng xóa liên kết đến bài viết
            var notifications = db.Notifications
                .Where(n => n.PostId == id)
                .ToList();

            foreach (var notification in notifications)
            {
                notification.PostId = null;
            }

            // Xóa các bình luận của bài viết
            var comments = db.Comments
                .Where(c => c.PostId == id)
                .ToList();

            db.Comments.RemoveRange(comments);

            // Xóa các biểu cảm của bài viết
            var likes = db.Likes
                .Where(l => l.PostId == id)
                .ToList();

            db.Likes.RemoveRange(likes);

            // Xóa bài viết
            db.Posts.Remove(post);

            // Lưu tất cả thay đổi
            db.SaveChanges();

            TempData["Message"] = "Xóa bài viết thành công!";

            return RedirectToAction("Newsfeed");
        }

        // 4. THẢ LIKE / UNLIKE (AJAX)
        // GET: Post/EditPost/1
        [HttpGet]
        public ActionResult EditPost(int id)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;

            var post = db.Posts.Find(id);

            if (post == null)
            {
                return HttpNotFound("Bài viết không tồn tại.");
            }

            // Chỉ chủ bài viết mới được sửa
            if (post.UserId != currentUserId && !IsAdmin())
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền sửa bài viết này.");
            }

            return View(post);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(Post model)
        {
            int? userId = GetCurrentUserId();

            // Kiểm tra đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = userId.Value;
            if (model == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var post = db.Posts.Find(model.PostId);

            if (post == null)
            {
                return HttpNotFound("Bài viết không tồn tại.");
            }

            // Kiểm tra quyền sở hữu
            if (post.UserId != currentUserId && !IsAdmin())
            {
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền sửa bài viết này.");
            }

            // Kiểm tra nội dung
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(
                    "Content",
                    "Nội dung không được để trống.");
            }
            else if (model.Content.Trim().Length > 2000)
            {
                ModelState.AddModelError(
                    "Content",
                    "Nội dung tối đa 2000 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                model.UserId = post.UserId;
                model.CreatedAt = post.CreatedAt;
                model.ImageUrl = post.ImageUrl;

                return View(model);
            }

            post.Content = model.Content.Trim();

            db.SaveChanges();

            TempData["Message"] = "Cập nhật bài viết thành công!";

            return RedirectToAction("Newsfeed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleReaction(int postId, string reactionType)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để biểu cảm!"
                });
            }

            int currentUserId = userId.Value;

            var post = db.Posts.Find(postId);

            if (post == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Bài viết không tồn tại!"
                });
            }

            // Chỉ chấp nhận 6 loại biểu cảm
            string[] validReactions =
            {
        "Like", "Love", "Haha", "Wow", "Sad", "Angry"
    };

            if (string.IsNullOrWhiteSpace(reactionType) ||
    !validReactions.Contains(reactionType))
            {
                return Json(new
                {
                    success = false,
                    message = "Biểu cảm không hợp lệ!"
                });
            }

            var existingLike = db.Likes.FirstOrDefault(l =>
                l.PostId == postId &&
                l.UserId == currentUserId);

            bool isReacted;

            if (existingLike != null)
            {
                if (existingLike.ReactionType == reactionType)
                {
                    // Chọn lại biểu cảm cũ thì xóa
                    db.Likes.Remove(existingLike);
                    isReacted = false;
                }
                else
                {
                    // Chọn biểu cảm khác thì cập nhật
                    existingLike.ReactionType = reactionType;
                    existingLike.CreatedAt = DateTime.Now;
                    isReacted = true;
                }
            }
            else
            {
                // Chưa có biểu cảm thì thêm mới
                db.Likes.Add(new Like
                {
                    PostId = postId,
                    UserId = currentUserId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                });

                isReacted = true;

                // Chỉ thông báo khi biểu cảm bài viết của người khác
                if (post.UserId != currentUserId)
                {
                    db.Notifications.Add(new Notification
                    {
                        UserId = post.UserId,
                        SenderId = currentUserId,
                        PostId = postId,
                        Message = "đã bày tỏ cảm xúc với bài viết của bạn",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            db.SaveChanges();

            int totalReactions = db.Likes.Count(l => l.PostId == postId);

            return Json(new
            {
                success = true,
                isReacted = isReacted,
                reactionType = isReacted ? reactionType : null,
                totalReactions = totalReactions
            });
        }

    }
}