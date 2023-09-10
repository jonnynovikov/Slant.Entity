using Slant.Entity.Demo.DomainModel;
using System;
using System.Threading.Tasks;

namespace Slant.Entity.Demo.Repositories;

public interface IUserRepository 
{
    User Get(Guid userId);
    ValueTask<User> GetAsync(Guid userId);
    void Add(User user);
}