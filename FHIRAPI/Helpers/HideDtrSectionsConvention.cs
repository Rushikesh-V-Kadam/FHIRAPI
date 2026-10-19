using Microsoft.AspNetCore.Mvc.ApplicationModels;
using FHIRAPI.Controllers;

namespace FHIRAPI.Helpers;

/// <summary>
/// Hides the DTR controllers (QuestionnaireResponseController, SmartAuthController) from Swagger when
/// Swagger:ShowDtrSections is false. Only Swagger is affected: the endpoints stay registered and keep working, because
/// the Payer Gateway saves and reads DTR forms through them and payer DTR apps use the SMART launch.
/// Registered in Program.cs (options.Conventions) only when the sections are hidden.
/// </summary>
public class HideDtrSectionsConvention : IControllerModelConvention
{
    /// <summary>The controllers whose sections are hidden.</summary>
    private static readonly Type[] HiddenControllers =
    {
        typeof(QuestionnaireResponseController),
        typeof(SmartAuthController)
    };

    /// <summary>Called once per controller at startup.</summary>
    public void Apply(ControllerModel controller)
    {
        if (!HiddenControllers.Contains(controller.ControllerType.AsType()))
        {
            return;
        }
        // Set on the controller and on every action: an action-level value wins over the controller-level one.
        controller.ApiExplorer.IsVisible = false;
        foreach (ActionModel action in controller.Actions)
        {
            action.ApiExplorer.IsVisible = false;
        }
    }
}
