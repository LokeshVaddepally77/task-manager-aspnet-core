using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskManagerMVC.TagHelpers
{

    [HtmlTargetElement("progress-bar")]
    public class ProgressBarTagHelper : TagHelper
    {
        public int Percentage { get; set; }
        public bool ShowLabel { get; set; } = true;
        public string Height { get; set; } = "20px";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            int boundedPercentage = Percentage > 100 ? 100 : Percentage < 0 ? 0 : Percentage;

            string barColor = boundedPercentage switch
            {
                >= 80 => "bg-success",
                >= 60 => "bg-info",
                >= 40 => "bg-warning",
                _ => "bg-danger"
            };

            output.TagName = "div";
            output.Attributes.SetAttribute("class", "progress");
            output.Attributes.SetAttribute("style", $"height: {Height};");

            string progressHtml = $@"
                <div class='progress-bar {barColor}' 
                     role='progressbar' 
                     style='width: {boundedPercentage}%' 
                     aria-valuenow='{boundedPercentage}' 
                     aria-valuemin='0' 
                     aria-valuemax='100'>
                    {(ShowLabel ? $"{boundedPercentage}%" : "")}
                </div>";

            output.Content.SetHtmlContent(progressHtml);
        }
    }
}