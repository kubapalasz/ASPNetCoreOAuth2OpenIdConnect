using System;
using System.Linq;
using System.Threading.Tasks;
using ImageGallery.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ImageGallery.API.Authorization
{
    public class MustOwnImageHandler : AuthorizationHandler<MustOwnImageRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGalleryRepository _galleryRepository;

        public MustOwnImageHandler(IHttpContextAccessor httpContextAccessor,
            IGalleryRepository galleryRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _galleryRepository = galleryRepository;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
            MustOwnImageRequirement requirement)
        {
            var imageId = _httpContextAccessor.HttpContext.GetRouteValue("id").ToString();

            if (!Guid.TryParse(imageId, out Guid imageIdGuid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var ownerId = context.User.Claims.FirstOrDefault(x => x.Type == "sub").Value;

            if (!_galleryRepository.IsImageOwner(imageIdGuid, ownerId))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // All is fine!
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
