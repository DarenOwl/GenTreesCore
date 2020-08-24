using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GenTreesCore.Entities;

namespace GenTreesCore.Services
{
    public interface IUserRepository
    {
        bool EmailIsConfirmed(string login);
        bool EmailIsRegistered(string email);
        User GetUserById(int id);
        User GetUserByLogin(string login);
        User LogIn(string login, string password);
        bool LoginIsRegistered(string login);
        void Register(string login, string email, string password);
    }

    /// <summary>
    /// Класс, реализующий действия с пользователями
    /// </summary>
    public class UserService : IUserRepository
    {
        private ApplicationContext db;

        public UserService(ApplicationContext db)
        {
            this.db = db;
        }

        public User GetUserByLogin(string login)
        {
            return db.Users.FirstOrDefault(u => u.Login == login);
        }

        public User GetUserById(int id)
        {
            return db.Users.FirstOrDefault(u => u.Id == id);
        }

        public void Register(string login, string email, string password)
        {
            var salt = GenerateSalt();
            var hash = GetPasswordHash(password, salt);
            db.Users.Add(
                new User
                {
                    Login = login,
                    PasswordHash = hash,
                    Salt = salt,
                    Email = email,
                    EmailConfirmed = false,
                    DateCreated = DateTime.Now,
                    LastVisit = DateTime.Now,
                    Role = Role.User
                });
            db.SaveChangesAsync();
        }

        public bool LoginIsRegistered(string login)
        {
            return db.Users.FirstOrDefault(x => x.Login == login) != null;
        }

        public bool EmailIsRegistered(string email)
        {
            return db.Users.FirstOrDefault(x => x.Email == email) != null;
        }
        public bool EmailIsConfirmed(string login)
        {
            return db.Users.FirstOrDefault(x => x.Login == login && x.EmailConfirmed) != null;
        }

        public User LogIn(string login, string password)
        {
            var user = db.Users.FirstOrDefault(x => x.Login == login);

            if (user == null)
                return null;

            var hash = GetPasswordHash(password, user.Salt);
            if (user.PasswordHash != hash)
                return null;

            user.LastVisit = DateTime.Now;
            db.Users.Update(user);
            db.SaveChanges();
            return user;
        }
        private string GenerateSalt()
        {
            byte[] data = new byte[8];
            RandomNumberGenerator.Fill(data);
            return Encoding.UTF8.GetString(data);
        }

        private string GetPasswordHash(string password, string salt)
        {
            byte[] data = Encoding.UTF8.GetBytes(password + salt);
            SHA256 sha256 = new SHA256CryptoServiceProvider();
            byte[] result = sha256.ComputeHash(data);

            return Encoding.UTF8.GetString(result);
        }
    }
}
