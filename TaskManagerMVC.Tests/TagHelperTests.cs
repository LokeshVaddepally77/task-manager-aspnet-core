using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using TaskManagerMVC.TagHelpers;

namespace TaskManagerMVC.Tests
{
    public class TagHelperTests
    {
        private static (TagHelperContext Context, TagHelperOutput Output) Create(string tagName)
        {
            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(tagName, new TagHelperAttributeList(),
                (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            return (context, output);
        }

        // ==================== STATUS BADGE ====================

        [Theory]
        [InlineData("New", "#3B82F6")]
        [InlineData("In Progress", "#06B6D4")]
        [InlineData("Completed", "#10B981")]
        [InlineData("On Hold", "#F59E0B")]
        [InlineData("Archived", "#6B7280")]
        public void StatusBadge_RendersSpanWithStatusColor(string status, string expectedColor)
        {
            var (context, output) = Create("status-badge");

            new StatusBadgeTagHelper { Status = status, CssClass = "ms-2" }.Process(context, output);

            Assert.Equal("span", output.TagName);
            Assert.Equal("badge ms-2", output.Attributes["class"].Value);
            Assert.Contains(expectedColor, output.Attributes["style"].Value.ToString());
            Assert.Equal(status, output.Content.GetContent());
        }

        // ==================== PRIORITY BADGE ====================

        [Theory]
        [InlineData("High", "#EF4444")]
        [InlineData("Medium", "#F59E0B")]
        [InlineData("Low", "#10B981")]
        [InlineData("Urgent", "#6B7280")]
        public void PriorityBadge_RendersSpanWithPriorityColor(string priority, string expectedColor)
        {
            var (context, output) = Create("priority-badge");

            new PriorityBadgeTagHelper { Priority = priority }.Process(context, output);

            Assert.Equal("span", output.TagName);
            Assert.Contains(expectedColor, output.Attributes["style"].Value.ToString());
            Assert.Equal(priority, output.Content.GetContent());
        }

        // ==================== PROGRESS BAR ====================

        [Theory]
        [InlineData(85, "bg-success")]
        [InlineData(80, "bg-success")]
        [InlineData(65, "bg-info")]
        [InlineData(45, "bg-warning")]
        [InlineData(10, "bg-danger")]
        public void ProgressBar_PicksColorByThreshold(int percentage, string expectedClass)
        {
            var (context, output) = Create("progress-bar");

            new ProgressBarTagHelper { Percentage = percentage }.Process(context, output);

            Assert.Equal("div", output.TagName);
            Assert.Contains($"progress-bar {expectedClass}", output.Content.GetContent());
            Assert.Contains($"aria-valuenow='{percentage}'", output.Content.GetContent());
        }

        [Theory]
        [InlineData(150, 100)]
        [InlineData(-20, 0)]
        public void ProgressBar_ClampsPercentageBetween0And100(int percentage, int expected)
        {
            var (context, output) = Create("progress-bar");

            new ProgressBarTagHelper { Percentage = percentage }.Process(context, output);

            Assert.Contains($"width: {expected}%", output.Content.GetContent());
            Assert.Contains($"aria-valuenow='{expected}'", output.Content.GetContent());
        }

        [Fact]
        public void ProgressBar_WithoutLabel_OmitsPercentageText()
        {
            var (withLabelContext, withLabel) = Create("progress-bar");
            var (noLabelContext, noLabel) = Create("progress-bar");

            new ProgressBarTagHelper { Percentage = 45, ShowLabel = true }.Process(withLabelContext, withLabel);
            new ProgressBarTagHelper { Percentage = 45, ShowLabel = false }.Process(noLabelContext, noLabel);

            // "45%" appears in the width style, and once more as the visible label
            Assert.Equal(2, Regex.Matches(withLabel.Content.GetContent(), "45%").Count);
            Assert.Single(Regex.Matches(noLabel.Content.GetContent(), "45%"));
        }

        [Fact]
        public void ProgressBar_UsesConfiguredHeight()
        {
            var (context, output) = Create("progress-bar");

            new ProgressBarTagHelper { Percentage = 50, Height = "8px" }.Process(context, output);

            Assert.Equal("height: 8px;", output.Attributes["style"].Value);
        }
    }
}
