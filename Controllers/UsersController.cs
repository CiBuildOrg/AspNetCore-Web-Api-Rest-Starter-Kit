using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using SampleApi.Repository;
using SampleApi.Models;
using SampleApi.Policies;
using SampleApi.Filters;
using SampleApi.ViewModels;

namespace SampleApi.Controllers
{
    [Route("api/v1/[controller]")]
    public class UsersController : BaseController<ApplicationUser>
    {
        public UsersController(IRepository repository) : base(repository)
        {

        }

        [PaginationHeadersFilter]
        [HttpGet]
        [Authorize(Policy = PermissionClaims.ReadUser)]
        public async Task<IActionResult> GetList([FromQuery] int page = firstPage, [FromQuery] int limit = minLimit)
        {
            page = (page < firstPage) ? firstPage : page;
            limit = (limit < minLimit) ? minLimit : limit;
            limit = (limit > maxLimit) ? maxLimit : limit;
            int skip = (page - 1) * limit;
            int count = await repository.GetCountAsync<ApplicationUser>(null);
            HttpContext.Items["count"] = count.ToString();
            HttpContext.Items["page"] = page.ToString();
            HttpContext.Items["limit"] = limit.ToString();
            IEnumerable<ApplicationUser> userList = await repository.GetAllAsync<ApplicationUser>(null, null, skip, limit);
            return Json(userList);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = PermissionClaims.ReadUser)]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            ApplicationUser user = await repository.GetByIdAsync<ApplicationUser>(id);
            if (user != null)
            {
                return Json(user);
            }
            return NotFound(new { message = "User does not exist!" });
        }

        [HttpPost]
        [Authorize(Policy = PermissionClaims.CreateUser)]
        public async Task<IActionResult> Create([FromBody] UserViewModel model)
        {
            if (await repository.GetUserManager().FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email is already in use");
                var modelErrors = new Dictionary<string, Object>();
                modelErrors["message"] = "The request has validation errors.";
                modelErrors["errors"] = new SerializableError(ModelState);
                return BadRequest(ModelState);
            }
            var role = await repository.GetRoleManager().FindByIdAsync(model.RoleId);
            if (role != null)
            {
                ModelState.AddModelError("Role", "Role does not exist");
                var modelErrors = new Dictionary<string, Object>();
                modelErrors["message"] = "The request has validation errors.";
                modelErrors["errors"] = new SerializableError(ModelState);
                return BadRequest(ModelState);
            }
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                TenantId = 2
            };
            var userCreationResult = await repository.GetUserManager().CreateAsync(user, model.Password);
            if (!userCreationResult.Succeeded)
            {
                foreach (var error in userCreationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            await repository.GetUserManager().AddToRoleAsync(user, "admin");
            repository.Create(user);
            await repository.SaveAsync();
            return Created($"/api/v1/users/{user.Id}", new { message = "User was created successfully!" });
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = PermissionClaims.UpdateUser)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ApplicationUser updateduser)
        {
            ApplicationUser user = repository.GetById<ApplicationUser>(id);
            if (user == null)
            {
                return NotFound(new { message = "User does not exist!" });
            }
            repository.Update(user, updateduser);
            await repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionClaims.DeleteUser)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            ApplicationUser user = repository.GetById<ApplicationUser>(id);
            if (user == null)
            {
                return NotFound(new { message = "User does not exist!" });
            }
            repository.Delete(user);
            await repository.SaveAsync();
            return NoContent();
        }
    }


}