// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4;

namespace IDP
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResource(
                    "roles",
                    "Your role(s)",
                    new List<string>
                    {
                        "role" // List of Claims which needs to be returned when application asks for this scope.
                    }),
                new IdentityResource("country", "The country you are living in", new List<string>{"country"}), 
                new IdentityResource("subscriptionlevel", "Your subscription level", new List<string>{"subscriptionlevel"}), 
            };

        public static IEnumerable<ApiResource> Apis =>
            new ApiResource[]
            {
                new ApiResource("imagegalleryapi",
                    "Image Gallery API",
                    new List<string> {"role"})
                {
                    ApiSecrets = new List<Secret>{ new Secret("apisecret".Sha256()) }
                },
            };

        public static IEnumerable<Client> Clients =>
            new[]
            {
                new Client
                {
                    AccessTokenType = AccessTokenType.Reference,
                    // IdentityTokenLifetime = by default 5 mins in [sec]
                    //AuthorizationCodeLifetime = by default 5 mins in [sec]
                    AccessTokenLifetime =  120, //by default 1 h in [sec]
                    AllowOfflineAccess = true,
                    //RefreshTokenExpiration = TokenExpiration.Sliding,
                    // SlidingRefreshTokenLifetime = 
                    // AbsoluteRefreshTokenLifetime = 
                    UpdateAccessTokenClaimsOnRefresh = true,
                    ClientName = "Image Gallery",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RedirectUris = new List<string>
                    {
                        "https://localhost:44389/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "https://localhost:44389/signout-callback-oidc"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address,
                        "roles", // allowed scope
                        "imagegalleryapi",
                        "country",
                        "subscriptionlevel"
                    },
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    }
                }
            };

    }
}