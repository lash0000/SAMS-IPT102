using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMS_IPT102.Services;

namespace SAMS_IPT102.Pages
{
    public class ClearAttendanceLogsModel : PageModel
    {
        private readonly DynamoDbService _dynamoDbService;

        public ClearAttendanceLogsModel(DynamoDbService dynamoDbService)
        {
            _dynamoDbService = dynamoDbService;
        }

        public async Task<IActionResult> OnPostClearAttendanceLogsAsync()
        {
            await _dynamoDbService.DeleteAllItemsAsync();
            return RedirectToPage("Success"); // Redirect to a success page after deletion
        }
    }
}
