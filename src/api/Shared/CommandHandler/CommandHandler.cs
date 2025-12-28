using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.Reflection;
using System.Text;
namespace Shared;
public abstract class CommandHandler : ICommandHandler
{
    protected IServiceProvider ServiceProvider { get; private set; }

    private HttpContext httpContext { get; set; }

    protected HttpContext HttpContext
    {
        get
        {
            return httpContext ??= GetService<IHttpContextAccessor>().HttpContext;
        }
    }

    private HttpRequest request { get; set; }

    protected HttpRequest Request
    {
        get
        {
            return request ??= HttpContext.Request;
        }
    }

    protected T GetService<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public IServiceProvider SetServiceProvider(IServiceProvider serviceProvider)
    {
        return ServiceProvider = serviceProvider;
    }

    protected string UserId
    {
        get
        {
            var usrId = HttpContext.User.FindFirst("UserId")?.Value;
            if (usrId == null)
                return null;

            return usrId;
        }
    }

    protected long EmployeeId
    {
        get
        {
            var usrId = HttpContext.User.FindFirst("UserId")?.Value;
            if (usrId == null)
                return -1;

            _ = long.TryParse(usrId, out long userId);

            return userId;
        }
    }

    protected long WorkspaceId
    {
        get
        {
            return Request.Headers.TryGetValue("WorkspaceId", out Microsoft.Extensions.Primitives.StringValues value) ? value.ToString().ToLong() : Request.Query["workspace"].ToString().ToLong();
        }
    }

    protected string UserEmail
    {
        get
        {
            return HttpContext.User.FindFirst("Email")?.Value.ToString();
        }
    }

    public bool BreakPipeline { get; private set; }

    public Dictionary<string, List<string>> Errors { get; set; } = [];

    public void AddError(string key, string message)
    {
        Errors ??= [];

        if (!Errors.ContainsKey(key))
            Errors[key] = [];

        Errors[key].Add(message);
    }

    public void AddError(string key, string[] messages)
    {
        Errors ??= [];

        if (!Errors.ContainsKey(key))
            Errors[key] = [];

        foreach (var message in messages)
            Errors[key].Add(message);
    }

    public void AddError(string key, List<string> messages)
    {
        AddError(key, messages.ToArray());
    }

    public void AddErrors(Dictionary<string, List<string>> errors)
    {
        foreach (var r in errors)
            AddError(r.Key, r.Value);
    }

    public void AddError(bool condition, string key, string message)
    {
        if (!condition)
            return;

        Errors ??= [];

        if (!Errors.ContainsKey(key))
            Errors[key] = [];

        Errors[key].Add(message);
    }

    public virtual Task<IResult> Validate()
    {
        return Task.FromResult<IResult>(default);
    }

    protected Task<IResult> Next() => Task.FromResult<IResult>(default);

    public virtual object Response()
    {
        return Ok();
    }

    public IResult Redirect(string url, bool permanent = false, bool preserveMethod = false)
    {
        return Results.Redirect(url);
    }

    public Task<IResult> RedirectAsync(string url, bool permanent = false, bool preserveMethod = false)
    {
        return Task.FromResult(Results.Redirect(url));
    }

    public static IResult Ok(object data = null)
    {
        return Results.Ok(new ApiResult
        {
            Code = "",
            Status = "SUCCESS",
            Data = data,
            Message = ""
        });
    }

    public static IResult Invalid(string message = null, string code = null)
    {
        return Results.BadRequest(new ApiResult
        {
            Code = code,
            Status = "INVALID",
            Message = message,
            Errors = new Dictionary<string, List<string>>(),
        });
    }

    public static Task<IResult> InvalidAsync(string message = null, string code = null)
    {
        return Task.FromResult(Invalid(message, code));
    }

    public static IResult Invalid(IDictionary<string, List<string>> errors, string message = null, string code = null)
    {
        return Results.BadRequest(new ApiResult
        {
            Code = code,
            Status = "INVALID",
            Message = message,
            Errors = errors,
        });
    }

    public static IResult NotFound()
    {
        return Results.NotFound(new ApiResult
        {
            Code = null,
            Status = "NOTFOUND",
        });
    }

    public static Task<IResult> NotFoundAsync()
    {
        return Task.FromResult(NotFound());
    }

    public IResult Forbid()
    {
        return Results.Forbid();
    }

    public virtual IResult Content(string content)
        => Results.Content(content, (MediaTypeHeaderValue)null);

