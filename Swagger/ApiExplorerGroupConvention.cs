using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SeinServices.Api.Swagger
{
    public class ApiExplorerGroupConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace ?? string.Empty;

            if (controllerNamespace.Contains(".FaultMon", StringComparison.Ordinal))
            {
                controller.ApiExplorer.GroupName = "faultmon";
                return;
            }

            if (controllerNamespace.Contains(".Chungyak", StringComparison.Ordinal))
            {
                controller.ApiExplorer.GroupName = "chungyak";
            }
        }
    }
}
