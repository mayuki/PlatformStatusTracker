using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.Infrastracture
{
    public class LetsEncryptWellKnownContentTypeProvider : IContentTypeProvider
    {
        private IContentTypeProvider _baseProvider;

        public LetsEncryptWellKnownContentTypeProvider()
            : this(new FileExtensionContentTypeProvider())
        { }

        public LetsEncryptWellKnownContentTypeProvider(IContentTypeProvider baseProvider)
        {
            _baseProvider = baseProvider;
        }

        public bool TryGetContentType(string subpath, [NotNullWhen(true)]out string? contentType)
        {
            if (subpath.StartsWith("/.well-known/acme-challenge/"))
            {
                contentType = "application/octet-stream";
                return true;
            }

            return _baseProvider.TryGetContentType(subpath, out contentType);
        }
    }
}
