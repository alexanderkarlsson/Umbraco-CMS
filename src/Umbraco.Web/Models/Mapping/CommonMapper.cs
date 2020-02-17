﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models;
using Umbraco.Core.Models.ContentEditing;
using Umbraco.Core.Models.Identity;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;
using Umbraco.Web.ContentApps;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Trees;
using UserProfile = Umbraco.Web.Models.ContentEditing.UserProfile;

namespace Umbraco.Web.Models.Mapping
{
    public class CommonMapper
    {
        private readonly IUserService _userService;
        private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
        private readonly ContentAppFactoryCollection _contentAppDefinitions;
        private readonly ILocalizedTextService _localizedTextService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public CommonMapper(IUserService userService, IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            ContentAppFactoryCollection contentAppDefinitions, ILocalizedTextService localizedTextService, IHttpContextAccessor httpContextAccessor, ICurrentUserAccessor currentUserAccessor)
        {
            _userService = userService;
            _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
            _contentAppDefinitions = contentAppDefinitions;
            _localizedTextService = localizedTextService;
            _httpContextAccessor = httpContextAccessor;
            _currentUserAccessor = currentUserAccessor;
        }

        public UserProfile GetOwner(IContentBase source, MapperContext context)
        {
            var profile = source.GetCreatorProfile(_userService);
            return profile == null ? null : context.Map<IProfile, UserProfile>(profile);
        }

        public UserProfile GetCreator(IContent source, MapperContext context)
        {
            var profile = source.GetWriterProfile(_userService);
            return profile == null ? null : context.Map<IProfile, UserProfile>(profile);
        }

        public ContentTypeBasic GetContentType(IContentBase source, MapperContext context)
        {

            var user = _currentUserAccessor.TryGetCurrentUser();
            if (user?.AllowedSections.Any(x => x.Equals(Constants.Applications.Settings)) ?? false)
            {
                var contentType = _contentTypeBaseServiceProvider.GetContentTypeOf(source);
                var contentTypeBasic = context.Map<IContentTypeComposition, ContentTypeBasic>(contentType);

                return contentTypeBasic;
            }
            //no access
            return null;
        }

        public string GetTreeNodeUrl<TController>(IContentBase source)
            where TController : ContentTreeControllerBase
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var urlHelper = new UrlHelper(httpContext.Request.RequestContext);
            return urlHelper.GetUmbracoApiService<TController>(controller => controller.GetTreeNode(source.Key.ToString("N"), null));
        }

        public string GetMemberTreeNodeUrl(IContentBase source)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var urlHelper = new UrlHelper(httpContext.Request.RequestContext);
            return urlHelper.GetUmbracoApiService<MemberTreeController>(controller => controller.GetTreeNode(source.Key.ToString("N"), null));
        }

        public IEnumerable<ContentApp> GetContentApps(IContentBase source)
        {
            var apps = _contentAppDefinitions.GetContentAppsFor(source).ToArray();

            // localize content app names
            foreach (var app in apps)
            {
                var localizedAppName = _localizedTextService.Localize($"apps/{app.Alias}");
                if (localizedAppName.Equals($"[{app.Alias}]", StringComparison.OrdinalIgnoreCase) == false)
                {
                    app.Name = localizedAppName;
                }
            }

            return apps;
        }
    }
}
