﻿using System;
using System.Collections.Generic;
using System.Linq;
using Axe.SimpleHttpMock.ServerImpl;
using Axe.SimpleHttpMock.ServerImpl.Handlers.UriTemplates;
using Xunit;

namespace Axe.SimpleHttpMock.Test
{
    public class UriTemplateFacts
    {
        const string BaseAddressWithTrailingSlash = "http://www.base.address.com/app/";
        const string BaseAddressWithoutTrailingSlash = "http://www.base.address.com/app";

        [Theory]
        [MemberData(nameof(CreateUriPathMatchingCases), BaseAddressWithoutTrailingSlash)]
        [MemberData(nameof(CreateUriPathMatchingCases), BaseAddressWithTrailingSlash)]
        public void should_match_simple_path_variable(string baseAddress, string template, string matchedUri)
        {
            Assert.True(new UriTemplate(template).IsMatch(new Uri(baseAddress), new Uri(matchedUri)));
        }

        [Theory]
        [MemberData(nameof(CreateUriPathNotMatchedCases), BaseAddressWithoutTrailingSlash)]
        [MemberData(nameof(CreateUriPathNotMatchedCases), BaseAddressWithTrailingSlash)]
        public void should_not_match_simple_path_variable(string baseAddress, string template, string notMatchedUri)
        {
            Assert.False(new UriTemplate(template).IsMatch(new Uri(baseAddress), new Uri(notMatchedUri)));
        }

        [Fact]
        public void should_not_collapse_path_segment()
        {
            Assert.False(new UriTemplate("/path").IsMatch(
                new Uri(BaseAddressWithoutTrailingSlash),
                new Uri("http://www.base.address.com/path")));
        }

        [Theory]
        [InlineData("path?name=value", BaseAddressWithoutTrailingSlash + "/path?name=value")]
        [InlineData("path?name1=value1&name2=value2", BaseAddressWithoutTrailingSlash + "/path?name1=value1&name2=value2")]
        [InlineData("path?name2=value2&name1=value1", BaseAddressWithoutTrailingSlash + "/path?name1=value1&name2=value2")]
        public void should_match_explicitly_specified_query_strings(string template, string matchedUri)
        {
            Assert.True(new UriTemplate(template).IsMatch(new Uri(BaseAddressWithoutTrailingSlash), new Uri(matchedUri)));
        }

        [Theory]
        [InlineData("path", BaseAddressWithoutTrailingSlash + "/path?name=value")]
        [InlineData("path", BaseAddressWithoutTrailingSlash + "/path?name1=value1&name2=value2")]
        [InlineData("path?name1=value1", BaseAddressWithoutTrailingSlash + "/path?name1=value1&name2=value2")]
        public void should_match_if_template_does_not_explicitly_specify_query_strings(string template, string matchedUri)
        {
            Assert.True(new UriTemplate(template).IsMatch(new Uri(BaseAddressWithoutTrailingSlash), new Uri(matchedUri)));
        }

        [Theory]
        [InlineData("path?name=value", BaseAddressWithoutTrailingSlash + "/path")]
        [InlineData("path?name=value&name1={variableName1}", BaseAddressWithoutTrailingSlash + "/path")]
        public void should_not_match_query_strings(string template, string notMatchedUri)
        {
            Assert.False(new UriTemplate(template).IsMatch(new Uri(BaseAddressWithoutTrailingSlash), new Uri(notMatchedUri)));
        }

        [Theory]
        [MemberData(nameof(CaptureVariableCases))]
        public void should_capture_path_variables(string template, string matchedUri, IEnumerable<KeyValuePair<string, object>> variables)
        {
            MatchingResult result = new UriTemplate(template).IsMatch(new Uri(BaseAddressWithoutTrailingSlash), new Uri(matchedUri));
            Assert.True(result);
            Assert.True(variables.All(v => result.Parameters[v.Key].Equals(v.Value)));
        }

        [Theory]
        [MemberData(nameof(CaptureQueryStringVariableCases))]
        public void should_capture_query_string_variables(string template, string matchedUri, IEnumerable<KeyValuePair<string, object>> variables)
        {
            MatchingResult result = new UriTemplate(template).IsMatch(new Uri(BaseAddressWithoutTrailingSlash), new Uri(matchedUri));
            Assert.True(result);
            Assert.True(variables.All(v => result.Parameters[v.Key].Equals(v.Value)));
        }

