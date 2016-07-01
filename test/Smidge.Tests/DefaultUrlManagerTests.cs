﻿using System;
using Xunit;
using Smidge.CompositeFiles;
using Smidge;
using Moq;
using System.Collections.Generic;
using Smidge.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smidge.Options;

namespace Smidge.Tests
{
    public class DefaultUrlManagerTests
    {
        [Theory]
        [InlineData("~/{Path}/{Name}.{Ext}.v{Version}", "abc", "hello", ".js", "123", "~/abc/hello.js.v123")]
        [InlineData("~/{Name}/{Path}/{Version}.{Ext}", "abc", "hello", ".js", "123", "~/hello/abc/123.js")]
        [InlineData("~/{Version}.{Name}.{Ext}.{Path}", "abc", "hello", ".js", "123", "~/123.hello.js.abc")]
        [InlineData("   ~/{Version}/{Name}/{Ext}/{Path}  ", "abc", "hello", ".js", "123", "~/123/hello/js/abc")]
        [InlineData("~/Extra{Version}/{Name}/{Ext}/{Path}Stuff", "abc", "hello", ".js", "123", "~/Extra123/hello/js/abcStuff")]
        public void Parse_Url_Pattern(string pattern, string path, string name, string ext, string version, string expected)
        {
            var result = DefaultUrlManager.BuildUrl(pattern, path, name, ext, version);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("~/{Path}/{Name}/{Ext}")]
        [InlineData("~/{Path}/{Name}/{Version}")]
        [InlineData("~/{Path}/{Ext}/{Version}")]
        [InlineData("~/{Name}/{Ext}/{Version}")]
        [InlineData("/{Path}/{Name}/{Ext}.v{Version}")]
        [InlineData("~{Path}/{Name}/{Ext}.v{Version}")]
        [InlineData("~/{Path}{Name}/{Ext}.v{Version}")]
        [InlineData("~/{Path}/{Name}{Ext}.v{Version}")]
        [InlineData("~/{Path}/{Name}/{Ext}{Version}")]
        public void Validates_Url_Pattern(string pattern)
        {
            Assert.Throws<FormatException>(() => DefaultUrlManager.BuildUrl(pattern, "abc", "hello", ".js", "123"));            
        }

        [Fact]
        public void Parse_Path()
        {
            var path = "c61531b5.2512be3b.bb1214f7.a21bd1fd.js.v1";
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg" } };
            var manager = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                Mock.Of<IHasher>(),
                Mock.Of<IRequestHelper>());

            var result = manager.ParsePath(path);

            Assert.Equal("1", result.Version);
            Assert.Equal(4, result.Names.Count());
            Assert.Equal(WebFileType.Js, result.WebType);
        }

        [Fact]
        public void Make_Bundle_Url()
        {
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("blah");
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { BundleFilePath = "sg" } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object,
                urlHelper);

            var url = creator.GetUrl("my-bundle", ".js");

            Assert.Equal("/sg/my-bundle.js.v1", url);
        }

        [Fact]
        public void Make_Composite_Url()
        {
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 100 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object,
                urlHelper);

            var url = creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js");

            Assert.Equal(1, url.Count());
            Assert.Equal("/sg/Test1.Test2.js.v1", url.First().Url);
            Assert.Equal("test1.test2", url.First().Key);
        }

        [Fact]
        public void Make_Composite_Url_Splits()
        {
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 14 + 10 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object,
                urlHelper);

            var url = creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js"), new JavaScriptFile("Test2.js") }, ".js");

            Assert.Equal(2, url.Count());
            Assert.Equal("/sg/Test1.js.v1", url.ElementAt(0).Url);
            Assert.Equal("test1", url.ElementAt(0).Key);
            Assert.Equal("/sg/Test2.js.v1", url.ElementAt(1).Url);
            Assert.Equal("test2", url.ElementAt(1).Key);
        }

        [Fact]
        public void Throws_When_Single_Dependency_Too_Long()
        {
            var urlHelper = new RequestHelper("http", new PathString(), new HeaderDictionary());
            var hasher = new Mock<IHasher>();
            hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns((string s) => s.ToLower());
            var options = new SmidgeOptions { UrlOptions = new UrlManagerOptions { CompositeFilePath = "sg", MaxUrlLength = 10 } };
            var creator = new DefaultUrlManager(
                Mock.Of<IOptions<SmidgeOptions>>(x => x.Value == options),
                Mock.Of<ISmidgeConfig>(x => x.Version == "1"),
                hasher.Object,
                urlHelper);

            Assert.Throws<InvalidOperationException>(() => creator.GetUrls(new List<IWebFile> { new JavaScriptFile("Test1.js") }, ".js"));

        }
    }
}