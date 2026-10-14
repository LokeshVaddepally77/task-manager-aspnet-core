using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskManagerMVC.TagHelpers
{
    
    [HtmlTargetElement("status-badge")]
    public class StatusBadgeTagHelper : TagHelper
    {
        public string Status { get; set; } = "";
        public string CssClass { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string badgeColor = Status switch
            {
                "In Progress" => "#06B6D4",
                "Completed" => "#10B981",
                "New" => "#3B82F6",
                "On Hold" => "#F59E0B",
                _ => "#6B7280"
            };

            output.TagName = "span";
            output.Attributes.SetAttribute("class", $"badge {CssClass}");
            output.Attributes.SetAttribute("style", $"background-color: {badgeColor};");
            output.Content.SetContent(Status);
        }
    }
}