namespace SAMS_IPT102.Models
{
    public class AttendanceRecord
    {
        public string StudentNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string RFIDNumber { get; set; }
        public string CurrentYear { get; set; }
        public string Course { get; set; }
        public string CurrentSection { get; set; }
        public string AttendanceDateTime { get; set; }

        // New property to format AttendanceDateTime in 12-hour format
        public string FormattedAttendanceTime
        {
            get
            {
                if (DateTime.TryParse(AttendanceDateTime, out var dateTime))
                {
                    return dateTime.ToString("MM/dd/yyyy hh:mm tt");
                }
                return "Not Provided";
            }
        }
    }
}
