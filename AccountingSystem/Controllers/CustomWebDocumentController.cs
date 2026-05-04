using DevExpress.AspNetCore.Reporting.WebDocumentViewer;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;

namespace AccountingSystem.Controllers
{
    public class CustomWebDocumentController : WebDocumentViewerController
    {
        public CustomWebDocumentController(IWebDocumentViewerMvcControllerService controllerService)
            : base(controllerService) { }
    }
}