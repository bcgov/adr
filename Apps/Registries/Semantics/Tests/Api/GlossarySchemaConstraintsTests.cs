namespace Adr.Semantics.Tests
{
    using Adr.Semantics.Models;

    public class GlossarySchemaConstraintsTests
    {
        [Theory]
        [InlineData(
            "string",
            """{"enum":["one","two"],"format":"email","minLength":1,"maxLength":10,"pattern":"^[a-z]+$"}""",
            5
        )]
        [InlineData(
            "number",
            """{"enum":[1,2.5],"format":"double","minimum":0,"maximum":100,"exclusiveMinimum":true,"exclusiveMaximum":false,"multipleOf":0.5}""",
            7
        )]
        [InlineData(
            "integer",
            """{"enum":[1,2],"format":"int64","minimum":0,"maximum":100,"multipleOf":2}""",
            5
        )]
        [InlineData("boolean", """{"enum":[true,false]}""", 1)]
        public void AcceptsConstraintsAppropriateForType(
            string schemaType,
            string source,
            int expectedCount
        )
        {
            var result = GlossarySchemaConstraints.Parse(source, schemaType);

            Assert.Empty(result.Issues);
            Assert.Equal(expectedCount, result.Constraints.Count);
        }

        [Fact]
        public void ExcludesConstraintsWithInvalidRelationships()
        {
            var result = GlossarySchemaConstraints.Parse(
                """{"minimum":10,"maximum":5,"exclusiveMaximum":true}""",
                "number"
            );

            Assert.Contains("minimum", result.Constraints.Keys);
            Assert.DoesNotContain("maximum", result.Constraints.Keys);
            Assert.DoesNotContain("exclusiveMaximum", result.Constraints.Keys);
            Assert.Contains(result.Issues, issue => issue.ConstraintName == "maximum");
            Assert.Contains(
                result.Issues,
                issue => issue.ConstraintName == "exclusiveMaximum"
            );
        }

        [Theory]
        [InlineData("string", """{"enum":["one",2]}""", "enum")]
        [InlineData("string", """{"pattern":"("}""", "pattern")]
        [InlineData("boolean", """{"format":"boolean"}""", "format")]
        [InlineData("integer", """{"enum":[1,1.5]}""", "enum")]
        [InlineData("number", """{"unknown":1}""", "unknown")]
        public void ExcludesInvalidConstraint(
            string schemaType,
            string source,
            string constraintName
        )
        {
            var result = GlossarySchemaConstraints.Parse(source, schemaType);

            Assert.Empty(result.Constraints);
            Assert.Contains(
                result.Issues,
                issue => issue.ConstraintName == constraintName
            );
        }
    }
}
