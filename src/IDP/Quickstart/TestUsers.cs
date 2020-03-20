// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace IDP
{
    public class TestUsers
    {
        public static List<TestUser> Users = new List<TestUser>
        {
            new TestUser{SubjectId = "818727", Username = "Frank", Password = "password", 
                Claims = 
                {
                    new Claim(JwtClaimTypes.GivenName, "Frank"),
                    new Claim(JwtClaimTypes.FamilyName, "Underwood"),
                    new Claim(JwtClaimTypes.Address, "Main Road 1"),
                }
            },
            new TestUser{SubjectId = "88421113", Username = "Claire", Password = "password", 
                Claims = 
                {
                    new Claim(JwtClaimTypes.GivenName, "Claire"),
                    new Claim(JwtClaimTypes.FamilyName, "Underwood"),
                    new Claim(JwtClaimTypes.Address, "Big Street 2"),
                }
            }
        };
    }
}