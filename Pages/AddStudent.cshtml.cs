using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SAMS_IPT102.Pages
{
    public class AddStudentModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AddStudentModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Bind the form fields to the NewStudent object
        [BindProperty]
        public Student NewStudent { get; set; }

        // Handle POST request for submitting the student record
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Return to the page with validation errors
                return Page();
            }

            // Set the current date for the student registration
            NewStudent.last_login = null;
            NewStudent.student_registered_date = DateTime.UtcNow.ToString("o");

            // Ensure the "is_student_new_registered" field is set to "yes"
            NewStudent.is_student_new_registered = "yes";

            // Create an HttpClient to send the request
            var client = _httpClientFactory.CreateClient();

            // Your AWS API endpoint URL
            var url = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records/";

            // Convert NewStudent object to JSON
            var jsonContent = JsonConvert.SerializeObject(NewStudent);

            // Create an HttpContent object from the JSON string
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send POST request to the API endpoint
            var response = await client.PostAsync(url, content);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Handle success (you can redirect or show a success message)
                TempData["SuccessMessage"] = "Student record added successfully.";
                return RedirectToPage("/Index"); // Redirect to a success page
            }
            else
            {
                // Handle failure (show error message)
                TempData["ErrorMessage"] = "Failed to add student record. Please try again.";
                return Page();
            }
        }
    }

    // Student model class to bind form data
    public class Student
    {
        public string department { get; set; }
        public string rfid_number { get; set; }
        public int enrollment_year { get; set; }
        public DateTime date_of_birth { get; set; }
        public string last_login { get; set; }
        public string gender { get; set; }
        public string is_student_new_registered { get; set; }
        public string student_campus { get; set; }
        public string last_name { get; set; }
        public string course { get; set; }
        public string current_section { get; set; }
        public string current_year { get; set; }
        public string first_name { get; set; }
        public string phone_number { get; set; }
        public string student_number { get; set; }
        public string student_type { get; set; }
        public string student_registered_date { get; set; }
        public string middle_name { get; set; }
    }
}
