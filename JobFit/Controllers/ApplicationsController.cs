using JobFit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JobFit.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly string connectionString;

        public ApplicationsController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // The logged-in user's id, or null if nobody is logged in.
        private int? CurrentUserId
        {
            get { return HttpContext.Session.GetInt32("UserID"); }
        }

        // Dashboard: list of the user's applications, optionally filtered by status.
        public IActionResult Index(string? status)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            if (status != null && !JobApplication.Statuses.Contains(status))
                status = null;

            List<JobApplication> list = new List<JobApplication>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT AppID, Company, Role, Location, AppliedDate, Status, Notes " +
                               "FROM Applications WHERE UserID = @UserID";
                if (status != null)
                    query += " AND Status = @Status";
                query += " ORDER BY AppliedDate DESC, AppID DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserID", CurrentUserId.Value);
                    if (status != null)
                        cmd.Parameters.AddWithValue("@Status", status);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadApplication(reader));
                    }
                }
            }

            ViewBag.Status = status;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            return View(new JobApplication());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(JobApplication model)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            CheckStatus(model);
            if (!ModelState.IsValid)
                return View(model);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO Applications (UserID, Company, Role, Location, AppliedDate, Status, Notes) " +
                               "VALUES (@UserID, @Company, @Role, @Location, @AppliedDate, @Status, @Notes)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserID", CurrentUserId.Value);
                    AddParameters(cmd, model);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Application added.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            JobApplication? app = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                // UserID is part of the WHERE so nobody can open someone else's row.
                string query = "SELECT AppID, Company, Role, Location, AppliedDate, Status, Notes " +
                               "FROM Applications WHERE AppID = @AppID AND UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AppID", id);
                    cmd.Parameters.AddWithValue("@UserID", CurrentUserId.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            app = ReadApplication(reader);
                    }
                }
            }

            if (app == null)
                return NotFound();

            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, JobApplication model)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            CheckStatus(model);
            if (!ModelState.IsValid)
                return View(model);

            int rows;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Applications SET Company = @Company, Role = @Role, Location = @Location, " +
                               "AppliedDate = @AppliedDate, Status = @Status, Notes = @Notes " +
                               "WHERE AppID = @AppID AND UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AppID", id);
                    cmd.Parameters.AddWithValue("@UserID", CurrentUserId.Value);
                    AddParameters(cmd, model);
                    rows = cmd.ExecuteNonQuery();
                }
            }

            if (rows == 0)
                return NotFound();

            TempData["Message"] = "Application updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "DELETE FROM Applications WHERE AppID = @AppID AND UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AppID", id);
                    cmd.Parameters.AddWithValue("@UserID", CurrentUserId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Application deleted.";
            return RedirectToAction("Index");
        }

        // Reads one row from the reader into a JobApplication.
        private static JobApplication ReadApplication(SqlDataReader reader)
        {
            return new JobApplication
            {
                AppID = reader.GetInt32(0),
                Company = reader.GetString(1),
                Role = reader.GetString(2),
                Location = reader.IsDBNull(3) ? null : reader.GetString(3),
                AppliedDate = reader.GetDateTime(4),
                Status = reader.GetString(5),
                Notes = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
        }

        // The same fields are used by both INSERT and UPDATE.
        private static void AddParameters(SqlCommand cmd, JobApplication model)
        {
            cmd.Parameters.AddWithValue("@Company", model.Company);
            cmd.Parameters.AddWithValue("@Role", model.Role);
            cmd.Parameters.AddWithValue("@Location", (object?)model.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppliedDate", model.AppliedDate.Date);
            cmd.Parameters.AddWithValue("@Status", model.Status);
            cmd.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
        }

        private void CheckStatus(JobApplication model)
        {
            if (!JobApplication.Statuses.Contains(model.Status))
                ModelState.AddModelError("Status", "Pick a valid status.");
        }
    }
}