    public virtual IResult Content(string content, string contentType)
        => Content(content, MediaTypeHeaderValue.Parse(contentType));

    public virtual IResult Content(string content, string contentType, Encoding contentEncoding)
    {
        var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
        mediaTypeHeaderValue.Encoding = contentEncoding ?? mediaTypeHeaderValue.Encoding;
        return Content(content, mediaTypeHeaderValue);
    }

    /// <summary>
    /// Creates a <see cref="ContentResult"/> object by specifying a
    /// <paramref name="content"/> string and a <paramref name="contentType"/>.
    /// </summary>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <returns>The created <see cref="ContentResult"/> object for the response.</returns>
    public virtual IResult Content(string content, MediaTypeHeaderValue contentType)
    {
        return Results.Content(content, contentType.ToString());
    }

    #region FileResult variants
    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType)
        => File(fileContents, contentType, fileDownloadName: null);

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, bool enableRangeProcessing)
        => File(fileContents, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, string? fileDownloadName)
        => new FileContentResult(fileContents, contentType) { FileDownloadName = fileDownloadName };

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, string? fileDownloadName, bool enableRangeProcessing)
        => new FileContentResult(fileContents, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new FileContentResult(fileContents, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new FileContentResult(fileContents, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new FileContentResult(fileContents, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
        };
    }

    /// <summary>
    /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
    [NonAction]
    public virtual FileContentResult File(byte[] fileContents, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new FileContentResult(fileContents, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType)
        => File(fileStream, contentType, fileDownloadName: null);

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, bool enableRangeProcessing)
        => File(fileStream, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, string? fileDownloadName)
        => new FileStreamResult(fileStream, contentType) { FileDownloadName = fileDownloadName };

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, bool enableRangeProcessing)
        => new FileStreamResult(fileStream, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new FileStreamResult(fileStream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>),
    /// and the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new FileStreamResult(fileStream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new FileStreamResult(fileStream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
        };
    }

    /// <summary>
    /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    [NonAction]
    public virtual FileStreamResult File(Stream fileStream, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new FileStreamResult(fileStream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType)
        => File(virtualPath, contentType, fileDownloadName: null);

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, bool enableRangeProcessing)
        => File(virtualPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, string? fileDownloadName)
        => new VirtualFileResult(virtualPath, contentType) { FileDownloadName = fileDownloadName };

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, string? fileDownloadName, bool enableRangeProcessing)
        => new VirtualFileResult(virtualPath, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new VirtualFileResult(virtualPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new VirtualFileResult(virtualPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new VirtualFileResult(virtualPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file to be returned.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
    [NonAction]
    public virtual VirtualFileResult File(string virtualPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new VirtualFileResult(virtualPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType)
        => PhysicalFile(physicalPath, contentType, fileDownloadName: null);

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, bool enableRangeProcessing)
        => PhysicalFile(physicalPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(
        string physicalPath,
        string contentType,
        string? fileDownloadName)
        => new PhysicalFileResult(physicalPath, contentType) { FileDownloadName = fileDownloadName };

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
    /// specified <paramref name="contentType" /> as the Content-Type and the
    /// specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(
        string physicalPath,
        string contentType,
        string? fileDownloadName,
        bool enableRangeProcessing)
        => new PhysicalFileResult(physicalPath, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and
    /// the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new PhysicalFileResult(physicalPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and
    /// the specified <paramref name="contentType" /> as the Content-Type.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new PhysicalFileResult(physicalPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
    {
        return new PhysicalFileResult(physicalPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
        };
    }

    /// <summary>
    /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the
    /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </summary>
    /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
    [NonAction]
    public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
    {
        return new PhysicalFileResult(physicalPath, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }
    #endregion

    public class ApiResult
    {
        public string Code { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public IDictionary<string, List<string>> Errors { get; set; }
    }
}

public abstract class SubProcessHandler
{
    public virtual void Execute()
    {
        var list = from p in GetType().GetMethods()
                   let attr = p.GetCustomAttribute(typeof(Pipeline)) as Pipeline
                   where attr != null
                   orderby attr.Order
                   select p; ;

        foreach (var r in list)
        {
            var attr = r.GetCustomAttribute(typeof(Pipeline)) as Pipeline;
            try
            {
                r.Invoke(this, null);
            }
            catch
            {
                if (!attr.SkipWhenError)
                    throw;
            }
        }
    }
}