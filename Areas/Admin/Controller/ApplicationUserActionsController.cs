using Accounting_Register_BE.Areas.Config.Users.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_Register_BE.Areas.Admin.Controllers
{
    [ApiController] [AllowAnonymous]
    [Area("user")]
    public class ApplicationUserActionsController : AreasControllerBase
    {
        public ApplicationUserActionsController(ApplicationDBContext context,
            RoleManager<IdentityRole> rolesManager, UserManager<ApplicationUser> userManager) : base(context, rolesManager, userManager)
        {
            
        }
        
        [HttpPost] [Route("/[area]/[action]")]
        public Task RegisterApi(ApplicationUser user)
        {
            return Task.CompletedTask;


        }


    }
}
