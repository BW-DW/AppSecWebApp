using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWorms.Pages
{
    [Authorize(Policy = "MustBelongToHRDepartment", AuthenticationSchemes = "MyCookieAuth")]
    public class HumanResourceModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
