using NUnit.Framework;
using FocusFinderApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using FocusFinderApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FocusFinderApp.Tests
{
    public class BookmarkTests
    {
        private FocusFinderDbContext _dbContext;

        private BookmarkController _controller;

        [SetUp] // Runs before each test
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<FocusFinderDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase") // Unique per test session
                .Options;

            _dbContext = new FocusFinderDbContext(options);
            _dbContext.Database.EnsureCreated();

            // Mocking session
            var httpContext = new DefaultHttpContext();
            var sessionMock = new Mock<ISession>(); // Use Moq for mocking
            var sessionValues = new Dictionary<string, byte[]>(); // Store session values

            sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => sessionValues[key] = value);
    
            sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) => sessionValues.TryGetValue(key, out value));

            httpContext.Session = sessionMock.Object; // Assign mock session

            // Set UserId in session
            var userIdBytes = BitConverter.GetBytes(1);
            httpContext.Session.Set("UserId", userIdBytes);

            _controller = new BookmarkController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }


        [TearDown] // Runs after each test
        public void TearDown()
        {
            _dbContext.Bookmarks.RemoveRange(_dbContext.Bookmarks);
            _dbContext.Users.RemoveRange(_dbContext.Users);
            _dbContext.SaveChanges();

            _controller?.Dispose();
            _dbContext?.Dispose();
            // _dbContext?.Dispose();
            // _controller?.Dispose();
            // _controller = null;
        }

        [Test]
        public void Test_BookmarkAddedToDatabase()
        {
            // Arrange
            var bookmark = new Bookmark { userId = 1, locationId = 1 };

            // Act
            _dbContext.Bookmarks.Add(bookmark);
            _dbContext.SaveChanges();

            // Assert
            var savedBookmark = _dbContext.Bookmarks.FirstOrDefault(a => a.userId == 1);
            Assert.That(savedBookmark, Is.Not.Null, "Achievement should be saved to the database.");
            Assert.That(savedBookmark.userId, Is.EqualTo(1), "User ID should be 1.");
        }
        
        
        [Test]
        public void Test_BookmarkAddedToUserBookmarkPage()
        {
            // Arrange
            var user = new User 
            {  
                Id = 1, 
                Username = "testUser",
                Email = "test@example.com",
                Password = "hashedpassword123"
            };
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            var bookmark = new Bookmark { userId = 1, locationId = 1 };
            _dbContext.Bookmarks.Add(bookmark);
            _dbContext.SaveChanges();

            // Setup session manually (instead of mocking GetString/GetInt32)
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockHttpSession();
            httpContext.Session.SetInt32("UserId", 1);
            httpContext.Session.SetString("Username", "testUser");

            _controller = new BookmarkController(_dbContext) 
            { 
                ControllerContext = new ControllerContext { HttpContext = httpContext } 
            };

            // Act
            var result = _controller.UserBookmarks();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<Bookmark>;

            Assert.That(model, Is.Not.Null, "Expected model to not be null");
            Assert.That(model.Count, Is.EqualTo(1), "Expected 1 bookmark but found 0");
        }

            
        // [Test]
        // public void BookmarkRemoved()
        // {
            // // Arrange
            // var bookmark = new Bookmark { userId = 1, locationId = 1 };

            // // Act
            // _dbContext.Bookmarks.Add(bookmark);
            // _dbContext.SaveChanges();

            // // Assert
            // var savedBookmark = _dbContext.Bookmarks.FirstOrDefault(a => a.userId == 1);
            // Assert.That(savedBookmark, Is.Not.Null, "Bookmark should be saved to the database.");
            // Assert.That(savedBookmark.locationId, Is.EqualTo(1), "Location ID should be 1.");
        // }

        }
    }
    
    // Mock session class
    public class MockHttpSession : ISession
    {
        private readonly Dictionary<string, object> _sessionStorage = new Dictionary<string, object>();
    
        public IEnumerable<string> Keys => _sessionStorage.Keys;
    
        public string Id => Guid.NewGuid().ToString();
    
        public bool IsAvailable => true;
    
        public void Clear() => _sessionStorage.Clear();
    
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
        public void Remove(string key) => _sessionStorage.Remove(key);
    
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
    
        public bool TryGetValue(string key, out byte[] value)
        {
            if (_sessionStorage.TryGetValue(key, out var objValue) && objValue is byte[] byteValue)
            {
                value = byteValue;
                return true;
            }
            value = null;
            return false;
        }
    
        public void SetInt32(string key, int value) => _sessionStorage[key] = BitConverter.GetBytes(value);
    
        public int? GetInt32(string key)
        {
            if (_sessionStorage.TryGetValue(key, out var objValue) && objValue is byte[] byteValue)
            {
                return BitConverter.ToInt32(byteValue, 0);
            }
            return null;
        }
    
        public void SetString(string key, string value) => _sessionStorage[key] = System.Text.Encoding.UTF8.GetBytes(value);
    
        public string GetString(string key)
        {
            if (_sessionStorage.TryGetValue(key, out var objValue) && objValue is byte[] byteValue)
            {
                return System.Text.Encoding.UTF8.GetString(byteValue);
            }
            return null;
        }
    }
