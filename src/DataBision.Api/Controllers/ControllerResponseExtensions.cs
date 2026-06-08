using DataBision.Api.Contracts;
using DataBision.Application.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

public static class ControllerResponseExtensions
{
    public static IActionResult OkData<T>(this ControllerBase ctrl, T data) =>
        ctrl.Ok(new ApiResponse<T> { Data = data, TraceId = ctrl.HttpContext.TraceIdentifier });

    public static IActionResult OkPaged<T>(
        this ControllerBase ctrl,
        IReadOnlyList<T> data,
        PagedMetaDto meta) =>
        ctrl.Ok(new PagedApiResponse<T>
        {
            Data = data,
            Meta = meta,
            TraceId = ctrl.HttpContext.TraceIdentifier,
        });

    public static IActionResult BadRequestError(
        this ControllerBase ctrl, string error, string message) =>
        ctrl.BadRequest(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            TraceId = ctrl.HttpContext.TraceIdentifier,
        });

    public static IActionResult ForbiddenError(
        this ControllerBase ctrl, string error, string message) =>
        new ObjectResult(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            TraceId = ctrl.HttpContext.TraceIdentifier,
        })
        { StatusCode = 403 };
}
