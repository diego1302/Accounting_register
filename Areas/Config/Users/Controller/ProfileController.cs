using Accounting_Register_BE.Areas.Users.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_Register_BE.Areas.Config.Users.Controller
{
    public record ProfileDto(string? email, string? user_first_name, string? user_middle_name, string? user_last_name, string? phone_number);

    public record UpdateProfileDto(string? user_first_name, string? user_middle_name, string? user_last_name, string? phone_number);

    /// <summary>Extra personal details of the signed-in user (the Identity API only handles email/password).</summary>
    [Authorize]
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;

        public ProfileController(UserManager<ApplicationUser> users)
        {
            _users = users;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileDto>> Get()
        {
            var user = await _users.GetUserAsync(User);
            if (user is null) return Unauthorized();
            return ToDto(user);
        }

        [HttpPut]
        public async Task<ActionResult<ProfileDto>> Update(UpdateProfileDto model)
        {
            var user = await _users.GetUserAsync(User);
            if (user is null) return Unauthorized();

            user.user_first_name = model.user_first_name;
            user.user_middle_name = model.user_middle_name;
            user.user_last_name = model.user_last_name;
            user.PhoneNumber = model.phone_number;

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) return ValidationProblem(new ValidationProblemDetails(
                result.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())));

            return ToDto(user);
        }

        private static ProfileDto ToDto(ApplicationUser u) =>
            new(u.Email, u.user_first_name, u.user_middle_name, u.user_last_name, u.PhoneNumber);
    }
}
