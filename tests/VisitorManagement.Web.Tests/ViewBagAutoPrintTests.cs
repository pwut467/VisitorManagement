using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using VisitorManagement.Web;

namespace VisitorManagement.Web.Tests;

public class ViewBagAutoPrintTests
{
    [Fact]
    public void ViewDataBoxedBoolIsReadable()
    {
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["AutoPrint"] = true
        };

        Assert.True(ViewFlag.IsOn(viewData["AutoPrint"]));
        viewData["AutoPrint"] = false;
        Assert.False(ViewFlag.IsOn(viewData["AutoPrint"]));
        viewData["AutoPrint"] = "true";
        Assert.True(ViewFlag.IsOn(viewData["AutoPrint"]));
    }
}
