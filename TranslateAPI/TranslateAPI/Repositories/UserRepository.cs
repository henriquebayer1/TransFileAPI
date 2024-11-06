using TranslateAPI.Domains;
using TranslateAPI.Interfaces;

namespace TranslateAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        public bool ChangePassword(string email, string newPassword)
        {
            throw new NotImplementedException();
        }

        public void Register(User user)
        {
            throw new NotImplementedException();
        }

        public User SearchByEmailAndPassword(string email, string password)
        {
            throw new NotImplementedException();
        }

        public User SearchById(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
