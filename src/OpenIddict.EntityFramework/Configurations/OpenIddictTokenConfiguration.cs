﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Text;
using OpenIddict.EntityFramework.Models;

namespace OpenIddict.EntityFramework
{
    /// <summary>
    /// Defines a relational mapping for the Token entity.
    /// </summary>
    /// <typeparam name="TToken">The type of the Token entity.</typeparam>
    /// <typeparam name="TApplication">The type of the Application entity.</typeparam>
    /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
    /// <typeparam name="TKey">The type of the Key entity.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class OpenIddictTokenConfiguration<TToken, TApplication, TAuthorization, TKey> : EntityTypeConfiguration<TToken>
        where TToken : OpenIddictToken<TKey, TApplication, TAuthorization>
        where TApplication : OpenIddictApplication<TKey, TAuthorization, TToken>
        where TAuthorization : OpenIddictAuthorization<TKey, TApplication, TToken>
        where TKey : IEquatable<TKey>
    {
        public OpenIddictTokenConfiguration()
        {
            // Note: unlike Entity Framework Core 1.x/2.x, Entity Framework 6.x
            // always throws an exception when using generic types as entity types.
            // To ensure a better exception is thrown, a manual check is made here.
            if (typeof(TToken).IsGenericType)
            {
                throw new InvalidOperationException(new StringBuilder()
                    .AppendLine("The token entity cannot be a generic type.")
                    .Append("Consider creating a non-generic derived class.")
                    .ToString());
            }

            // Warning: optional foreign keys MUST NOT be added as CLR properties because
            // Entity Framework would throw an exception due to the TKey generic parameter
            // being non-nullable when using value types like short, int, long or Guid.

            HasKey(token => token.Id);

            Property(token => token.ConcurrencyToken)
                .IsConcurrencyToken();

            Property(token => token.ReferenceId)
                .HasMaxLength(450)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute()));

            Property(token => token.Subject)
                .IsRequired();

            Property(token => token.Type)
                .IsRequired();

            ToTable("OpenIddictTokens");
        }
    }
}
