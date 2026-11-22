using MiniSocialMedia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MiniSocialMedia.Models.ViewModle
{
    public class ProfileViewModel
    {
        public User User { get; set; }

        public List<Post> Posts { get; set; }

        public int CurrentUserId { get; set; }

        public bool IsFollowing { get; set; }
        public int PostsCount { get; set; }

        public int FollowersCount { get; set; }

        public int FollowingCount { get; set; }
        public List<User> Followers { get; set; }

        public List<User> Following { get; set; }

    }
}