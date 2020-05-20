﻿
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Reflection;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Diagnostics;
using Umbraco.Core.Hosting;
using Umbraco.Core.Logging;
using Umbraco.Net;
using Umbraco.Core.Persistence;
using Umbraco.Core.Runtime;
using Umbraco.Tests.Common;
using Umbraco.Web.Common.AspNetCore;
using IHostingEnvironment = Umbraco.Core.Hosting.IHostingEnvironment;

namespace Umbraco.Tests.Integration.Implementations
{
    public class TestHelper : TestHelperBase
    {
        private IBackOfficeInfo _backOfficeInfo;
        private IHostingEnvironment _hostingEnvironment;
        private readonly IApplicationShutdownRegistry _hostingLifetime;
        private readonly IIpResolver _ipResolver;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tempWorkingDir;

        public TestHelper() : base(typeof(TestHelper).Assembly)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _httpContextAccessor = Mock.Of<IHttpContextAccessor>(x => x.HttpContext == httpContext);
            _ipResolver = new AspNetIpResolver(_httpContextAccessor);

            var hostEnvironment = new Mock<IWebHostEnvironment>();
            hostEnvironment.Setup(x => x.ApplicationName).Returns("UmbracoIntegrationTests");
            hostEnvironment.Setup(x => x.ContentRootPath)
                .Returns(() => Assembly.GetExecutingAssembly().GetRootDirectorySafe());
            hostEnvironment.Setup(x => x.WebRootPath).Returns(() => WorkingDirectory);
            _hostEnvironment = hostEnvironment.Object;

            _hostingLifetime = new AspNetCoreApplicationShutdownRegistry(Mock.Of<IHostApplicationLifetime>());

            Logger = new ProfilingLogger(new ConsoleLogger(new MessageTemplates()), Profiler);
        }


        public override string WorkingDirectory
        {
            get
            {
                // For Azure Devops we can only store a database in certain locations so we will need to detect if we are running
                // on a build server and if so we'll use the %temp% path.

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("System_DefaultWorkingDirectory")))
                {
                    // we are using Azure Devops!

                    if (_tempWorkingDir != null) return _tempWorkingDir;

                    var temp = Path.Combine(Environment.ExpandEnvironmentVariables("%temp%"), "UmbracoTemp");
                    Directory.CreateDirectory(temp);
                    _tempWorkingDir = temp;
                    return _tempWorkingDir;

                }
                else
                {
                    return base.WorkingDirectory;
                }
            }
        }

        public IUmbracoBootPermissionChecker UmbracoBootPermissionChecker { get; } =
            new TestUmbracoBootPermissionChecker();

        public AppCaches AppCaches { get; } = new AppCaches(NoAppCache.Instance, NoAppCache.Instance,
            new IsolatedCaches(type => NoAppCache.Instance));

        public IProfilingLogger Logger { get; private set; }

        public IProfiler Profiler { get; } = new VoidProfiler();

        public IHttpContextAccessor GetHttpContextAccessor() => _httpContextAccessor;

        public IWebHostEnvironment GetWebHostEnvironment() => _hostEnvironment;

        public override IDbProviderFactoryCreator DbProviderFactoryCreator =>
            new SqlServerDbProviderFactoryCreator(Constants.DbProviderNames.SqlServer, DbProviderFactories.GetFactory);

        public override IBulkSqlInsertProvider BulkSqlInsertProvider => new SqlServerBulkSqlInsertProvider();

        public override IMarchal Marchal { get; } = new AspNetCoreMarchal();

        public override IBackOfficeInfo GetBackOfficeInfo()
        {
            if (_backOfficeInfo == null)
                _backOfficeInfo =
                    new AspNetCoreBackOfficeInfo(SettingsForTests.GetDefaultGlobalSettings(GetUmbracoVersion()));
            return _backOfficeInfo;
        }

        public override IHostingEnvironment GetHostingEnvironment()
            => _hostingEnvironment ??= new TestHostingEnvironment(
                SettingsForTests.DefaultHostingSettings,
                _hostEnvironment);

        public override IApplicationShutdownRegistry GetHostingEnvironmentLifetime() => _hostingLifetime;

        public override IIpResolver GetIpResolver() => _ipResolver;

        /// <summary>
        /// Some test files are copied to the /bin (/bin/debug) on build, this is a utility to return their physical path based on a virtual path name
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public override string MapPathForTestFiles(string relativePath)
        {
            if (!relativePath.StartsWith("~/"))
                throw new ArgumentException("relativePath must start with '~/'", nameof(relativePath));

            var codeBase = typeof(TestHelperBase).Assembly.CodeBase;
            var uri = new Uri(codeBase);
            var path = uri.LocalPath;
            var bin = Path.GetDirectoryName(path);

            return relativePath.Replace("~/", bin + "/");
        }
    }
}