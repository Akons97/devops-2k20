using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using WebApplication.Entities;
using WebApplication.Exceptions;
using WebApplication.Models.Authentication;
using WebApplication.Services;

using Xunit;

namespace WebApplication.Tests
{
    public class UserServiceTests
    {
        private static UserService CreateUserService(DatabaseContext dbContext)
        {
            return new UserService(dbContext, Mock.Of<ILogger<UserService>>());
        }
        
        [Fact]
        public async Task CreateUser_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(CreateUser_Works))
                .Options;

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                var model = new RegisterModel { Username = "a", Email = "a@a.a", Pwd = "a" };

                await service.CreateUser(model);
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                // We assume a single user is registered because the db is "fresh".
                var user = dbContext.Users.SingleOrDefault();
                
                Assert.NotNull(user);
                Assert.Equal("a", user.Username);
                Assert.Equal("a@a.a", user.Email);
                
                // Ensure password is hashed
                Assert.NotEqual("a", user.Password);
            }
        }
        
        [Fact]
        public async Task CreateUser_Throws_WhenUsernameAlreadyExists()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(CreateUser_Throws_WhenUsernameAlreadyExists))
                .Options;

            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                
                await Assert.ThrowsAsync<CreateUserException>(async () => {
                    var service = CreateUserService(dbContext);
                    var model = new RegisterModel { Username = "a", Email = "a@a.a", Pwd = "a" };

                    await service.CreateUser(model);
                });
            }
        }
        
        [Fact]
        public async Task CreateUser_Throws_WhenEmailAlreadyExists()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(CreateUser_Throws_WhenUsernameAlreadyExists))
                .Options;

            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                
                await Assert.ThrowsAsync<CreateUserException>(async () => {
                    var service = CreateUserService(dbContext);
                    var model = new RegisterModel { Username = "a", Email = "a@a.a", Pwd = "a" };

                    await service.CreateUser(model);
                });
            }
        }

        [Fact]
        public async Task GetUserFromUsername_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(GetUserFromUsername_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                dbContext.Users.Add(new User { Username = "c", Email = "c@c.c", Password = "c" });
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                var user = await service.GetUserFromUsername("b");
                
                Assert.NotNull(user);
                Assert.Equal("b", user.Username);
            }
        }
        
        [Fact]
        public async Task GetUserFromUsername_Throws_WhenUserDoesNotExit()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(GetUserFromUsername_Works))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () => 
                {
                    var service = CreateUserService(dbContext);
                    
                    await service.GetUserFromUsername("b");
                });
            }
        }
        
        [Fact]
        public async Task IsUserFollowing_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(IsUserFollowing_Works))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);

                var isFollowing = await service.IsUserFollowing(2, "a");
                
                Assert.False(isFollowing);
            }
        }
        
        [Fact]
        public async Task GetUserFollowers_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(GetUserFollowers_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                dbContext.Users.Add(new User { Username = "c", Email = "c@c.c", Password = "c" });
                
                dbContext.Followers.Add(new Follower { WhoID = 2, WhomID = 1 });
                dbContext.Followers.Add(new Follower { WhoID = 3, WhomID = 1 });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                var result = await service.GetUserFollowers("a", 5);
                
                Assert.Equal(2, result.Count);
            }
        }
        
        [Fact]
        public async Task GetUserFollowers_Throws_WhenUserDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(GetUserFollowers_Throws_WhenUserDoesNotExist))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                dbContext.Users.Add(new User { Username = "c", Email = "c@c.c", Password = "c" });
                
                dbContext.Followers.Add(new Follower { WhoID = 2, WhomID = 1 });
                dbContext.Followers.Add(new Follower { WhoID = 3, WhomID = 1 });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                var result = await service.GetUserFollowers("a", 5);
                
                Assert.Equal(2, result.Count);
            }
        }
        
        [Fact]
        public async Task AddFollower_UsingUsername_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUsername_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                
                await service.AddFollower("a", "b");
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                var followers = dbContext.Followers.Where(row => row.WhomID == 1).ToList();
                
                Assert.Single(followers);
            }
        }
        
        [Fact]
        public async Task AddFollower_UsingUsername_Throws_WhenUserToFollow_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUsername_Throws_WhenUserToFollow_DoesNotExist))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () =>
                {
                    var service = CreateUserService(dbContext);

                    await service.AddFollower("a", "b");
                });
            }
        }
        
        [Fact]
        public async Task AddFollower_UsingUsername_Throws_WhenUserFollowing_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUsername_Throws_WhenUserFollowing_DoesNotExist))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.SaveChanges();
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () =>
                {
                    var service = CreateUserService(dbContext);

                    await service.AddFollower("a", "b");
                });
            }
        }
                
        [Fact]
        public async Task AddFollower_UsingUserID_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUserID_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                
                await service.AddFollower(1, "b");
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                var followers = dbContext.Followers.Where(row => row.WhomID == 1).ToList();
                
                Assert.Single(followers);
            }
        }
        
        [Fact]
        public async Task AddFollower_UsingUserID_Throws_WhenUserToFollow_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUserID_Throws_WhenUserToFollow_DoesNotExist))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () =>
                {
                    var service = CreateUserService(dbContext);

                    await service.AddFollower(1, "b");
                });
            }
        }
        
        [Fact]
        public async Task AddFollower_UsingUserID_Throws_WhenUserFollowing_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(AddFollower_UsingUserID_Throws_WhenUserFollowing_DoesNotExist))
                .Options;

            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.SaveChanges();
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () =>
                {
                    var service = CreateUserService(dbContext);

                    await service.AddFollower(1, "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUsername_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUsername_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                
                dbContext.Followers.Add(new Follower { WhoID = 2, WhomID = 1 });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                
                await service.RemoveFollower("a", "b");
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                var followers = dbContext.Followers.Where(row => row.WhomID == 1).ToList();
                
                Assert.Empty(followers);
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUsername_Throws_WhenUserToFollow_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUsername_Throws_WhenUserToFollow_DoesNotExist))
                .Options;
            
            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower("a", "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUsername_Throws_WhenUserFollowing_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUsername_Throws_WhenUserFollowing_DoesNotExist))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower("a", "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUsername_Throws_WhenFollowerRelation_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUsername_Throws_WhenFollowerRelation_DoesNotExist))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownFollowerRelationException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower("a", "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUserID_Works()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUserID_Works))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });
                
                dbContext.Followers.Add(new Follower { WhoID = 2, WhomID = 1 });
                
                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                var service = CreateUserService(dbContext);
                
                await service.RemoveFollower(2, "a");
            }
            
            await using (var dbContext = new DatabaseContext(options))
            {
                var followers = dbContext.Followers.Where(row => row.WhomID == 1).ToList();
                
                Assert.Empty(followers);
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUserID_Throws_WhenUserToFollow_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUserID_Throws_WhenUserToFollow_DoesNotExist))
                .Options;

            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower(1, "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUserID_Throws_WhenUserFollowing_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUserID_Throws_WhenUserFollowing_DoesNotExist))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });

                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownUserException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower(1, "b");
                });
            }
        }
        
        [Fact]
        public async Task RemoveFollower_UsingUserID_Throws_WhenFollowerRelation_DoesNotExist()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(nameof(RemoveFollower_UsingUserID_Throws_WhenFollowerRelation_DoesNotExist))
                .Options;

            // Seeding the database
            await using (var dbContext = new DatabaseContext(options))
            {
                dbContext.Users.Add(new User { Username = "a", Email = "a@a.a", Password = "a" });
                dbContext.Users.Add(new User { Username = "b", Email = "b@b.b", Password = "b" });

                dbContext.SaveChanges();
            }

            await using (var dbContext = new DatabaseContext(options))
            {
                await Assert.ThrowsAsync<UnknownFollowerRelationException>(async () => 
                {
                    var service = CreateUserService(dbContext);

                    await service.RemoveFollower(1, "b");
                });
            }
        }
    }
}
