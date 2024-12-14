using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using SAMS_IPT102.Services;

namespace SAMS_IPT102.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly HttpClient _httpClient;
        private readonly DynamoDbService _dynamoDbService;

        // Constructor (No changes here)
        public DashboardModel(ILogger<IndexModel> logger, HttpClient httpClient, DynamoDbService dynamoDbService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _dynamoDbService = dynamoDbService;
        }

        // List to store attendance records
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        // OnGet method to fetch attendance logs and student records
        public async Task OnGet()
        {
            var attendanceLogUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/attendance-log-attempts";
            var studentRecordsUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records/";

            // Fetch attendance logs (no changes here)
            var attendanceResponse = await _httpClient.GetAsync(attendanceLogUrl);
            var attendanceContent = await attendanceResponse.Content.ReadAsStringAsync();
            var attendanceLogs = JsonConvert.DeserializeObject<List<AttendanceLog>>(attendanceContent) ?? new List<AttendanceLog>();

            // Fetch student records (no changes here)
            var studentResponse = await _httpClient.GetAsync(studentRecordsUrl);
            var studentContent = await studentResponse.Content.ReadAsStringAsync();
            var studentRecords = JsonConvert.DeserializeObject<List<StudentRecord>>(studentContent) ?? new List<StudentRecord>();

            // Join attendance and student records based on student_number (no changes here)
            AttendanceRecords = (from attendance in attendanceLogs
                                 join student in studentRecords on attendance.student_number equals student.student_number
                                 select new AttendanceRecord
                                 {
                                     StudentNumber = attendance.student_number ?? "",
                                     LastName = student.last_name ?? "",
                                     FirstName = student.first_name ?? "",
                                     MiddleInitial = !string.IsNullOrEmpty(student.middle_name)
                                         ? student.middle_name.Substring(0, 1).ToUpper()
                                         : "",
                                     Course = $"{student.current_year} S.Y, {student.course}",
                                     CurrentSection = $"{student.current_section}",
                                     AttendanceDateTime = FormatAttendanceDateTime(attendance.attendance_time_in)
                                 })
                                 .OrderByDescending(a => DateTime.TryParse(a.AttendanceDateTime, out DateTime parsedDate) ? parsedDate : DateTime.MinValue)
                                 .ToList();
        }

        // OnPostClearAttendanceLogsAsync method to delete all attendance logs
        public async Task<IActionResult> OnPostClearAttendanceLogsAsync()
        {
            try
            {
                await _dynamoDbService.DeleteAllItemsAsync();
                _logger.LogInformation("All attendance logs have been cleared successfully.");
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to clear attendance logs: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Failed to clear attendance logs.");
                return RedirectToPage("Error");
            }
        }

        // OnPostGenerateAttendanceReport method to generate an attendance report in Word format
        [HttpPost]
        public async Task<IActionResult> OnPostGenerateAttendanceReport()
        {
            try
            {
                // Fetch student records from the API
                var client = new HttpClient();
                var response = await client.GetAsync("https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records");
                response.EnsureSuccessStatusCode();

                var studentRecordsJson = await response.Content.ReadAsStringAsync();
                var studentRecords = JsonConvert.DeserializeObject<List<StudentRecord>>(studentRecordsJson);

                // Retrieve attendance data from localStorage (mocked here, replace with actual retrieval)
                var attendanceData = new
                {
                    subject_code = "BSIT301",
                    subject_name = "Advanced Programming",
                    school_course = "BSIT",
                    year_section = "3K",
                    start_classes = "10:00",
                    end_classes = "12:00"
                };

                // Define the file path for the report
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AttendanceReport.docx");

                // Create the Word document
                using (var doc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Add a Header
                    body.Append(new Paragraph(new Run(new Text("Quezon City University")) { RunProperties = new RunProperties { Bold = new Bold() } }));
                    body.Append(new Paragraph(new Run(new Text("Generated Attendance Report for Section " + attendanceData.year_section))));
                    body.Append(new Paragraph(new Run(new Text($"Subject Code: {attendanceData.subject_code}"))));
                    body.Append(new Paragraph(new Run(new Text($"Subject Name: {attendanceData.subject_name}"))));
                    body.Append(new Paragraph(new Run(new Text($"Course: {attendanceData.school_course}"))));
                    body.Append(new Paragraph(new Run(new Text($"Date Generated: {DateTime.Now:MMMM dd, yyyy}"))));

                    // Add a Table
                    var table = new Table();

                    // Add Table Headers
                    var headerRow = new TableRow();
                    headerRow.Append(
                        new TableCell(new Paragraph(new Run(new Text("Student Number")))),
                        new TableCell(new Paragraph(new Run(new Text("Name (LN, FN, MI)")))),
                        new TableCell(new Paragraph(new Run(new Text("Year & Section")))),
                        new TableCell(new Paragraph(new Run(new Text("Time In")))),
                        new TableCell(new Paragraph(new Run(new Text("Remarks"))))
                    );
                    table.Append(headerRow);

                    // Add Data Rows from API
                    foreach (var student in studentRecords)
                    {
                        var dataRow = new TableRow();
                        dataRow.Append(
                            new TableCell(new Paragraph(new Run(new Text(student.student_number)))),
                            new TableCell(new Paragraph(new Run(new Text($"{student.last_name}, {student.first_name}, {student.middle_name?.Substring(0, 1)}.")))),
                            new TableCell(new Paragraph(new Run(new Text(student.current_section)))),
                            new TableCell(new Paragraph(new Run(new Text(attendanceData.start_classes)))), // Use actual attendance time-in if available
                            new TableCell(new Paragraph(new Run(new Text("")))) // Placeholder for remarks
                        );
                        table.Append(dataRow);
                    }

                    body.Append(table);

                    // Add Footer
                    body.Append(new Paragraph(new Run(new Text("Note: This is a system-generated report. If discrepancies are found, please verify with the registration portal."))));
                    body.Append(new Paragraph(new Run(new Text("Professor: ________________________________"))));
                }

                // Return the generated document as a downloadable file
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "AttendanceReport.docx");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to generate attendance report: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Failed to generate attendance report.");
                return Page();
            }
        }


        // Method to format the attendance datetime for the report
        private string FormatAttendanceDateTime(string attendanceTimeIn)
        {
            if (DateTime.TryParse(attendanceTimeIn, out DateTime attendanceDateTime))
            {
                TimeZoneInfo taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                DateTime taipeiDateTime = TimeZoneInfo.ConvertTime(attendanceDateTime, taipeiTimeZone);
                return taipeiDateTime.ToString("dd/MM/yyyy hh:mm tt");
            }
            return string.Empty;
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
            public string current_year { get; set; }
            public string current_section { get; set; } = "";
        }

        public class AttendanceRecord
        {
            public string StudentNumber { get; set; } = "";
            public string LastName { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string MiddleInitial { get; set; } = "";
            public string Course { get; set; } = "";
            public string CurrentSection { get; set; } = "";
            public string AttendanceDateTime { get; set; } = "";
        }

        public class GenerateReportRequest
        {
            public ReportData ReportData { get; set; }
            public List<StudentData> Students { get; set; }
        }

        public class ReportData
        {
            public string subject_code { get; set; }
            public string subject_name { get; set; }
            public string professor_name { get; set; }
            public string course { get; set; }
        }

        public class StudentData
        {
            public string student_number { get; set; }
            public string first_name { get; set; }
            public string middle_name { get; set; }
            public string last_name { get; set; }
            public string current_section { get; set; }
        }
    }
}