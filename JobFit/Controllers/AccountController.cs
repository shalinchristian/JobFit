using JobFit.Helpers;
using JobFit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JobFit.Controllers
{
    public class AccountController : Controller
    {
        private readonly string connectionString;

        public AccountController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Check that the username and email are not taken.
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email";
                using (SqlCommand check = new SqlCommand(checkQuery, con))
                {
                    check.Parameters.AddWithValue("@Username", model.Username);
                    check.Parameters.AddWithValue("@Email", model.Email);
                    int count = (int)check.ExecuteScalar();
                    if (count > 0)
                    {
                        ModelState.AddModelError("", "That username or email is already registered.");
                        return View(model);
                    }
                }

                string insertQuery = "INSERT INTO Users (Username, Email, PasswordHash) VALUES (@Username, @Email, @PasswordHash)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hash(model.Username, model.Password));
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Account created. Please log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT UserID, Username, PasswordHash FROM Users WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string hash = reader.GetString(2);

                            if (PasswordHelper.Verify(username, hash, model.Password))
                            {
                                HttpContext.Session.SetInt32("UserID", userId);
                                HttpContext.Session.SetString("Username", username);
                                return RedirectToAction("Index", "Applications");
                            }
                        }
                    }
                }
            }

            ModelState.AddModelError("", "Wrong username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
