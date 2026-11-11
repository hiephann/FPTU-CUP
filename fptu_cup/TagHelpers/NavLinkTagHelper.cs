using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace fptu_cup.TagHelpers
{
    /// <summary>
    /// TagHelper tự viết: &lt;a nav-key="schedule" ...&gt; được thêm class "is-active" và aria-current="page"
    /// khi trang hiện tại đặt ViewData["Nav"] = "schedule" (dùng cho menu công khai và menu quản lý).
    /// </summary>
    [HtmlTargetElement("a", Attributes = "nav-key")]
    public class NavLinkTagHelper : TagHelper
    {
        [HtmlAttributeName("nav-key")]
        public string NavKey { get; set; } = "";

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.Equals(ViewContext.ViewData["Nav"] as string, NavKey, StringComparison.OrdinalIgnoreCase))
            {
                output.AddClass("is-active", HtmlEncoder.Default);
                output.Attributes.SetAttribute("aria-current", "page");
            }
        }
    }
}
