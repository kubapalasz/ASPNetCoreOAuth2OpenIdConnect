
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using ImageGallery.Client.HttpHandlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ImageGallery.Client
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Clear claims mapping
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddControllersWithViews()
                 .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddAuthorization(authorizationOptions =>
            {
                IdentityModelEventSource.ShowPII = true;

                authorizationOptions.AddPolicy
                (
                    "CanOrderFrame",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.RequireClaim("country", "be");
                        policyBuilder.RequireClaim("subscriptionlevel", "PayingUser");
                    }
                );
            });

            services.AddHttpContextAccessor();

            services.AddTransient<BearerTokenHandler>();

            // create an HttpClient used for accessing the API
            services
                .AddHttpClient("APIClient", client =>
                {
                    client.BaseAddress = new Uri("https://localhost:44366/");
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                })
                .AddHttpMessageHandler<BearerTokenHandler>();

            services.AddHttpClient("IDPClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44317/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookieOptions =>
                    {
                        cookieOptions.AccessDeniedPath = "/Authorization/AccessDenied";
                    })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.Authority = "https://localhost:44317/"; // IDP address
                        options.ClientId = "imagegalleryclient"; // Client id  should match one defined in IDP
                        options.ResponseType = "code"; // Grant - code flow
                        //options.UsePkce = false;
                        //options.SignedOutCallbackPath - change just when it's not default.
                        //options.Scope.Add("openid"); // < could be added but openid is added by default by OpenIdConnectOptions in Github
                        //options.Scope.Add("profile"); // < could be added but profile is added by default by OpenIdConnect
                        options.Scope.Add("address");
                        options.Scope.Add("roles");
                        options.Scope.Add("imagegalleryapi");
                        options.Scope.Add("country");
                        options.Scope.Add("subscriptionlevel");
                        options.Scope.Add("offline_access");
                        options.SaveTokens = true;

                        options.ClaimActions.MapUniqueJsonKey("role", "role");
                        options.ClaimActions.MapUniqueJsonKey("country", "country");
                        options.ClaimActions.MapUniqueJsonKey("subscriptionlevel", "subscriptionlevel");
                        
                        //options.ClaimActions.Remove("nbf"); ///<-- removes filter not-before, this claim now will show up.
                        options.ClaimActions.DeleteClaim("sid"); /// <-- removes claim, will NOT show up.
                        options.ClaimActions.DeleteClaim("idp");

                        options.ClientSecret = "secret";
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = JwtClaimTypes.GivenName,
                            RoleClaimType = JwtClaimTypes.Role
                        };

                    });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                // The default HSTS value is 30 days. You may want to change this for
                // production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}
