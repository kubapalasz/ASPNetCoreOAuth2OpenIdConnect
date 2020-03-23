using ImageGallery.Client.ViewModels;
using ImageGallery.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Controllers
{ 
    [Authorize]
    public class GalleryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GalleryController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? 
                throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<IActionResult> Index()
        {
            await WriteOutIndentityInformation();

            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/images/");
            
            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    return View(new GalleryIndexViewModel(
                        await JsonSerializer.DeserializeAsync<List<Image>>(responseStream)));
                }
            }

            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("AccessDenied", "Authorization");
            }

            throw new InvalidOperationException("Probem accessing API");
        }

        public async Task<IActionResult> EditImage(Guid id)
        {

            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/images/{id}");

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            { 
                var deserializedImage = await JsonSerializer.DeserializeAsync<Image>(responseStream);

                var editImageViewModel = new EditImageViewModel()
                {
                    Id = deserializedImage.Id,
                    Title = deserializedImage.Title
                };

                return View(editImageViewModel);
            }     
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(EditImageViewModel editImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForUpdate instance
            var imageForUpdate = new ImageForUpdate() { 
                Title = editImageViewModel.Title };

            // serialize it
            var serializedImageForUpdate = JsonSerializer.Serialize(imageForUpdate);

            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/images/{editImageViewModel.Id}");

            request.Content = new StringContent(
                serializedImageForUpdate,
                System.Text.Encoding.Unicode,
                "application/json");
            
            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return RedirectToAction("Index");       
        }

        public async Task<IActionResult> DeleteImage(Guid id)
        {
            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/images/{id}");

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "PayingUser")]
        public IActionResult AddImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PayingUser")]
        public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForCreation instance
            var imageForCreation = new ImageForCreation()
            { Title = addImageViewModel.Title };

            // take the first (only) file in the Files list
            var imageFile = addImageViewModel.Files.First();

            if (imageFile.Length > 0)
            {
                using (var fileStream = imageFile.OpenReadStream())
                using (var ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    imageForCreation.Bytes = ms.ToArray();
                }
            }

            // serialize it
            var serializedImageForCreation = JsonSerializer.Serialize(imageForCreation);  
            
            var httpClient = _httpClientFactory.CreateClient("APIClient");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/images");

            request.Content = new StringContent(
                serializedImageForCreation,
                System.Text.Encoding.Unicode,
                "application/json");

            var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return RedirectToAction("Index");
        }

        private async Task WriteOutIndentityInformation()
        {
            // Get the saved identity token
            var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            // Write it out to console
            Debug.WriteLine($"Identity tokent {identityToken}");

            foreach (var userClaim in User.Claims)
            {
                Debug.WriteLine($"Claim type={userClaim.Type} value={userClaim.Value}");
            }
        }

        public async Task Logout()
        {
            var idpClient = _httpClientFactory.CreateClient("IDPClient");
            var discoveryDocument = await idpClient.GetDiscoveryDocumentAsync();

            if (discoveryDocument.IsError)
            {
                throw new Exception("Failed to get Discovery document  for revoking token access");
            }

            var accessTokenRevokeResponse = await idpClient.RevokeTokenAsync(new TokenRevocationRequest
            {
                Address = discoveryDocument.RevocationEndpoint,
                ClientId = "imagegalleryclient",
                ClientSecret = "secret",
                Token = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)
            });

            if (accessTokenRevokeResponse.IsError)
            {
                throw new Exception("Failed to get Discovery document  for revoking token access");
            }

            var refreshTokenRevokeResponse = await idpClient.RevokeTokenAsync(new TokenRevocationRequest
            {
                Address = discoveryDocument.RevocationEndpoint,
                ClientId = "imagegalleryclient",
                ClientSecret = "secret",
                Token = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken)
            });

            if (refreshTokenRevokeResponse.IsError)
            {
                throw new Exception("Failed to get Discovery document  for revoking token access");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        }

        [Authorize(Policy = "CanOrderFrame")]
        public async Task<IActionResult> OrderFrame()
        {
            var idpClient = _httpClientFactory.CreateClient("IDPClient");

            var metaDataResponse = await idpClient.GetDiscoveryDocumentAsync();

            if (metaDataResponse.IsError)
            {
                throw new Exception("Failed to get medata data from IDP", metaDataResponse.Exception);
            }

            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            var userInforReponse = await idpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = metaDataResponse.UserInfoEndpoint,
                Token = accessToken
            });

            if (userInforReponse.IsError)
            {
                throw new Exception("Failed to get user info from IDP", userInforReponse.Exception);
            }

            var address = userInforReponse.Claims.FirstOrDefault(c => c.Type == "address")?.Value;

            return View(new OrderFrameViewModel(address));
        }
    }
}
