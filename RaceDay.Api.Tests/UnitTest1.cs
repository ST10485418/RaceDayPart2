
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class UnitTest1
    {
        // TEST 1: Public events can be viewed.
        [Fact]
        public async Task PublicEventsEndpoint_ReturnsSuccess()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TEST 2: A user without a token cannot view their profile.
        [Fact]
        public async Task ProfileEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // TEST 3: A user without a token cannot create an event.
        [Fact]
        public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsync(
                "/api/events", CreateEventContent());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // TEST 4: A participant cannot create an event.
        [Fact]
        public async Task Participant_CannotCreateEvent()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"participant_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            var response = await client.PostAsync(
                "/api/events", CreateEventContent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // TEST 5: An organiser can create an event.
        [Fact]
        public async Task Organiser_CanCreateEvent()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"organiser_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDayContext>();

                var user = await context.Users.SingleAsync(
                    u => u.Email == email);

                user.Role = "Organiser";
                await context.SaveChangesAsync();
            }

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            var response = await client.PostAsync(
                "/api/events", CreateEventContent());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // TEST 6: A participant can enrol successfully.
        [Fact]
        public async Task Participant_CanCreateEnrolment()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"enrol_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            int userId = await GetUserId(factory, email);
            int eventCategoryId = await SeedEventCategory(
                factory, userId, true, true);

            var response = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            string json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            Assert.Equal(
                "Enrolment created successfully.",
                document.RootElement.GetProperty("message").GetString());

            Assert.Equal(
                "Pending",
                document.RootElement.GetProperty("enrolment")
                    .GetProperty("status").GetString());
        }

        // TEST 7: The same participant cannot enrol twice.
        [Fact]
        public async Task DuplicateEnrolment_ReturnsConflict()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"duplicate_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            int userId = await GetUserId(factory, email);
            int eventCategoryId = await SeedEventCategory(
                factory, userId, true, true);

            var firstResponse = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondResponse = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        // TEST 8: A participant cannot enrol in an inactive category.
        [Fact]
        public async Task InactiveCategory_ReturnsBadRequest()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"inactive_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            int userId = await GetUserId(factory, email);
            int eventCategoryId = await SeedEventCategory(
                factory, userId, true, false);

            var response = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // TEST 9: A participant can cancel their enrolment.
        [Fact]
        public async Task Participant_CanCancelEnrolment()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"cancel_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            int userId = await GetUserId(factory, email);
            int eventCategoryId = await SeedEventCategory(
                factory, userId, true, true);

            var createResponse = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            string json = await createResponse.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            int enrolmentId = document.RootElement.GetProperty("enrolment")
                .GetProperty("enrolmentID").GetInt32();

            var cancelResponse = await client.DeleteAsync(
                $"/api/enrolments/{enrolmentId}");

            Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayContext>();

            var enrolment = await context.Enrolments
                .SingleAsync(e => e.EnrolmentID == enrolmentId);

            Assert.Equal("Cancelled", enrolment.Status);
        }

        // TEST 10: The API rejects enrolment in an inactive event.
        [Fact]
        public async Task InactiveEvent_ReturnsBadRequest()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"eventinactive_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);
            SetToken(client, token);

            int userId = await GetUserId(factory, email);
            int eventCategoryId = await SeedEventCategory(
                factory, userId, false, true);

            var response = await client.PostAsync(
                "/api/enrolments",
                CreateEnrolmentContent(eventCategoryId));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // TEST 11: A new participant can register successfully.
        [Fact]
        public async Task RegisterUser_WithValidDetails_ReturnsCreated()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"register_{Guid.NewGuid():N}@example.com";

            var registration = new
            {
                firstName = "Test",
                lastName = "Participant",
                email,
                password = "TestPassword123!",
                phone = "0712345678"
            };

            using var content = JsonContent(registration);
            var response = await client.PostAsync(
                "/api/auth/register", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // TEST 12: Registering with an existing email is rejected.
        [Fact]
        public async Task RegisterUser_WithDuplicateEmail_ReturnsConflict()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"duplicateuser_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            var registration = new
            {
                firstName = "Another",
                lastName = "Participant",
                email,
                password,
                phone = "0712345678"
            };

            using var content = JsonContent(registration);
            var response = await client.PostAsync(
                "/api/auth/register", content);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        // TEST 13: A user can log in with valid credentials.
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"login_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            string token = await LoginAndGetToken(client, email, password);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        // TEST 14: Login fails when the password is incorrect.
        [Fact]
        public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"wrongpassword_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            var login = new
            {
                email,
                password = "WrongPassword123!"
            };

            using var content = JsonContent(login);
            var response = await client.PostAsync(
                "/api/auth/login", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // TEST 15: The stored password is not plaintext.
        [Fact]
        public async Task RegisterUser_StoresHashedPassword()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateClient();

            string email = $"hash_{Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            await RegisterUser(client, email, password);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayContext>();

            var user = await context.Users
                .SingleAsync(u => u.Email == email);

            Assert.NotEqual(password, user.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        }

        private static async Task RegisterUser(
            HttpClient client,
            string email,
            string password)
        {
            var registration = new
            {
                firstName = "Test",
                lastName = "User",
                email,
                password,
                phone = "0712345678"
            };

            using var content = JsonContent(registration);
            var response = await client.PostAsync(
                "/api/auth/register", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private static async Task<string> LoginAndGetToken(
            HttpClient client,
            string email,
            string password)
        {
            var login = new { email, password };

            using var content = JsonContent(login);
            var response = await client.PostAsync(
                "/api/auth/login", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            return document.RootElement.GetProperty("accessToken")
                .GetString()
                ?? throw new Exception("Login did not return an access token.");
        }

        private static void SetToken(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);
        }

        private static async Task<int> GetUserId(
            RaceDayWebApplicationFactory factory,
            string email)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayContext>();

            var user = await context.Users
                .SingleAsync(u => u.Email == email);

            return user.UserID;
        }

        private static async Task<int> SeedEventCategory(
            RaceDayWebApplicationFactory factory,
            int organiserId,
            bool eventIsActive,
            bool categoryIsActive)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayContext>();

            var category = new Category
            {
                CategoryName = $"Open {Guid.NewGuid():N}",
                CategoryType = "Run",
                Description = "Automated test category"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var eventItem = new RaceDay.Api.Models.Event
            {
                OrganiserID = organiserId,
                EventName = "Automated Test Event",
                Description = "Event for enrolment testing",
                EventDate = DateTime.UtcNow.AddDays(30),
                Location = "Johannesburg",
                DistanceKm = 5,
                EventType = "Run",
                IsActive = eventIsActive,
                CreatedAt = DateTime.UtcNow
            };

            context.Events.Add(eventItem);
            await context.SaveChangesAsync();

            var eventCategory = new EventCategory
            {
                EventID = eventItem.EventID,
                CategoryID = category.CategoryID,
                EntryFee = 100,
                MaxEntries = 10,
                StartTime = TimeSpan.FromHours(8),
                IsActive = categoryIsActive
            };

            context.EventCategories.Add(eventCategory);
            await context.SaveChangesAsync();

            return eventCategory.EventCategoryID;
        }

        private static StringContent CreateEventContent()
        {
            var eventRequest = new
            {
                eventName = "Test Running Event",
                description = "An event created during automated testing.",
                eventDate = DateTime.UtcNow.AddDays(30),
                location = "Johannesburg",
                distanceKm = 10.0m,
                eventType = "Run",
                isActive = true
            };

            return JsonContent(eventRequest);
        }

        private static StringContent CreateEnrolmentContent(
            int eventCategoryId)
        {
            var request = new
            {
                eventCategoryID = eventCategoryId
            };

            return JsonContent(request);
        }

        private static StringContent JsonContent(object value)
        {
            return new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json");
        }
    }
}
