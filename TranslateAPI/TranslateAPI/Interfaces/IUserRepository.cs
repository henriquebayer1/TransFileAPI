using TranslateAPI.Domains;

namespace TranslateAPI.Interfaces
{
    public interface IUserRepository
    {
        void Register(User user);

        Task<User> SearchById(string id);

        User SearchByEmailAndPassword(string email, string password);

        bool ChangePassword(string email, string newPassword);

    }
}
