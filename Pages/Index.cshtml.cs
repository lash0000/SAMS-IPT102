using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace SAMS_IPT102.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly HttpClient _httpClient;

        public IndexModel(ILogger<IndexModel> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public List<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        public async Task OnGet()
        {
            var attendanceLogUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/attendance-log-attempts";
            var studentRecordsUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records/";

            // Fetch attendance logs
            var attendanceResponse = await _httpClient.GetAsync(attendanceLogUrl);
            var attendanceContent = await attendanceResponse.Content.ReadAsStringAsync();
            var attendanceLogs = JsonConvert.DeserializeObject<List<AttendanceLog>>(attendanceContent) ?? new List<AttendanceLog>();

            // Fetch student records
            var studentResponse = await _httpClient.GetAsync(studentRecordsUrl);
            var studentContent = await studentResponse.Content.ReadAsStringAsync();
            var studentRecords = JsonConvert.DeserializeObject<List<StudentRecord>>(studentContent) ?? new List<StudentRecord>();

            // Join records based on student_number
            AttendanceRecords = (from attendance in attendanceLogs
                                 join student in studentRecords on attendance.student_number equals student.student_number
                                 select new AttendanceRecord
                                 {
                                     StudentNumber = attendance.student_number ?? "",
                                     LastName = student.last_name ?? "",
                                     FirstName = student.first_name ?? "",
                                     MiddleInitial = !string.IsNullOrEmpty(student.middle_name)
                                         ? student.middle_name.Substring(0, 1).ToUpper()
                                         : "", // Extract middle initial from middle_name
                                     RFIDNumber = student.rfid_number ?? "",
                                     Course = $"{student.enrollment_year} School Year, {student.course}",
                                     AttendanceDateTime = FormatAttendanceDateTime(attendance.attendance_time_in)
                                 }).ToList();
        }

        private string FormatAttendanceDateTime(string attendanceTimeIn)
        {
            if (DateTime.TryParse(attendanceTimeIn, out DateTime attendanceDateTime))
            {
                return attendanceDateTime.ToString("dd/MM/yyyy hh:mm tt");  // Format to DD/MM/YYYY (12-Hour format)
            }
            return string.Empty;  // Return empty string if the date is invalid or null
        }
    }

    // Model classes to deserialize JSON
    public class AttendanceLog
    {
        public string student_number { get; set; } = "";
        public string attendance_time_in { get; set; } = "";
    }

    public class StudentRecord
    {
        public string student_number { get; set; } = "";
        public string last_name { get; set; } = "";
        public string middle_name { get; set; } = "";
        public string first_name { get; set; } = "";
        public string rfid_number { get; set; } = "";
        public int enrollment_year { get; set; }
        public string course { get; set; } = "";
    }

    public class AttendanceRecord
    {
        public string StudentNumber { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleInitial { get; set; } = "";
        public string RFIDNumber { get; set; } = "";
        public string Course { get; set; } = "";
        public string AttendanceDateTime { get; set; } = "";
    }
}
