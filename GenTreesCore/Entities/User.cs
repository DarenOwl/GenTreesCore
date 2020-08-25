using System;

namespace GenTreesCore.Entities
{
    public class User : IIdentified
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastVisit { get; set; }
        public Role Role { get; set; }
    }

    public enum Role
    {
        User,
        Admin
    }
}