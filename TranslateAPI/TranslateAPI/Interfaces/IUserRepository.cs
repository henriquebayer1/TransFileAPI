using TranslateAPI.Domains;

namespace TranslateAPI.Interfaces
{
    public interface IUserRepository
    {

        void Register(User user);

        User SearchById(Guid id);

        User SearchByEmailAndPassword(string email, string password);

        bool ChangePassword(string email, string newPassword);

    }
}
