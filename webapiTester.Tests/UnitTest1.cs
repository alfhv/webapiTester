using System;
using Xunit;

namespace webapiTester.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var target = new WebApiTester();

            var inputFiles = new string[] 
            {
                @"D:\tests\1_input.json",
                @"D:\tests\2.input.json",
                @"D:\tests\3-input.json"
            };
            var outputFiles = new string[] 
            {
                @"D:\tests\1-output.json",
                @"D:\tests\2_output.json"
            };

            var result = target.MatchFiles(inputFiles, outputFiles);
        }
    }
}
