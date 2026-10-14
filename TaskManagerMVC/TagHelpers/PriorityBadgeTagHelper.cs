using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskManagerMVC.TagHelpers
{
    
    [HtmlTargetElement("priority-badge")]
    public class PriorityBadgeTagHelper : TagHelper
    {
        public string Priority { get; set; } = "";
        public string CssClass { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string badgeColor = Priority switch
            {
                "High" => "#EF4444",
                "Medium" => "#F59E0B",
                "Low" => "#10B981",
                _ => "#6B7280"
            };

            output.TagName = "span";
            output.Attributes.SetAttribute("class", $"badge {CssClass}");
            output.Attributes.SetAttribute("style", $"background-color: {badgeColor};");
            output.Content.SetContent(Priority);
        }
    }
}