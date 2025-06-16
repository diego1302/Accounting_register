using Accounting_Register_BE.Areas.Config.Users.Model;
using Accounting_Register_BE.DataBase;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_Register_BE.Areas
{
    public class AreasControllerBase : Controller
    {
        private readonly ApplicationDBContext _applicationDBContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _rolesManager;
        
        public AreasControllerBase(ApplicationDBContext context,
            RoleManager<IdentityRole> rolesManager, UserManager<ApplicationUser> userManager)
        {
            _applicationDBContext = context;
            _userManager = userManager;
            _rolesManager = rolesManager;            
        }
    }
}
