﻿namespace Seed.Data.EF
{
    using Seed.Data.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Helper class to seed sample data into a <see cref="WebApiCoreSeedContext"/>
    /// </summary>
    public static class DatabaseSeed
    {
        /// <summary>
        /// Initializes a <see cref="WebApiCoreSeedContext"/> with sample Data
        /// </summary>
        /// <param name="dbContext">Context to be initialized with sample data</param>
        public static void Initialize(WebApiCoreSeedContext dbContext)
        {
            dbContext.Database.EnsureCreated();

            if (!dbContext.Users.Any())
            { 
                var users = new[]
                {
                    new User { CreatedOn = DateTime.Now, Email = "noreply@nextonlabs.com", FirstName = "John", LastName = "Doe", UserName = "JohnDoe" },
                };

                dbContext.Users.AddRange(users);
            }

            dbContext.SaveChanges();
        }
    }
}
