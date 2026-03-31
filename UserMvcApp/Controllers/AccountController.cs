using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UserMvcApp.ViewModels;

namespace UserMvcApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        //  REGISTER
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("UserApi");
                var payload = JsonSerializer.Serialize(new
                {
                    fullName = model.FullName,
                    email = model.Email,
                    password = model.Password,
                    phone = model.Phone
                });

                var response = await client.PostAsync("api/auth/register",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", $"Registration failed: {responseText}");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Could not reach API: {ex.Message}");
                return View(model);
            }
        }

        //  LOGIN 
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("UserApi");

                var payload = JsonSerializer.Serialize(new
                {
                    email = model.Email,
                    password = model.Password
                });

                var response = await client.PostAsync("api/auth/login",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", $"Error {(int)response.StatusCode}: {json}");
                    return View(model);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<JsonElement>(json, options);

                var token = result.GetProperty("token").GetString()!;
                var user = result.GetProperty("user");
                var userId = user.GetProperty("id").GetInt32().ToString();
                var fullName = user.GetProperty("fullName").GetString()!;
                var email = user.GetProperty("email").GetString()!;

                // Save to session
                HttpContext.Session.SetString("JwtToken", token);
                HttpContext.Session.SetString("UserId", userId);

                // Sign in with cookie
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email)
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return RedirectToAction("Profile", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }


        // PROFILE
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var token = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var client = _httpClientFactory.CreateClient("UserApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/users/{userId}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Login");

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var user = JsonSerializer.Deserialize<JsonElement>(json, options);

            var model = new ProfileViewModel
            {
                Id = user.GetProperty("id").GetInt32(),
                FullName = user.GetProperty("fullName").GetString()!,
                Email = user.GetProperty("email").GetString()!,
                Phone = user.GetProperty("phone").GetString()!,
            };

            return View(model);
        }

        // EDIT PROFILE 
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var token = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            try
            {
                var client = _httpClientFactory.CreateClient("UserApi");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"api/users/{userId}");
                var json = await response.Content.ReadAsStringAsync();

                // Show exactly what the API returned
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Debug"] = $"API failed! Status={response.StatusCode}, Body={json}";
                    return RedirectToAction("Login");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var user = JsonSerializer.Deserialize<JsonElement>(json, options);

                var model = new EditProfileViewModel
                {
                    Id = user.GetProperty("id").GetInt32(),
                    FullName = user.GetProperty("fullName").GetString()!,
                    Email = user.GetProperty("email").GetString()!,
                    Phone = user.GetProperty("phone").GetString()!
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Debug"] = $"Exception: {ex.Message}";
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = HttpContext.Session.GetString("JwtToken");
            if (token == null) return RedirectToAction("Login");

            var client = _httpClientFactory.CreateClient("UserApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var payload = JsonSerializer.Serialize(new
            {
                fullName = model.FullName,
                phone = model.Phone,
                newEmail = model.NewEmail,
                currentPassword = model.CurrentPassword,
                newPassword = model.NewPassword
            });

            var response = await client.PutAsync($"api/users/{model.Id}",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            else
                ModelState.AddModelError("", responseText);

            return View(model);
        }

        //  LOGOUT 
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}