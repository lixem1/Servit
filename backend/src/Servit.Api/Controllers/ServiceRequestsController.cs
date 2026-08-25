using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Servit.Api.Contracts.ServiceRequests;
using Servit.Api.Extensions;
using Servit.Api.Hubs;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/service-requests")]
public class ServiceRequestsController(
    ServitDbContext dbContext,
    IHubContext<ServiceRequestsHub> hub,
    IFileStorageService fileStorage) : ControllerBase
{
    private const double MatchRadiusMeters = 20_000;

    private const int MaxPhotos = 10;
    private const int MaxAudios = 3;
    private const long MaxPhotoBytes = 8 * 1024 * 1024;
    private const long MaxVideoBytes = 100 * 1024 * 1024;
    private const long MaxAudioBytes = 15 * 1024 * 1024;

    private static readonly HashSet<string> AllowedPhotoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/heic", "image/heif"
    };
    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime"
    };
    private static readonly HashSet<string> AllowedAudioTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mp4", "audio/m4a", "audio/x-m4a", "audio/mpeg", "audio/wav", "audio/x-wav"
    };

    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(150 * 1024 * 1024)]
    public async Task<ActionResult<ServiceRequestDto>> Create([FromForm] CreateServiceRequestRequest request)
    {
        var category = await dbContext.Categories.FindAsync(request.CategoryId);
        if (category is null) return BadRequest("Invalid categoryId.");

        var photos = request.Photos ?? [];
        var audios = request.Audios ?? [];

        if (photos.Count > MaxPhotos) return BadRequest($"A maximum of {MaxPhotos} photos is allowed.");
        if (audios.Count > MaxAudios) return BadRequest($"A maximum of {MaxAudios} audio clips is allowed.");

        foreach (var photo in photos)
        {
            if (!AllowedPhotoTypes.Contains(photo.ContentType)) return BadRequest($"Unsupported photo type: {photo.ContentType}.");
            if (photo.Length > MaxPhotoBytes) return BadRequest("A photo exceeds the maximum allowed size.");
        }
        if (request.Video is not null)
        {
            if (!AllowedVideoTypes.Contains(request.Video.ContentType)) return BadRequest($"Unsupported video type: {request.Video.ContentType}.");
            if (request.Video.Length > MaxVideoBytes) return BadRequest("The video exceeds the maximum allowed size.");
        }
        foreach (var audio in audios)
        {
            if (!AllowedAudioTypes.Contains(audio.ContentType)) return BadRequest($"Unsupported audio type: {audio.ContentType}.");
            if (audio.Length > MaxAudioBytes) return BadRequest("An audio clip exceeds the maximum allowed size.");
        }

        var location = new Point(request.Lng, request.Lat) { SRID = 4326 };
        var serviceRequest = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = User.GetUserId(),
            CategoryId = request.CategoryId,
            Description = request.Description,
            Location = location
        };

        dbContext.ServiceRequests.Add(serviceRequest);

        foreach (var photo in photos)
        {
            serviceRequest.Attachments.Add(await SaveAttachmentAsync(serviceRequest.Id, photo, AttachmentType.Photo));
        }
        if (request.Video is not null)
        {
            serviceRequest.Attachments.Add(await SaveAttachmentAsync(serviceRequest.Id, request.Video, AttachmentType.Video));
        }
        foreach (var audio in audios)
        {
            serviceRequest.Attachments.Add(await SaveAttachmentAsync(serviceRequest.Id, audio, AttachmentType.Audio));
        }

        await dbContext.SaveChangesAsync();

        var matchedProviders = await GetMatchedProviderUserIdsAsync(request.CategoryId, location);

        var dto = ToDto(serviceRequest, category.Name);
        foreach (var providerUserId in matchedProviders)
        {
            await hub.Clients.User(providerUserId.ToString())
                .SendAsync(HubEvents.ServiceRequestCreated, dto);
        }

        return Ok(dto);
    }

    private async Task<List<Guid>> GetMatchedProviderUserIdsAsync(int categoryId, Point location)
    {
        return await dbContext.Providers
            .Where(p => p.Location != null
                && p.User.DeletedAt == null
                && p.ProviderCategories.Any(pc => pc.CategoryId == categoryId)
                && p.Location.Distance(location) <= MatchRadiusMeters)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    private async Task<ServiceRequestAttachment> SaveAttachmentAsync(Guid serviceRequestId, IFormFile file, AttachmentType type)
    {
        await using var stream = file.OpenReadStream();
        var relativePath = await fileStorage.SaveAsync(stream, file.FileName, serviceRequestId.ToString());

        return new ServiceRequestAttachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            Type = type,
            StoragePath = relativePath,
            FileName = file.FileName,
            ContentType = file.ContentType
        };
    }

    [HttpGet("nearby")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetNearby()
    {
        var provider = await dbContext.Providers
            .Include(p => p.ProviderCategories)
            .FirstOrDefaultAsync(p => p.UserId == User.GetUserId());
        if (provider is null) return NotFound();

        var categoryIds = provider.ProviderCategories.Select(pc => pc.CategoryId).ToList();

        var pending = await dbContext.ServiceRequests
            .Include(sr => sr.Category)
            .Include(sr => sr.Attachments)
            .Where(sr => sr.Status == ServiceRequestStatus.Pending && categoryIds.Contains(sr.CategoryId))
            .ToListAsync();

        var requestIds = pending.Select(sr => sr.Id).ToList();
        var myResponses = (await dbContext.ProviderResponses
            .Where(r => r.ProviderId == provider.Id && requestIds.Contains(r.ServiceRequestId))
            .ToListAsync())
            .ToDictionary(r => r.ServiceRequestId);

        MyProviderResponseDto? MyResponseFor(Guid requestId) =>
            myResponses.TryGetValue(requestId, out var r) ? ToMyResponseDto(r) : null;

        var results = provider.Location is null
            ? pending.OrderByDescending(sr => sr.CreatedAt)
                .Select(sr => ToDto(sr, sr.Category.Name, myResponse: MyResponseFor(sr.Id)))
            : pending.Select(sr => ToDto(sr, sr.Category.Name, sr.Location.Distance(provider.Location), myResponse: MyResponseFor(sr.Id)))
                .OrderBy(dto => dto.DistanceMeters);

        return Ok(results.ToList());
    }

    [HttpGet("assigned")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetAssigned()
    {
        var provider = await dbContext.Providers.FirstOrDefaultAsync(p => p.UserId == User.GetUserId());
        if (provider is null) return NotFound();

        var requests = await dbContext.ServiceRequests
            .Include(sr => sr.Category)
            .Include(sr => sr.Attachments)
            .Include(sr => sr.Customer)
            .Include(sr => sr.Responses)
            .Where(sr => sr.Responses.Any(r => r.ProviderId == provider.Id && r.Status == ProviderResponseStatus.Accepted))
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();

        var requestIds = requests.Select(sr => sr.Id).ToList();
        var reviewsByRequestId = await dbContext.Reviews
            .Where(r => requestIds.Contains(r.ServiceRequestId))
            .ToDictionaryAsync(r => r.ServiceRequestId);

        return Ok(requests.Select(sr =>
        {
            var accepted = sr.Responses.First(r => r.ProviderId == provider.Id && r.Status == ProviderResponseStatus.Accepted);
            reviewsByRequestId.TryGetValue(sr.Id, out var review);
            return ToDto(sr, sr.Category.Name, myResponse: ToMyResponseDto(accepted), customerName: sr.Customer.FullName, review: review);
        }).ToList());
    }

    private static MyProviderResponseDto ToMyResponseDto(ProviderResponse r) => new()
    {
        Id = r.Id,
        Message = r.Message,
        ProposedPrice = r.ProposedPrice,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt
    };

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetMine()
    {
        var userId = User.GetUserId();
        var requests = await dbContext.ServiceRequests
            .Include(sr => sr.Category)
            .Include(sr => sr.Attachments)
            .Include(sr => sr.Responses)
            .Where(sr => sr.CustomerId == userId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();

        var requestIds = requests.Select(sr => sr.Id).ToList();
        var reviewedIds = (await dbContext.Reviews
            .Where(r => requestIds.Contains(r.ServiceRequestId))
            .Select(r => r.ServiceRequestId)
            .ToListAsync()).ToHashSet();

        return Ok(requests.Select(sr => ToDto(
            sr,
            sr.Category.Name,
            hasReview: reviewedIds.Contains(sr.Id),
            responseCount: sr.Responses.Count)).ToList());
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(Guid id, Guid attachmentId)
    {
        var serviceRequest = await dbContext.ServiceRequests.FindAsync(id);
        if (serviceRequest is null) return NotFound();

        var isOwner = serviceRequest.CustomerId == User.GetUserId();
        if (!isOwner && !User.IsInRole(Roles.Provider)) return Forbid();

        var attachment = await dbContext.ServiceRequestAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ServiceRequestId == id);
        if (attachment is null) return NotFound();

        var stream = fileStorage.OpenRead(attachment.StoragePath);
        return File(stream, attachment.ContentType, attachment.FileName);
    }

    [HttpPost("{id:guid}/responses")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<ProviderResponseDto>> Respond(Guid id, CreateProviderResponseRequest request)
    {
        var serviceRequest = await dbContext.ServiceRequests.FindAsync(id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.Status != ServiceRequestStatus.Pending)
        {
            return BadRequest("This request is no longer accepting responses.");
        }

        var provider = await dbContext.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == User.GetUserId());
        if (provider is null) return NotFound();

        var response = await dbContext.ProviderResponses
            .FirstOrDefaultAsync(r => r.ServiceRequestId == id && r.ProviderId == provider.Id);

        if (response is null)
        {
            response = new ProviderResponse
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = id,
                ProviderId = provider.Id
            };
            dbContext.ProviderResponses.Add(response);
        }
        else if (response.Status == ProviderResponseStatus.Accepted)
        {
            return BadRequest("This request is no longer accepting responses.");
        }

        response.Message = request.Message;
        response.ProposedPrice = request.ProposedPrice;
        response.Status = ProviderResponseStatus.Pending;

        await dbContext.SaveChangesAsync();

        var dto = ToDto(response, provider);
        await hub.Clients.User(serviceRequest.CustomerId.ToString())
            .SendAsync(HubEvents.ResponseReceived, dto);

        return Ok(dto);
    }

    [HttpGet("{id:guid}/responses")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<List<ProviderResponseDto>>> GetResponses(Guid id)
    {
        var serviceRequest = await dbContext.ServiceRequests.FindAsync(id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();

        var responses = await dbContext.ProviderResponses
            .Include(r => r.Provider).ThenInclude(p => p.User)
            .Where(r => r.ServiceRequestId == id)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        return Ok(responses.Select(r => ToDto(r, r.Provider)).ToList());
    }

    [HttpPost("{id:guid}/responses/{responseId:guid}/accept")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> AcceptResponse(Guid id, Guid responseId)
    {
        var serviceRequest = await dbContext.ServiceRequests
            .Include(sr => sr.Responses).ThenInclude(r => r.Provider).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(sr => sr.Id == id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();

        var accepted = serviceRequest.Responses.FirstOrDefault(r => r.Id == responseId);
        if (accepted is null) return NotFound();

        accepted.Status = ProviderResponseStatus.Accepted;
        foreach (var other in serviceRequest.Responses.Where(r => r.Id != responseId))
        {
            other.Status = ProviderResponseStatus.Declined;
        }
        serviceRequest.Status = ServiceRequestStatus.Assigned;

        var matchedProviders = await GetMatchedProviderUserIdsAsync(serviceRequest.CategoryId, serviceRequest.Location);

        await dbContext.SaveChangesAsync();

        await hub.Clients.User(accepted.Provider.UserId.ToString())
            .SendAsync(HubEvents.ResponseAccepted, ToDto(accepted, accepted.Provider));

        foreach (var providerUserId in matchedProviders)
        {
            await hub.Clients.User(providerUserId.ToString())
                .SendAsync(HubEvents.ServiceRequestUnavailable, serviceRequest.Id);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/responses/{responseId:guid}/reject")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> RejectResponse(Guid id, Guid responseId)
    {
        var serviceRequest = await dbContext.ServiceRequests
            .Include(sr => sr.Responses).ThenInclude(r => r.Provider).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(sr => sr.Id == id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();

        var rejected = serviceRequest.Responses.FirstOrDefault(r => r.Id == responseId);
        if (rejected is null) return NotFound();
        if (rejected.Status != ProviderResponseStatus.Pending)
        {
            return BadRequest("Only a pending response can be rejected.");
        }

        rejected.Status = ProviderResponseStatus.Declined;
        await dbContext.SaveChangesAsync();

        await hub.Clients.User(rejected.Provider.UserId.ToString())
            .SendAsync(HubEvents.ResponseDeclined, ToDto(rejected, rejected.Provider));

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var serviceRequest = await dbContext.ServiceRequests
            .Include(sr => sr.Category)
            .Include(sr => sr.Responses).ThenInclude(r => r.Provider).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(sr => sr.Id == id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();
        if (serviceRequest.Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
        {
            return BadRequest("This request can no longer be cancelled.");
        }

        var pendingResponses = serviceRequest.Responses
            .Where(r => r.Status == ProviderResponseStatus.Pending)
            .ToList();
        foreach (var response in pendingResponses)
        {
            response.Status = ProviderResponseStatus.Declined;
        }

        serviceRequest.Status = ServiceRequestStatus.Cancelled;

        var matchedProviders = await GetMatchedProviderUserIdsAsync(serviceRequest.CategoryId, serviceRequest.Location);

        await dbContext.SaveChangesAsync();

        // A cancellation isn't a "your quote was declined, try a cheaper one" situation —
        // the whole request is gone, so it gets its own event/message instead of ResponseDeclined.
        foreach (var response in pendingResponses)
        {
            await hub.Clients.User(response.Provider.UserId.ToString())
                .SendAsync(HubEvents.ServiceRequestCancelled, new { requestId = serviceRequest.Id, categoryName = serviceRequest.Category.Name });
        }

        foreach (var providerUserId in matchedProviders)
        {
            await hub.Clients.User(providerUserId.ToString())
                .SendAsync(HubEvents.ServiceRequestUnavailable, serviceRequest.Id);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Complete(Guid id)
    {
        var serviceRequest = await dbContext.ServiceRequests.FindAsync(id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();
        if (serviceRequest.Status != ServiceRequestStatus.Assigned)
        {
            return BadRequest("Only an assigned request can be marked as completed.");
        }

        serviceRequest.Status = ServiceRequestStatus.Completed;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<ReviewDto>> SubmitReview(Guid id, SubmitReviewRequest request)
    {
        var serviceRequest = await dbContext.ServiceRequests
            .Include(sr => sr.Responses)
            .Include(sr => sr.Customer)
            .FirstOrDefaultAsync(sr => sr.Id == id);
        if (serviceRequest is null) return NotFound();
        if (serviceRequest.CustomerId != User.GetUserId()) return Forbid();
        if (serviceRequest.Status != ServiceRequestStatus.Completed)
        {
            return BadRequest("Only a completed request can be reviewed.");
        }

        var alreadyReviewed = await dbContext.Reviews.AnyAsync(r => r.ServiceRequestId == id);
        if (alreadyReviewed) return BadRequest("This request has already been reviewed.");

        var acceptedResponse = serviceRequest.Responses.FirstOrDefault(r => r.Status == ProviderResponseStatus.Accepted);
        if (acceptedResponse is null) return BadRequest("This request has no accepted provider to review.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = id,
            ProviderId = acceptedResponse.ProviderId,
            CustomerId = serviceRequest.CustomerId,
            Rating = request.Rating,
            Comment = request.Comment
        };
        dbContext.Reviews.Add(review);

        var provider = await dbContext.Providers.FindAsync(acceptedResponse.ProviderId);
        if (provider is not null)
        {
            var ratings = await dbContext.Reviews
                .Where(r => r.ProviderId == provider.Id)
                .Select(r => r.Rating)
                .ToListAsync();
            ratings.Add(request.Rating);

            provider.RatingCount = ratings.Count;
            provider.AverageRating = ratings.Average();
        }

        await dbContext.SaveChangesAsync();

        return Ok(new ReviewDto
        {
            Id = review.Id,
            CustomerName = serviceRequest.Customer.FullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }

    private static ServiceRequestDto ToDto(
        ServiceRequest sr,
        string categoryName,
        double? distanceMeters = null,
        bool hasReview = false,
        int responseCount = 0,
        MyProviderResponseDto? myResponse = null,
        string? customerName = null,
        Review? review = null) => new()
    {
        Id = sr.Id,
        CategoryId = sr.CategoryId,
        CategoryName = categoryName,
        Description = sr.Description,
        Lat = sr.Location.Y,
        Lng = sr.Location.X,
        Status = sr.Status.ToString(),
        CreatedAt = sr.CreatedAt,
        DistanceMeters = distanceMeters,
        HasReview = hasReview || review != null,
        ResponseCount = responseCount,
        MyResponse = myResponse,
        CustomerName = customerName,
        Rating = review?.Rating,
        ReviewComment = review?.Comment,
        Attachments = sr.Attachments.Select(a => new AttachmentDto
        {
            Id = a.Id,
            Type = a.Type.ToString(),
            FileName = a.FileName
        }).ToList()
    };

    private static ProviderResponseDto ToDto(ProviderResponse response, Provider provider) => new()
    {
        Id = response.Id,
        ServiceRequestId = response.ServiceRequestId,
        ProviderId = provider.Id,
        ProviderName = provider.User.FullName,
        ProviderAverageRating = provider.AverageRating,
        ProviderRatingCount = provider.RatingCount,
        Message = response.Message,
        ProposedPrice = response.ProposedPrice,
        Status = response.Status.ToString(),
        CreatedAt = response.CreatedAt
    };
}
