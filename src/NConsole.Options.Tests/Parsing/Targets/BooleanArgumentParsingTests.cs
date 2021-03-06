﻿namespace NConsole.Options.Parsing.Targets
{
    using Xunit;
    using Xunit.Abstractions;

    public class BooleanArgumentParsingTests : TargetArgumentParsingTestFixtureBase<bool>
    {
        public BooleanArgumentParsingTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

#pragma warning disable xUnit1008
        /// <summary>
        /// Verifies that the <see cref="OptionSet"/> Can Parse the <paramref name="args"/>.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="requiredOrOptional"></param>
        /// <param name="args"></param>
        /// <param name="expectedValues"></param>
        /// <param name="unprocessedArgs"></param>
        /// <inheritdoc />
        [ClassData(typeof(Data.Parsing.Targets.BooleanOptionSetParsingTestCases))]
        public override void Can_Parse_Arguments(string prototype, string description, char requiredOrOptional
            , string[] args, bool[] expectedValues, string[] unprocessedArgs)
        {
            Callback = ParsedValues.Add;

            base.Can_Parse_Arguments(prototype, description, requiredOrOptional
                , args, expectedValues, unprocessedArgs);
        }
#pragma warning restore xUnit1008

    }
}
