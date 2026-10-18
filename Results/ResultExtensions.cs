using Microsoft.AspNetCore.Mvc;

namespace SocialMediaAppBackend.Results;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.Success)
        {
            return controller.Ok(result.Data);
        }

        return result.Kind switch
        {
            ErrorKind.NotFound => controller.NotFound(new { message = result.Error }),
            ErrorKind.Unauthorized => controller.Unauthorized(new { message = result.Error }),
            ErrorKind.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, new { message = result.Error }),
            ErrorKind.Conflict => controller.Conflict(new { message = result.Error }),
            _ => controller.BadRequest(new { message = result.Error })
        };
    }
}
