// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
            var idpDbConnectionString = "Server=(localdb)\\mssqllocaldb;Database=IDPDataDB;Trusted_Connection=True;";

            IdentityModelEventSource.ShowPII = true;

            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer()
                //.AddInMemoryIdentityResources(Config.Ids)
                //.AddInMemoryApiResources(Config.Apis)
                //.AddInMemoryClients(Config.Clients)
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

            //builder.AddDeveloperSigningCredential();
            builder.AddSigningCredential(LoadCertificateFromStore());

            // Verify https://localhost:44317/.well-known/openid-configuration/jwks
            // "kid": "84E0E23405FF068047623BDA4BB3DA1B4BE0664B",

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            builder.AddConfigurationStore(options =>
            {
                options.ConfigureDbContext =
                    builder => builder.UseSqlServer(idpDbConnectionString,
                        options => { options.MigrationsAssembly(migrationsAssembly); });
            });


            // PackageManager > Add-Migration -name InitialIdentityServerConfigurationDBMigration -context ConfigurationDBContext
            // Error > Your startup project 'IDP' doesn't reference Microsoft.EntityFrameworkCore.Design.
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            InitializeDatabae(app);

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
            string thumbPrint = "84e0e23405ff068047623bda4bb3da1b4be0664b";

            using (var store = new X509Store(StoreName.My,StoreLocation.LocalMachine))
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

        private void InitializeDatabae(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var id in Config.Ids)
                    {
                        context.IdentityResources.Add(id.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var api in Config.Apis)
                    {
                        context.ApiResources.Add(api.ToEntity());
                    }
                    context.SaveChanges();
                }

            }
        }
    }
}