        [Theory]
        [InlineData("?name=value", BaseAddressWithoutTrailingSlash + "?name=value")]
        [InlineData("?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "?name=value")]
        [InlineData("path/subPath?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "/path/subpath?name=value")]
        [InlineData("path/subPath?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "/path/subpath/?name=value")]
        [InlineData("path/{variable_path}?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "/path/subpath/?name=value")]
        [InlineData("path/{variable_path}/subPath?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "/path/1/subpath?name=value")]
        [InlineData("path/{variable_path}/subPath/{variable_path2}?name=value&other={variable}", BaseAddressWithoutTrailingSlash + "/path/1/subpath/2?name=value")]
        public void should_match_uri(string template, string matchedUri)
        {
            Assert.True(new UriTemplate(template).IsMatch(new Uri(BaseAddressWithTrailingSlash), new Uri(matchedUri)));
        }

        public static IEnumerable<object[]> CreateUriPathMatchingCases(string baseAddress)
        {
            return new[]
            {
                new[] {baseAddress, "/", baseAddress},
                new[] {baseAddress, "", baseAddress},
                new[] {baseAddress, "/path/subpath/", RemoveTrailingSlash(baseAddress) + "/path/subpath"},
                new[] {baseAddress, "/path/{variable}", RemoveTrailingSlash(baseAddress) + "/path/subpath"},
                new[] {baseAddress, "/path/{variable}/", RemoveTrailingSlash(baseAddress) + "/path/subpath"},
                new[] {baseAddress, "/path/{variable}/subpath", RemoveTrailingSlash(baseAddress) + "/Path/2/subpath"},
                new[] {baseAddress, "/path/{variable}/subpath/", RemoveTrailingSlash(baseAddress) + "/Path/2/subpath"},
                new[] {baseAddress, "/path/{variable1}/{variable2}", RemoveTrailingSlash(baseAddress) + "/path/2/subpath"},
                new[] {baseAddress, "/path/{variable1}/divider/{variable2}", RemoveTrailingSlash(baseAddress) + "/path/2/divider/subpath"}
            };
        }

        public static IEnumerable<object[]> CreateUriPathNotMatchedCases(string baseAddress)
        {
            return new[]
            {
                new[] {baseAddress, "/path/", RemoveTrailingSlash(baseAddress) + "/path/subpath"},
                new[] {baseAddress, "/subpath", RemoveTrailingSlash(baseAddress) + "/path/subpath"},
                new[] {baseAddress, "/{variable}/subpath", RemoveTrailingSlash(baseAddress) + "/path/2/3/subpath"},
                new[] {baseAddress, "/path/{variable1}/{variable2}", RemoveTrailingSlash(baseAddress) + "/v/2/subpath"},
                new[] {baseAddress, "/path/{variable1}/{variable2}", RemoveTrailingSlash(baseAddress) + "/path/2/subpath/additional"},
                new[] {baseAddress, "/path/{variable1}/divider/{variable2}", RemoveTrailingSlash(baseAddress) + "/path/2/divider"}
            };
        }

        public static IEnumerable<object[]> CaptureVariableCases => new[]
        {
            new object[] {"/path/{variable}", "http://www.base.address.com:80/app/path/Subpath", new[] {new KeyValuePair<string, object>("variable", "Subpath")}},
            new object[] {"/path/{variable}/subpath", BaseAddressWithoutTrailingSlash + "/path/2/subpath", new[] {new KeyValuePair<string, object>("variable", "2")}},
            new object[] {"/path/{variable1}/{variable2}", BaseAddressWithoutTrailingSlash + "/path/2/subPath", new[] {new KeyValuePair<string, object>("variable1", "2"), new KeyValuePair<string, object>("variable2", "subPath")}},
            new object[] {"/path/{variable1}/divider/{variable2}", BaseAddressWithoutTrailingSlash + "/path/2/divider/Subpath", new[] {new KeyValuePair<string, object>("variable1", "2"), new KeyValuePair<string, object>("variable2", "Subpath")}}
        };

        public static IEnumerable<object[]> CaptureQueryStringVariableCases => new[]
        {
            new object[] {"path?name1={variable1}", BaseAddressWithoutTrailingSlash + "/path?name1=v1", new[] {new KeyValuePair<string, object>("variable1", "v1")}},
            new object[] {"path?name1={variable1}&name2={variable2}", BaseAddressWithoutTrailingSlash + "/path?name1=v1&name2=v2", new[] {new KeyValuePair<string, object>("variable1", "v1"), new KeyValuePair<string, object>("variable2", "v2")}},
            new object[] {"path?name2={variable2}&name1={variable1}", BaseAddressWithoutTrailingSlash + "/path?name1=v1&name2=v2", new[] {new KeyValuePair<string, object>("variable1", "v1"), new KeyValuePair<string, object>("variable2", "v2")}},
            new object[] {"path?name1={variable1}&name2={variable2}", BaseAddressWithoutTrailingSlash + "/path?name1=v1", new[] {new KeyValuePair<string, object>("variable1", "v1"), new KeyValuePair<string, object>("variable2", string.Empty)}},
            new object[] {"path?{this_is_not_variable}={this_is_variable}", BaseAddressWithoutTrailingSlash + "/path?{this_is_not_variable}=v1", new[] {new KeyValuePair<string, object>("this_is_variable", "v1")}}
        };

        static string RemoveTrailingSlash(string value)
        {
            return value.TrimEnd('/');
        }
    }
}