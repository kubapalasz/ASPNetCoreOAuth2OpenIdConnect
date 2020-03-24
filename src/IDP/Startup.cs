// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;

namespace IDP
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer()
                .AddInMemoryIdentityResources(Config.Ids)
                .AddInMemoryApiResources(Config.Apis)
                .AddInMemoryClients(Config.Clients)
                .AddTestUsers(TestUsers.Users);

            // not recommended for production - you need to store your key material somewhere secure

            // Problematic when using LoadBalancers each of servers behind LB will get different sign in credentials
            // ApplicationPool restart will cause signing with new credentials
            // We need signing material (public & private key)
            // - raw RSA Key 
            // - signing certificate

            // SU PowerShell > New-SelfSignedCertificate -Subject "CN=IDPServerSigningCert" -CertStoreLocation "cert:\LocalMachine\My"
            // Manage User Ceritificates > Certificate > Details
            // THumbprint = 84e0e23405ff068047623bda4bb3da1b4be0664b

            // Finally >  New-SelfSignedCertificate -Subject "CN=IDPServerSigningCert" -CertStoreLocation "cert:\CurrentUser\My"

            //builder.AddDeveloperSigningCredential();
            builder.AddSigningCredential(LoadCertificateFromStore());

            // Verify https://localhost:44317/.well-known/openid-configuration/jwks
            // "kid": "84E0E23405FF068047623BDA4BB3DA1B4BE0664B",
            // "kid": "00B616B1F32F66935D37E273A6665924CB142DD4",
            // "kid": "fc77a753103a078211c4f8bbf9049785303c3a46",
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private X509Certificate2 LoadCertificateFromStore()
        {
            string thumbPrint = "fc77a753103a078211c4f8bbf9049785303c3a46";

            using (var store = new X509Store(StoreName.My,StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);
                if (certCollection.Count == 0)
                {
                    throw new Exception("No cert found in store");
                }

                return certCollection[0];
            }
        }
    }
}
