using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TCBackend.Services.IServices;

namespace TCBackend.Authorization
{
    

    public class PermissionAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationHandler(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IAuthorizationRequirement requirement)
        {
            var endpoint = context.Resource as HttpContext;
            if (endpoint == null)
            {
                return;
            }

            var permissionAttribute = endpoint.GetEndpoint()?.Metadata.GetMetadata<HasPermissionAttribute>();
            if (permissionAttribute == null)
            {
                // If the endpoint doesn't have the attribute, we don't need to do anything.
                // But for this setup, we will assume all authorized endpoints will have it.
                // For a protected endpoint without this attribute, we might want to fail it.
                return;
            }

            // Get the user's ID from the claims.
            // NOTE: This assumes you have authentication set up (e.g., JWT)
            // and the user's ID is stored in the 'sub' or 'NameIdentifier' claim.
            var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                context.Fail(); // User ID is not present or invalid
                return;
            }

            // Check the permission using our service
            if (await _permissionService.HasPermissionAsync(userId, permissionAttribute.Permission))
            {
                context.Succeed(requirement); // User is authorized!
            }
            else
            {
                context.Fail(); // User is not authorized
            }
        }
    }
}
