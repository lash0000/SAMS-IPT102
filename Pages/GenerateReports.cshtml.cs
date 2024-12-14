using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using SAMS_IPT102.Models;
using System;

namespace SAMS_IPT102.Pages
{
    public class GenerateReportsModel : PageModel
    {
        // Properties for form inputs
        [BindProperty]
        public string SubjectCode { get; set; }
        [BindProperty]
        public string YearSection { get; set; }
        [BindProperty]
        public string SubjectName { get; set; }
        [BindProperty]
        public string Room { get; set; }
        [BindProperty]
        public string TimeIn { get; set; }
        [BindProperty]
        public string TimeOut { get; set; }
        [BindProperty]
        public string ProfessorName { get; set; }

        // List to hold attendance records retrieved from the API
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        private readonly IHttpClientFactory _httpClientFactory;

        public GenerateReportsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task OnGetAsync()
        {
            // Optional: Populate default values or fetch from session if necessary
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Retrieve attendance records from the API
            await FetchAttendanceRecordsFromAPI();

            // Parse and format TimeIn and TimeOut with Taipei timezone
            DateTime timeInDateTime, timeOutDateTime;
            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

            string formattedTimeIn = DateTime.TryParse(TimeIn, out timeInDateTime)
                ? TimeZoneInfo.ConvertTime(timeInDateTime, taipeiTimeZone).ToString("dd/MM/yyyy hh:mm tt")
                : "Invalid Time";

            string formattedTimeOut = DateTime.TryParse(TimeOut, out timeOutDateTime)
                ? TimeZoneInfo.ConvertTime(timeOutDateTime, taipeiTimeZone).ToString("dd/MM/yyyy hh:mm tt")
                : "Invalid Time";

            // Generate the DOCX file
            using (var memoryStream = new MemoryStream())
            {
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(memoryStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());

                    // Title: SAMS_Generated_Attendance_Report
                    Body body = mainPart.Document.Body;
                    body.AppendChild(new Paragraph(new Run(new Text("SAMS_Generated_Attendance_Report"))));

                    // Subject Details Section
                    body.AppendChild(new Paragraph(new Run(new Text("Subject Code: " + SubjectCode))));
                    body.AppendChild(new Paragraph(new Run(new Text("Year and Section: " + YearSection))));
                    body.AppendChild(new Paragraph(new Run(new Text("Subject Name: " + SubjectName))));
                    body.AppendChild(new Paragraph(new Run(new Text("Room: " + Room))));
                    body.AppendChild(new Paragraph(new Run(new Text("Time-In: " + formattedTimeIn))));
                    body.AppendChild(new Paragraph(new Run(new Text("Time-Out: " + formattedTimeOut))));
                    body.AppendChild(new Paragraph(new Run(new Text("Professor Name: " + ProfessorName))));

                    // Attendance Records Table Section with Table Grid Design
                    Table table = new Table();

                    // Define table properties for grid styling
                    TableProperties tableProperties = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    );
                    table.AppendChild(tableProperties);

                    // Header Row
                    TableRow headerRow = new TableRow(
                        new TableCell(new Paragraph(new Run(new Text("Student Number")))),
                        new TableCell(new Paragraph(new Run(new Text("Student (LN, FN, MI)")))),
                        new TableCell(new Paragraph(new Run(new Text("RFID Number")))),
                        new TableCell(new Paragraph(new Run(new Text("Current Year & Course")))),
                        new TableCell(new Paragraph(new Run(new Text("Current Section")))),
                        new TableCell(new Paragraph(new Run(new Text("Date & Time (Time-In)"))))
                    );

                    // Style the header row
                    foreach (TableCell cell in headerRow.Elements<TableCell>())
                    {
                        cell.TableCellProperties = new TableCellProperties(
                            new Shading { Val = ShadingPatternValues.Clear, Fill = "D9D9D9" }, // Light gray background for header
                            new TableCellBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 4 },
                                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                                new RightBorder { Val = BorderValues.Single, Size = 4 }
                            )
                        );
                    }
                    table.Append(headerRow);

