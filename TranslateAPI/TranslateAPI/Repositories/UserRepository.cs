using MongoDB.Driver;
using TranslateAPI.Domains;
using TranslateAPI.Interfaces;
using TranslateAPI.Services;



namespace TranslateAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _user;


        public UserRepository(MongoDbService mongoDbService)
        {
            _user = mongoDbService.GetDatabase.GetCollection<User>("user");
        }
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

        public async Task<User> SearchById(string id)
        {
            try
            {
                var user = await _user.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (user == null)
                {
                    return null!;
                }

                return user;

            }
            catch (Exception e)
            {

                throw;
            }
        }
    }
}
