using DocumentFormat.OpenXml;
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

        public int AbsenteesCount { get; set; } = 0;
        public int RegisteredStudentsCount { get; set; } = 0;

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

            // Count the total registered students (length of student records)
            RegisteredStudentsCount = studentRecords.Count;

            // Join attendance and student records based on student_number
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

            // Find absentees by checking which students from the studentRecords are not in the attendanceLogs
            var attendedStudentNumbers = attendanceLogs.Select(a => a.student_number).Distinct();
            var allStudentNumbers = studentRecords.Select(s => s.student_number).Distinct();
            var absentees = allStudentNumbers.Except(attendedStudentNumbers).ToList();

            // Count the absentees
            AbsenteesCount = absentees.Count;
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
        public async Task<IActionResult> OnPostGenerateAttendanceReport(
            string subject_code,
            string subject_name,
            string room_assigned,
            string school_campus,
            string school_course,
            string year_section,
            string professor_name,
            string start_classes,
            string end_classes)
        {
            try
            {
                // Fetch attendance logs
                var attendanceLogUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/attendance-log-attempts";
                var attendanceResponse = await _httpClient.GetAsync(attendanceLogUrl);
                var attendanceContent = await attendanceResponse.Content.ReadAsStringAsync();
                var attendanceLogs = JsonConvert.DeserializeObject<List<AttendanceLog>>(attendanceContent) ?? new List<AttendanceLog>();

                // Fetch student records
                var studentRecordsUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records";
                var studentResponse = await _httpClient.GetAsync(studentRecordsUrl);
                var studentContent = await studentResponse.Content.ReadAsStringAsync();
                var studentRecords = JsonConvert.DeserializeObject<List<StudentRecord>>(studentContent) ?? new List<StudentRecord>();

                var attendanceRecords = (from student in studentRecords
                                         join attendance in attendanceLogs
                                         on student.student_number equals attendance.student_number into attendanceGroup
                                         from attendance in attendanceGroup.DefaultIfEmpty()
                                         select new AttendanceRecord
                                         {
                                             StudentNumber = student.student_number ?? "",
                                             LastName = student.last_name ?? "",
                                             FirstName = student.first_name ?? "",
                                             MiddleInitial = !string.IsNullOrEmpty(student.middle_name)
                                                 ? student.middle_name.Substring(0, 1).ToUpper()
                                                 : "",
                                             Course = $"{student.current_year} S.Y, {student.course}",
                                             CurrentSection = $"{student.current_section}",
                                             AttendanceDateTime = attendance?.attendance_time_in != null
                                                 ? FormatAttendanceDateTime(attendance.attendance_time_in)
                                                 : "Absent"
                                         })
                                         .GroupBy(a => a.StudentNumber)  // Group by student number
                                         .Select(group => group.OrderByDescending(a => DateTime.TryParse(a.AttendanceDateTime, out DateTime parsedDate) ? parsedDate : DateTime.MinValue).FirstOrDefault())  // Get the latest attendance record
                                         .OrderByDescending(a => DateTime.TryParse(a.AttendanceDateTime, out DateTime parsedDate) ? parsedDate : DateTime.MinValue)
                                         .ToList();

                // Generate Word document in memory
                using var memoryStream = new MemoryStream();
                using (var doc = WordprocessingDocument.Create(memoryStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Add report header
                    //body.AppendChild(new Paragraph(new Run(new Text("Student Attendance Report"))
                    //{
                    //    RunProperties = new RunProperties(new Bold(), new FontSize { Val = "28" })
                    //}));

                    var titleParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center } // Center the text
                        ),
                        new Run(
                            new RunProperties(new Bold(), new FontSize { Val = "28" }), // Bold and font size
                            new Text($"GENERATED ATTENDANCE REPORT FOR SECTION {year_section}") // Include year_section dynamically
                        )
                    );
                    body.AppendChild(titleParagraph);


                    body.AppendChild(new Paragraph(new Run(new Text($"Subject Code: {subject_code} - {subject_name}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Course: {school_course}, Section: {year_section}"))));
                    // body.AppendChild(new Paragraph(new Run(new Text($"Professor: {professor_name}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Class Schedule: {start_classes} - {end_classes}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Room: {room_assigned}, Campus: {school_campus}"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Date Generated: {DateTime.Now:MMMM dd, yyyy}"))));
                    body.AppendChild(new Paragraph(new Run(new Text("")))); // Add spacing

                    // Create attendance table
                    var attendanceTable = new Table();
                    attendanceTable.AppendChild(new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 12 },
                            new BottomBorder { Val = BorderValues.Single, Size = 12 },
                            new LeftBorder { Val = BorderValues.Single, Size = 12 },
                            new RightBorder { Val = BorderValues.Single, Size = 12 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 12 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 12 }
                        )
                    ));

                    // Add table header row
                    var headerRow = new TableRow();
                    headerRow.Append(
                        CreateTableCell("Student Number", true),
                        CreateTableCell("Last Name", true),
                        CreateTableCell("First Name", true),
                        CreateTableCell("Middle Initial", true),
                        CreateTableCell("Course & Year", true),
                        CreateTableCell("Section", true),
                        CreateTableCell("Attendance Time-In", true)
                    );
                    attendanceTable.AppendChild(headerRow);

                    // Add student attendance records to table
                    foreach (var record in attendanceRecords)
                    {
                        var row = new TableRow();
                        row.Append(
                            CreateTableCell(record.StudentNumber),
                            CreateTableCell(record.LastName),
                            CreateTableCell(record.FirstName),
                            CreateTableCell(record.MiddleInitial),
                            CreateTableCell(record.Course),
                            CreateTableCell(record.CurrentSection),
                            CreateTableCell(record.AttendanceDateTime)
                        );
                        attendanceTable.AppendChild(row);
                    }

                    // Append table to the document
                    body.AppendChild(attendanceTable);

                    // Add Note Section
                    body.AppendChild(new Paragraph(new Run(new Text("")))); // Add spacing
                    body.AppendChild(new Paragraph(new Run(new Text("Note: These are all registered students inside the attendance management system. If there are known circumstances, please consider them to register inside the web portal."))));

                    // Add Professor Signature Section
                    body.AppendChild(new Paragraph(new Run(new Text("")))); // Add spacing
                    body.AppendChild(new Paragraph(new Run(new Text("___________________________________________"))));
                    body.AppendChild(new Paragraph(new Run(new Text($"Professor: {professor_name} (Full Signature Over Printed Name)"))));

                    mainPart.Document.Save();
                }

                memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                // Return the Word document as a file download
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "AttendanceReport.docx");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating attendance report: {ex.Message}");
                return StatusCode(500, "An error occurred while generating the report.");
            }
        }

        // Helper method for creating table cells
        private TableCell CreateTableCell(string text, bool isHeader = false)
        {
            var runProperties = new RunProperties();
            if (isHeader)
            {
                runProperties.Bold = new Bold();
                runProperties.FontSize = new FontSize { Val = "22" };
            }

            var paragraph = new Paragraph(new Run(new Text(text)));
            return new TableCell(paragraph);
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