                    // Data Rows
                    foreach (var record in AttendanceRecords)
                    {
                        TableRow row = new TableRow(
                            new TableCell(new Paragraph(new Run(new Text(record.StudentNumber)))),
                            new TableCell(new Paragraph(new Run(new Text(record.LastName + ", " + record.FirstName + " " + record.MiddleInitial)))),
                            new TableCell(new Paragraph(new Run(new Text(record.RFIDNumber)))),
                            new TableCell(new Paragraph(new Run(new Text(record.Course)))),
                            new TableCell(new Paragraph(new Run(new Text(record.CurrentSection)))),
                            new TableCell(new Paragraph(new Run(new Text(record.AttendanceDateTime)))) // Use AttendanceDateTime for time-in
                        );

                        // Apply borders to each cell for grid design
                        foreach (TableCell cell in row.Elements<TableCell>())
                        {
                            cell.TableCellProperties = new TableCellProperties(
                                new TableCellBorders(
                                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                                    new RightBorder { Val = BorderValues.Single, Size = 4 }
                                )
                            );
                        }

                        table.Append(row);
                    }

                    body.Append(table);
                }

                // Return the document as a downloadable file
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "GeneratedAttendanceReport.docx");
            }
        }

        // Helper method to fetch attendance records from the API
        private async Task FetchAttendanceRecordsFromAPI()
        {
            // API URLs
            string studentRecordsUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/student-records/";
            string attendanceLogsUrl = "https://zmwu5nxsk7.execute-api.ap-southeast-2.amazonaws.com/dev/api/v1/attendance-log-attempts";

            using (var client = _httpClientFactory.CreateClient())
            {
                // Fetch Student Records
                var studentResponse = await client.GetStringAsync(studentRecordsUrl);
                var studentRecords = JsonConvert.DeserializeObject<List<StudentRecord>>(studentResponse) ?? new List<StudentRecord>();

                // Fetch Attendance Logs
                var attendanceResponse = await client.GetStringAsync(attendanceLogsUrl);
                var attendanceLogs = JsonConvert.DeserializeObject<List<AttendanceLog>>(attendanceResponse) ?? new List<AttendanceLog>();

                // Join Student Records with Attendance Logs
                AttendanceRecords = (from student in studentRecords
                                     join log in attendanceLogs on student.student_number equals log.student_number into logs
                                     from log in logs.DefaultIfEmpty()
                                     select new AttendanceRecord
                                     {
                                         StudentNumber = student.student_number,
                                         FirstName = student.first_name,
                                         LastName = student.last_name,
                                         MiddleInitial = string.IsNullOrEmpty(student.middle_name) ? "" : student.middle_name.Substring(0, 1),
                                         RFIDNumber = student.rfid_number,
                                         Course = student.course,
                                         CurrentSection = student.current_section,
                                         AttendanceDateTime = log?.attendance_time_in != null
                                     ? DateTime.TryParse(log.attendance_time_in, out var dateTime)
                                         ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).ToString("MM/dd/yyyy hh:mm tt") // Apply GMT +8 (Taipei Standard Time)
                                         : "Invalid Date"
                                     : "Missing"
                                     }).ToList();
            }
        }
    }

    public class StudentRecord
    {
        public string student_number { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string middle_name { get; set; }
        public string rfid_number { get; set; }
        public string course { get; set; }
        public string current_section { get; set; }
    }

    public class AttendanceLog
    {
        public string student_number { get; set; }
        public string attendance_time_in { get; set; }
    }


    public class AttendanceRecord
{
    public string StudentNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleInitial { get; set; } // Calculate this based on the middle name
    public string RFIDNumber { get; set; }
    public string Course { get; set; }
    public string CurrentSection { get; set; }
    public string AttendanceDateTime { get; set; }
}

}
