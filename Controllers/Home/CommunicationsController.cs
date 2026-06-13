using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.Wrappers;
using TCBackend.Model.Content;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.Home
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommunicationsController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;
        private const long MaxAttachmentBytes = 25L * 1024 * 1024;

        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".gif", ".txt", ".csv", ".zip"
        };

        private readonly TCDbContext _context;
        private readonly IDataTableService _dataTableService;
        private readonly IStorageService _storageService;
        private readonly IPermissionService _permissionService;

        public CommunicationsController(
            TCDbContext context,
            IDataTableService dataTableService,
            IStorageService storageService,
            IPermissionService permissionService)
        {
            _context = context;
            _dataTableService = dataTableService;
            _storageService = storageService;
            _permissionService = permissionService;
        }

        private int GetLoginUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private static (int pageNumber, int pageSize) NormalizePaging(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            return (pageNumber, pageSize);
        }

        // Soft-deletes a content row (sets Status = "Archived"). Validates that the row
        // exists and matches the expected ContentType so a Policy id can't be deleted
        // through the Announcement route, etc.
        private async Task<IActionResult> ArchiveContentAsync(int id, string contentType, string label)
        {
            var item = await _context.ContentItem
                .FirstOrDefaultAsync(c => c.ContentId == id && c.ContentType == contentType);

            if (item == null)
            {
                return NotFound(new ApiResponse<string>(0, $"{label} not found.", null));
            }

            if (item.Status == "Archived")
            {
                return Ok(new ApiResponse<string>(1, $"{label} is already archived.", null));
            }

            item.Status = "Archived";
            item.ModifiedBy = GetLoginUserId();
            item.ModifiedOn = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string>(1, $"{label} deleted successfully.", null));
        }

        // Validates an optional set of uploaded files (size + extension allowlist).
        // Returns an error message when invalid, otherwise null.
        private static string? ValidateAttachments(IEnumerable<IFormFile>? files)
        {
            if (files == null) return null;

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    return $"File '{file.FileName}' is empty.";
                }

                if (file.Length > MaxAttachmentBytes)
                {
                    return $"File '{file.FileName}' exceeds the {MaxAttachmentBytes / (1024 * 1024)}MB limit.";
                }

                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(extension) || !AllowedAttachmentExtensions.Contains(extension))
                {
                    return $"File type '{extension}' is not allowed for '{file.FileName}'.";
                }
            }

            return null;
        }

        // Persists uploaded files to ContentAttachments/{contentId} and inserts the
        // matching ContentAttachment rows. Returns the saved attachment DTOs.
        private async Task<List<ContentAttachmentDto>> SaveAttachmentsAsync(int contentId, IEnumerable<IFormFile>? files)
        {
            var saved = new List<ContentAttachmentDto>();
            if (files == null) return saved;

            int userId = GetLoginUserId();
            string folderName = $"ContentAttachments/{contentId}";

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                string fileUrl = await _storageService.SaveFileAsync(file, folderName);

                var attachment = new ContentAttachment
                {
                    ContentId = contentId,
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    ContentKind = file.ContentType,
                    FileSizeBytes = file.Length,
                    UploadedBy = userId,
                    UploadedOn = DateTime.Now
                };

                _context.ContentAttachment.Add(attachment);
                await _context.SaveChangesAsync();

                saved.Add(new ContentAttachmentDto
                {
                    AttachmentId = attachment.AttachmentId,
                    ContentId = attachment.ContentId,
                    FileName = attachment.FileName,
                    FileUrl = attachment.FileUrl,
                    ContentKind = attachment.ContentKind,
                    FileSizeBytes = attachment.FileSizeBytes,
                    UploadedBy = attachment.UploadedBy,
                    UploadedOn = attachment.UploadedOn
                });
            }

            return saved;
        }

        // Maps a ContentType discriminator to its write permission string.
        private static string? MapContentTypeToWritePermission(string contentType) => contentType switch
        {
            "Announcement" => "announcements.write",
            "Policy" => "policies.write",
            "Post" => "posts.write",
            _ => null
        };

        // Maps a ContentType discriminator to its read permission string.
        private static string? MapContentTypeToReadPermission(string contentType) => contentType switch
        {
            "Announcement" => "announcements.read",
            "Policy" => "policies.read",
            "Post" => "posts.read",
            _ => null
        };

        // Maps a ContentType discriminator to its delete permission string.
        private static string? MapContentTypeToDeletePermission(string contentType) => contentType switch
        {
            "Announcement" => "announcements.delete",
            "Policy" => "policies.delete",
            "Post" => "posts.delete",
            _ => null
        };

        // Resolves author display info (FullName + PhotoUrl) for a set of user ids
        // from vw_EmployeeDetails. Returned as a map keyed by UserId; de-duped because
        // the view can yield multiple rows per user.
        private async Task<Dictionary<int, (string? Name, string? PhotoUrl)>> LoadAuthorsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, (string?, string?)>();
            }

            var rows = await _context.EmployeeDetails.AsNoTracking()
                .Where(e => ids.Contains(e.UserId))
                .Select(e => new { e.UserId, e.FullName, e.PhotoUrl })
                .ToListAsync();

            return rows
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => (g.First().FullName, g.First().PhotoUrl));
        }

        // Executes a paged list stored procedure (@UserId, @PageNo, @PageSize) and
        // builds a PagedResult. The SP is expected to emit a per-row TotalCount window
        // column, read from the first row to populate the envelope's total.
        private async Task<PagedResult<T>> ExecutePagedContentSpAsync<T>(
            string storedProcedure, int pageNumber, int pageSize) where T : IPagedRow
        {
            var connection = _context.Database.GetDbConnection();
            var rows = (await connection.QueryAsync<T>(
                storedProcedure,
                new { UserId = GetLoginUserId(), PageNo = pageNumber, PageSize = pageSize },
                commandType: CommandType.StoredProcedure)).ToList();

            int totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResult<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = rows
            };
        }

        // Batch-loads attachments from ContentAttachment (EF) for the supplied content rows
        // in a single query (no N+1) and assigns them to each row by ContentId.
        private async Task LoadAttachmentsAsync<T>(IReadOnlyCollection<T> items) where T : IAttachmentCarrier
        {
            if (items.Count == 0)
            {
                return;
            }

            var contentIds = items.Select(i => i.ContentId).Distinct().ToList();

            var attachments = await _context.ContentAttachment.AsNoTracking()
                .Where(a => contentIds.Contains(a.ContentId))
                .OrderBy(a => a.UploadedOn)
                .ThenBy(a => a.AttachmentId)
                .Select(a => new ContentAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    ContentId = a.ContentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    ContentKind = a.ContentKind,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedBy = a.UploadedBy,
                    UploadedOn = a.UploadedOn
                })
                .ToListAsync();

            if (attachments.Count == 0)
            {
                return;
            }
            foreach (var attachment in attachments)
            {
                attachment.FileUrl = await _storageService.GetSecureFileUrlAsync(attachment.FileUrl);
            }

            var byContent = attachments.ToLookup(a => a.ContentId);

            foreach (var item in items)
            {
                if (byContent.Contains(item.ContentId))
                {
                    item.Attachments = byContent[item.ContentId].ToList();
                }
            }
        }

        // Generic paged list core for EF-backed, status-scoped content lists (drafts,
        // archived) that the public feed SPs intentionally exclude. Applies the shared
        // ordering (pinned, then newest) and paging, then projects each row with the
        // caller-supplied, content-type-specific projection.
        private async Task<PagedResult<TDto>> GetContentListAsync<TDto>(
            System.Linq.Expressions.Expression<Func<ContentItem, bool>> predicate,
            System.Linq.Expressions.Expression<Func<ContentItem, TDto>> projection,
            int pageNumber, int pageSize)
        {
            var query = _context.ContentItem.AsNoTracking().Where(predicate);

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.CreatedOn)
                .ThenByDescending(c => c.ContentId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(projection)
                .ToListAsync();

            return new PagedResult<TDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        // Post list view: resolves engagement counts and the caller's own reaction, then
        // hydrates authors (vw_EmployeeDetails) and attachments (with secure URLs).
        private async Task<PagedResult<PostListItemDto>> GetPostListAsync(
            System.Linq.Expressions.Expression<Func<ContentItem, bool>> predicate,
            int pageNumber, int pageSize)
        {
            int uid = GetLoginUserId();

            var result = await GetContentListAsync(predicate, c => new PostListItemDto
            {
                ContentId = c.ContentId,
                Kind = c.Kind,
                Title = c.Title,
                Body = c.Body,
                IsPinned = c.IsPinned,
                CreatedOn = c.CreatedOn,
                AuthorUserId = c.CreatedBy,
                LikeCount = c.Engagements.Count(e => e.EngagementType == "Like"),
                CommentCount = c.Comments.Count(cm => cm.Status == "Active"),
                MyReaction = c.Engagements
                    .Where(e => e.EngagementType == "Like" && e.UserId == uid)
                    .Select(e => e.ReactionType)
                    .FirstOrDefault(),
                ILiked = c.Engagements.Any(e => e.EngagementType == "Like" && e.UserId == uid)
            }, pageNumber, pageSize);

            var authors = await LoadAuthorsAsync(result.Items.Select(i => i.AuthorUserId));
            foreach (var item in result.Items)
            {
                if (authors.TryGetValue(item.AuthorUserId, out var author))
                {
                    item.AuthorName = author.Name;
                    item.AuthorPhoto = author.PhotoUrl;
                }
            }

            await LoadAttachmentsAsync(result.Items);

            return result;
        }

        // Announcement list view: resolves category, like count, and the caller's read
        // state, then hydrates the author display name (vw_EmployeeDetails).
        private async Task<PagedResult<AnnouncementListItemDto>> GetAnnouncementListAsync(
            System.Linq.Expressions.Expression<Func<ContentItem, bool>> predicate,
            int pageNumber, int pageSize)
        {
            int uid = GetLoginUserId();

            var result = await GetContentListAsync(predicate, c => new AnnouncementListItemDto
            {
                ContentId = c.ContentId,
                Kind = c.Kind,
                Title = c.Title,
                Body = c.Body,
                Priority = c.Priority,
                IsPinned = c.IsPinned,
                PublishOn = c.PublishOn,
                ExpiresOn = c.ExpiresOn,
                CreatedOn = c.CreatedOn,
                AuthorUserId = c.CreatedBy,
                CategoryName = c.Category != null ? c.Category.CategoryName : null,
                LikeCount = c.Engagements.Count(e => e.EngagementType == "Like"),
                IsRead = c.Engagements.Any(e => e.EngagementType == "Read" && e.UserId == uid)
            }, pageNumber, pageSize);

            var authors = await LoadAuthorsAsync(result.Items.Select(i => i.AuthorUserId));
            foreach (var item in result.Items)
            {
                if (authors.TryGetValue(item.AuthorUserId, out var author))
                {
                    item.AuthorName = author.Name;
                }
            }

            return result;
        }

        // Policy list view: resolves category and owner department, and the caller's
        // acknowledgement state against the CURRENT RevisionNo only.
        private async Task<PagedResult<PolicyListItemDto>> GetPolicyListAsync(
            System.Linq.Expressions.Expression<Func<ContentItem, bool>> predicate,
            int pageNumber, int pageSize)
        {
            int uid = GetLoginUserId();

            return await GetContentListAsync(predicate, c => new PolicyListItemDto
            {
                ContentId = c.ContentId,
                Title = c.Title,
                Body = c.Body,
                RevisionNo = c.RevisionNo,
                OwnerDepartmentId = c.OwnerDepartmentId,
                OwnerDepartmentName = _context.DepartmentMaster
                    .Where(d => d.DepId == c.OwnerDepartmentId)
                    .Select(d => d.DepartmentName)
                    .FirstOrDefault(),
                CategoryName = c.Category != null ? c.Category.CategoryName : null,
                PublishOn = c.PublishOn,
                ExpiresOn = c.ExpiresOn,
                CreatedOn = c.CreatedOn,
                HasAcknowledged = c.Engagements.Any(e => e.EngagementType == "Ack" && e.UserId == uid && e.RevisionNo == c.RevisionNo),
                NeedsAck = !c.Engagements.Any(e => e.EngagementType == "Ack" && e.UserId == uid && e.RevisionNo == c.RevisionNo)
            }, pageNumber, pageSize);
        }

        // Validates that the target content exists and matches the expected type,
        // then saves the uploaded files as attachments.
        private async Task<IActionResult> AttachToContentAsync(int id, string contentType, string label, IEnumerable<IFormFile>? files)
        {
            var fileList = files?.ToList();
            if (fileList == null || fileList.Count == 0)
            {
                return BadRequest(new ApiResponse<string>(0, "No files uploaded.", null));
            }

            var attachmentError = ValidateAttachments(fileList);
            if (attachmentError != null)
            {
                return BadRequest(new ApiResponse<string>(0, attachmentError, null));
            }

            bool exists = await _context.ContentItem
                .AnyAsync(c => c.ContentId == id && c.ContentType == contentType);

            if (!exists)
            {
                return NotFound(new ApiResponse<string>(0, $"{label} not found.", null));
            }

            var attachments = await SaveAttachmentsAsync(id, fileList);
            return Ok(new ApiResponse<List<ContentAttachmentDto>>(1, "Attachments uploaded successfully.", attachments));
        }

        #region Announcements

        [HttpPost("Announcement")]
        [HasPermission("announcements.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CreateContentResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAnnouncement([FromForm] CreateAnnouncementDto dto)
        {
            var attachmentError = ValidateAttachments(dto.Attachments);
            if (attachmentError != null)
            {
                return BadRequest(new ApiResponse<int>(0, attachmentError, 0));
            }

            try
            {
                var p = new DynamicParameters();
                p.Add("@Title", dto.Title);
                p.Add("@Body", dto.Body);
                p.Add("@CategoryId", dto.CategoryId);
                p.Add("@Kind", dto.Kind);
                p.Add("@Priority", dto.Priority);
                p.Add("@IsPinned", dto.IsPinned);
                p.Add("@PublishOn", dto.PublishOn);
                p.Add("@ExpiresOn", dto.ExpiresOn);
                p.Add("@Status", dto.Status);
                p.Add("@CreatedBy", GetLoginUserId());
                p.Add("@Audience", _dataTableService.ToDataTable(dto.Audience).AsTableValuedParameter("dbo.ContentAudienceList"));
                p.Add("@NewContentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var connection = _context.Database.GetDbConnection();
                await connection.ExecuteAsync("sp_InsertAnnouncement", p, commandType: CommandType.StoredProcedure);

                int newContentId = p.Get<int>("@NewContentId");
                var attachments = await SaveAttachmentsAsync(newContentId, dto.Attachments);

                var result = new CreateContentResultDto { ContentId = newContentId, Attachments = attachments };
                return Ok(new ApiResponse<CreateContentResultDto>(1, "Announcement created successfully.", result));
            }
            catch (SqlException ex)
            {
                return BadRequest(new ApiResponse<int>(0, ex.Message, 0));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpGet("Announcement")]
        [HasPermission("announcements.read")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AnnouncementListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAnnouncements([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await ExecutePagedContentSpAsync<AnnouncementListItemDto>("dbo.sp_GetAnnouncements", pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<AnnouncementListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpDelete("Announcement/{id}")]
        [HasPermission("announcements.delete")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            try
            {
                return await ArchiveContentAsync(id, "Announcement", "Announcement");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost("Announcement/{id}/Attachment")]
        [HasPermission("announcements.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<ContentAttachmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddAnnouncementAttachments(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                return await AttachToContentAsync(id, "Announcement", "Announcement", files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists the current user's own draft announcements. Drafts are private: a user can
        // only ever see the drafts they authored, regardless of permissions.
        [HttpGet("Announcement/Drafts")]
        [HasPermission("announcements.write")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AnnouncementListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyDraftAnnouncements([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
                int uid = GetLoginUserId();

                var result = await GetAnnouncementListAsync(
                    c => c.ContentType == "Announcement" && c.Status == "Draft" && c.CreatedBy == uid,
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<AnnouncementListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists archived announcements for moderation. Restricted to HR/Admin via
        // announcements.delete, the same permission that performs the archive action.
        [HttpGet("Announcement/Archived")]
        [HasPermission("announcements.delete")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AnnouncementListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetArchivedAnnouncements([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await GetAnnouncementListAsync(
                    c => c.ContentType == "Announcement" && c.Status == "Archived",
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<AnnouncementListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion

        #region Policies

        [HttpPost("Policy")]
        [HasPermission("policies.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CreateContentResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePolicy([FromForm] CreatePolicyDto dto)
        {
            var attachmentError = ValidateAttachments(dto.Attachments);
            if (attachmentError != null)
            {
                return BadRequest(new ApiResponse<int>(0, attachmentError, 0));
            }

            try
            {
                var p = new DynamicParameters();
                p.Add("@Title", dto.Title);
                p.Add("@Body", dto.Body);
                p.Add("@CategoryId", dto.CategoryId);
                p.Add("@Kind", dto.Kind);
                p.Add("@Priority", dto.Priority);
                p.Add("@IsPinned", dto.IsPinned);
                p.Add("@PublishOn", dto.PublishOn);
                p.Add("@ExpiresOn", dto.ExpiresOn);
                p.Add("@Status", dto.Status);
                p.Add("@OwnerDepartmentId", dto.OwnerDepartmentId);
                p.Add("@CreatedBy", GetLoginUserId());
                p.Add("@Audience", _dataTableService.ToDataTable(dto.Audience).AsTableValuedParameter("dbo.ContentAudienceList"));
                p.Add("@NewContentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var connection = _context.Database.GetDbConnection();
                await connection.ExecuteAsync("sp_InsertPolicy", p, commandType: CommandType.StoredProcedure);

                int newContentId = p.Get<int>("@NewContentId");
                var attachments = await SaveAttachmentsAsync(newContentId, dto.Attachments);

                var result = new CreateContentResultDto { ContentId = newContentId, Attachments = attachments };
                return Ok(new ApiResponse<CreateContentResultDto>(1, "Policy created successfully.", result));
            }
            catch (SqlException ex)
            {
                return BadRequest(new ApiResponse<int>(0, ex.Message, 0));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpGet("Policy")]
        [HasPermission("policies.read")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PolicyListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPolicies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await ExecutePagedContentSpAsync<PolicyListItemDto>("dbo.sp_GetPolicies", pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<PolicyListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpDelete("Policy/{id}")]
        [HasPermission("policies.delete")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePolicy(int id)
        {
            try
            {
                return await ArchiveContentAsync(id, "Policy", "Policy");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost("Policy/{id}/Attachment")]
        [HasPermission("policies.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<ContentAttachmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddPolicyAttachments(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                return await AttachToContentAsync(id, "Policy", "Policy", files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists the current user's own draft policies. Drafts are private: a user can only
        // ever see the drafts they authored, regardless of permissions.
        [HttpGet("Policy/Drafts")]
        [HasPermission("policies.write")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PolicyListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyDraftPolicies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
                int uid = GetLoginUserId();

                var result = await GetPolicyListAsync(
                    c => c.ContentType == "Policy" && c.Status == "Draft" && c.CreatedBy == uid,
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<PolicyListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists archived policies for moderation. Restricted to HR/Admin via
        // policies.delete, the same permission that performs the archive action.
        [HttpGet("Policy/Archived")]
        [HasPermission("policies.delete")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PolicyListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetArchivedPolicies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await GetPolicyListAsync(
                    c => c.ContentType == "Policy" && c.Status == "Archived",
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<PolicyListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion

        #region Posts

        [HttpPost("Post")]
        [HasPermission("posts.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CreateContentResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            var attachmentError = ValidateAttachments(dto.Attachments);
            if (attachmentError != null)
            {
                return BadRequest(new ApiResponse<int>(0, attachmentError, 0));
            }

            try
            {
                var p = new DynamicParameters();
                p.Add("@Body", dto.Body);
                p.Add("@Title", dto.Title);
                p.Add("@Kind", dto.Kind);
                p.Add("@CategoryId", dto.CategoryId);
                p.Add("@IsPinned", dto.IsPinned);
                p.Add("@Status", dto.Status);
                p.Add("@CreatedBy", GetLoginUserId());
                p.Add("@NewContentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var connection = _context.Database.GetDbConnection();
                await connection.ExecuteAsync("sp_InsertPost", p, commandType: CommandType.StoredProcedure);

                int newContentId = p.Get<int>("@NewContentId");
                var attachments = await SaveAttachmentsAsync(newContentId, dto.Attachments);

                var result = new CreateContentResultDto { ContentId = newContentId, Attachments = attachments };
                return Ok(new ApiResponse<CreateContentResultDto>(1, "Post created successfully.", result));
            }
            catch (SqlException ex)
            {
                return BadRequest(new ApiResponse<int>(0, ex.Message, 0));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpGet("Post")]
        [HasPermission("posts.read")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await ExecutePagedContentSpAsync<PostListItemDto>("dbo.sp_GetPostFeed", pageNumber, pageSize);
                await LoadAttachmentsAsync(result.Items);

                return Ok(new ApiResponse<PagedResult<PostListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists the current user's own draft posts. Drafts are private: a user can only
        // ever see the drafts they authored, regardless of permissions.
        [HttpGet("Post/Drafts")]
        [HasPermission("posts.write")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyDraftPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
                int uid = GetLoginUserId();

                var result = await GetPostListAsync(
                    c => c.ContentType == "Post" && c.Status == "Draft" && c.CreatedBy == uid,
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<PostListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Lists archived posts for moderation. Restricted to HR/Admin via posts.delete,
        // the same permission that performs the archive (soft-delete) action.
        [HttpGet("Post/Archived")]
        [HasPermission("posts.delete")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetArchivedPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

                var result = await GetPostListAsync(
                    c => c.ContentType == "Post" && c.Status == "Archived",
                    pageNumber, pageSize);

                return Ok(new ApiResponse<PagedResult<PostListItemDto>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpDelete("Post/{id}")]
        [HasPermission("posts.delete")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                return await ArchiveContentAsync(id, "Post", "Post");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost("Post/{id}/Attachment")]
        [HasPermission("posts.write")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<ContentAttachmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddPostAttachments(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                return await AttachToContentAsync(id, "Post", "Post", files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion

        #region Content (Shared)

        [HttpGet("Content/{id}")]
        [HasPermission("content.read")]
        [ProducesResponseType(typeof(ApiResponse<ContentDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContentDetail(int id)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                using var grid = await connection.QueryMultipleAsync(
                    "dbo.sp_GetContentDetail",
                    new { ContentId = id, UserId = GetLoginUserId() },
                    commandType: CommandType.StoredProcedure);

                var header = await grid.ReadFirstOrDefaultAsync<ContentDetailHeaderDto>();
                if (header == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Content not found.", null));
                }

                var attachments = (await grid.ReadAsync<ContentDetailAttachmentDto>()).ToList();
                var comments = (await grid.ReadAsync<ContentDetailCommentDto>()).ToList();
                var reactions = (await grid.ReadAsync<ContentReactionBreakdownDto>()).ToList();

                var detail = new ContentDetailDto
                {
                    Header = header,
                    Attachments = attachments,
                    Comments = BuildCommentTree(comments),
                    Reactions = reactions
                };

                return Ok(new ApiResponse<ContentDetailDto>(1, "Success", detail));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Assembles the flat comment list from the stored procedure into a threaded tree,
        // preserving the SP's ordering for both top-level comments and their replies.
        private static List<ContentDetailCommentDto> BuildCommentTree(List<ContentDetailCommentDto> flat)
        {
            var byId = flat.ToDictionary(c => c.CommentId);
            var roots = new List<ContentDetailCommentDto>();

            foreach (var comment in flat)
            {
                if (comment.ParentCommentId.HasValue && byId.TryGetValue(comment.ParentCommentId.Value, out var parent))
                {
                    parent.Replies.Add(comment);
                }
                else
                {
                    roots.Add(comment);
                }
            }

            return roots;
        }

        #endregion

        #region Attachments

        // Deletes a single attachment regardless of content type. The required write
        // permission is resolved from the parent content's ContentType and checked
        // programmatically (a static [HasPermission] can't cover all three types).
        [HttpDelete("Attachment/{attachmentId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            try
            {
                var attachment = await _context.ContentAttachment
                    .Include(a => a.Content)
                    .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

                if (attachment == null || attachment.Content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Attachment not found.", null));
                }

                var permission = MapContentTypeToWritePermission(attachment.Content.ContentType);
                if (permission == null)
                {
                    return StatusCode(500, new ApiResponse<string>(0, "Unsupported content type for attachment.", null));
                }

                if (!await _permissionService.HasPermissionAsync(GetLoginUserId(), permission))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse<string>(0, "You do not have permission to delete this attachment.", null));
                }

                await _storageService.DeleteFileAsync(attachment.FileUrl);

                _context.ContentAttachment.Remove(attachment);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>(1, "Attachment deleted successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion

        #region Engagement

        // Loads a non-archived content row of the expected type for engagement actions.
        private async Task<ContentItem?> GetEngageableContentAsync(int id, string contentType)
        {
            return await _context.ContentItem
                .FirstOrDefaultAsync(c => c.ContentId == id && c.ContentType == contentType && c.Status != "Archived");
        }

        // Likes or unlikes a post (toggle). Returns the new like state and total count.
        [HttpPost("Post/{id}/Like")]
        [HasPermission("posts.read")]
        [ProducesResponseType(typeof(ApiResponse<EngagementStateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ToggleLike(int id)
        {
            try
            {
                var content = await GetEngageableContentAsync(id, "Post");
                if (content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Post not found.", null));
                }

                int uid = GetLoginUserId();
                var existing = await _context.ContentEngagement
                    .FirstOrDefaultAsync(e => e.ContentId == id && e.UserId == uid && e.EngagementType == "Like");

                bool isEngaged;
                if (existing != null)
                {
                    _context.ContentEngagement.Remove(existing);
                    isEngaged = false;
                }
                else
                {
                    _context.ContentEngagement.Add(new ContentEngagement
                    {
                        ContentId = id,
                        UserId = uid,
                        EngagementType = "Like",
                        CreatedOn = DateTime.Now
                    });
                    isEngaged = true;
                }

                await _context.SaveChangesAsync();

                int count = await _context.ContentEngagement
                    .CountAsync(e => e.ContentId == id && e.EngagementType == "Like");

                var state = new EngagementStateDto
                {
                    ContentId = id,
                    EngagementType = "Like",
                    IsEngaged = isEngaged,
                    Count = count
                };

                return Ok(new ApiResponse<EngagementStateDto>(1, isEngaged ? "Post liked." : "Post unliked.", state));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Marks an announcement as read for the current user (idempotent).
        [HttpPost("Announcement/{id}/Read")]
        [HasPermission("announcements.read")]
        [ProducesResponseType(typeof(ApiResponse<EngagementStateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAnnouncementRead(int id)
        {
            try
            {
                var content = await GetEngageableContentAsync(id, "Announcement");
                if (content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Announcement not found.", null));
                }

                int uid = GetLoginUserId();
                bool alreadyRead = await _context.ContentEngagement
                    .AnyAsync(e => e.ContentId == id && e.UserId == uid && e.EngagementType == "Read");

                if (!alreadyRead)
                {
                    _context.ContentEngagement.Add(new ContentEngagement
                    {
                        ContentId = id,
                        UserId = uid,
                        EngagementType = "Read",
                        CreatedOn = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                int count = await _context.ContentEngagement
                    .CountAsync(e => e.ContentId == id && e.EngagementType == "Read");

                var state = new EngagementStateDto
                {
                    ContentId = id,
                    EngagementType = "Read",
                    IsEngaged = true,
                    Count = count
                };

                return Ok(new ApiResponse<EngagementStateDto>(1, "Announcement marked as read.", state));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Acknowledges a policy for the current user against its CURRENT revision
        // (idempotent per revision; a new revision requires a fresh acknowledgement).
        [HttpPost("Policy/{id}/Acknowledge")]
        [HasPermission("policies.read")]
        [ProducesResponseType(typeof(ApiResponse<EngagementStateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcknowledgePolicy(int id)
        {
            try
            {
                var content = await GetEngageableContentAsync(id, "Policy");
                if (content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Policy not found.", null));
                }

                int uid = GetLoginUserId();
                int revision = content.RevisionNo;
                bool alreadyAcked = await _context.ContentEngagement
                    .AnyAsync(e => e.ContentId == id && e.UserId == uid && e.EngagementType == "Ack" && e.RevisionNo == revision);

                if (!alreadyAcked)
                {
                    _context.ContentEngagement.Add(new ContentEngagement
                    {
                        ContentId = id,
                        UserId = uid,
                        EngagementType = "Ack",
                        RevisionNo = revision,
                        CreatedOn = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                int count = await _context.ContentEngagement
                    .CountAsync(e => e.ContentId == id && e.EngagementType == "Ack" && e.RevisionNo == revision);

                var state = new EngagementStateDto
                {
                    ContentId = id,
                    EngagementType = "Ack",
                    IsEngaged = true,
                    Count = count
                };

                return Ok(new ApiResponse<EngagementStateDto>(1, "Policy acknowledged.", state));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion

        #region Comments

        // Returns the active comment thread for a content item (top-level comments with
        // nested replies). The required read permission is resolved from the parent
        // content's ContentType and checked programmatically (generic route).
        [HttpGet("Content/{id}/Comments")]
        [ProducesResponseType(typeof(ApiResponse<List<ContentCommentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetComments(int id)
        {
            try
            {
                var content = await _context.ContentItem.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ContentId == id && c.Status != "Archived");

                if (content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Content not found.", null));
                }

                var permission = MapContentTypeToReadPermission(content.ContentType);
                if (permission == null)
                {
                    return StatusCode(500, new ApiResponse<string>(0, "Unsupported content type for comments.", null));
                }

                if (!await _permissionService.HasPermissionAsync(GetLoginUserId(), permission))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse<string>(0, "You do not have permission to view these comments.", null));
                }

                var flat = await _context.ContentComment.AsNoTracking()
                    .Where(cm => cm.ContentId == id && cm.Status == "Active")
                    .OrderBy(cm => cm.CreatedOn)
                    .Select(cm => new ContentCommentDto
                    {
                        CommentId = cm.CommentId,
                        ContentId = cm.ContentId,
                        ParentCommentId = cm.ParentCommentId,
                        Body = cm.Body,
                        CreatedBy = cm.CreatedBy,
                        CreatedOn = cm.CreatedOn,
                        ModifiedOn = cm.ModifiedOn
                    })
                    .ToListAsync();

                var authors = await LoadAuthorsAsync(flat.Select(cm => cm.CreatedBy));
                foreach (var comment in flat)
                {
                    if (authors.TryGetValue(comment.CreatedBy, out var author))
                    {
                        comment.AuthorName = author.Name;
                        comment.AuthorPhotoUrl = author.PhotoUrl;
                    }
                }

                // Build the reply tree. Comments whose parent is missing (e.g. a
                // soft-deleted parent) surface as roots so they aren't lost.
                var byId = flat.ToDictionary(cm => cm.CommentId);
                var roots = new List<ContentCommentDto>();
                foreach (var comment in flat)
                {
                    if (comment.ParentCommentId.HasValue && byId.TryGetValue(comment.ParentCommentId.Value, out var parent))
                    {
                        parent.Replies.Add(comment);
                    }
                    else
                    {
                        roots.Add(comment);
                    }
                }

                return Ok(new ApiResponse<List<ContentCommentDto>>(1, "Success", roots));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Adds a comment (or a reply when ParentCommentId is set) to a content item.
        [HttpPost("Content/{id}/Comment")]
        [ProducesResponseType(typeof(ApiResponse<ContentCommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Body))
                {
                    return BadRequest(new ApiResponse<string>(0, "Body is required.", null));
                }

                var content = await _context.ContentItem.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ContentId == id && c.Status != "Archived");

                if (content == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Content not found.", null));
                }

                var permission = MapContentTypeToReadPermission(content.ContentType);
                if (permission == null)
                {
                    return StatusCode(500, new ApiResponse<string>(0, "Unsupported content type for comments.", null));
                }

                if (!await _permissionService.HasPermissionAsync(GetLoginUserId(), permission))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse<string>(0, "You do not have permission to comment on this content.", null));
                }

                // A reply must point to an active comment on the same content.
                if (dto.ParentCommentId.HasValue)
                {
                    bool parentExists = await _context.ContentComment
                        .AnyAsync(cm => cm.CommentId == dto.ParentCommentId.Value
                                     && cm.ContentId == id
                                     && cm.Status == "Active");

                    if (!parentExists)
                    {
                        return BadRequest(new ApiResponse<string>(0, "Parent comment not found.", null));
                    }
                }

                int uid = GetLoginUserId();
                var comment = new ContentComment
                {
                    ContentId = id,
                    ParentCommentId = dto.ParentCommentId,
                    Body = dto.Body,
                    Status = "Active",
                    CreatedBy = uid,
                    CreatedOn = DateTime.Now
                };

                _context.ContentComment.Add(comment);
                await _context.SaveChangesAsync();

                var authors = await LoadAuthorsAsync(new[] { uid });
                authors.TryGetValue(uid, out var author);

                var result = new ContentCommentDto
                {
                    CommentId = comment.CommentId,
                    ContentId = comment.ContentId,
                    ParentCommentId = comment.ParentCommentId,
                    Body = comment.Body,
                    CreatedBy = comment.CreatedBy,
                    AuthorName = author.Name,
                    AuthorPhotoUrl = author.PhotoUrl,
                    CreatedOn = comment.CreatedOn,
                    ModifiedOn = comment.ModifiedOn
                };

                return Ok(new ApiResponse<ContentCommentDto>(1, "Comment added successfully.", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Edits a comment's body. Allowed for the comment's author only.
        [HttpPut("Comment/{commentId}")]
        [ProducesResponseType(typeof(ApiResponse<EditCommentResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditComment(int commentId, [FromBody] EditCommentDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Body))
                {
                    return BadRequest(new ApiResponse<string>(0, "Body is required.", null));
                }

                var comment = await _context.ContentComment
                    .FirstOrDefaultAsync(cm => cm.CommentId == commentId);

                if (comment == null || comment.Status == "Deleted")
                {
                    return NotFound(new ApiResponse<string>(0, "Comment not found.", null));
                }

                int uid = GetLoginUserId();
                if (comment.CreatedBy != uid)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse<string>(0, "You can only edit your own comment.", null));
                }

                comment.Body = dto.Body;
                comment.ModifiedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                var result = new EditCommentResultDto
                {
                    CommentId = comment.CommentId,
                    Body = comment.Body,
                    ModifiedOn = comment.ModifiedOn
                };

                return Ok(new ApiResponse<EditCommentResultDto>(1, "Comment updated successfully.", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Soft-deletes a comment (sets Status = "Deleted"). Allowed for the comment's
        // author or any user with the parent content's delete permission.
        [HttpDelete("Comment/{commentId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var comment = await _context.ContentComment
                    .Include(cm => cm.Content)
                    .FirstOrDefaultAsync(cm => cm.CommentId == commentId);

                if (comment == null || comment.Content == null || comment.Status == "Deleted")
                {
                    return NotFound(new ApiResponse<string>(0, "Comment not found.", null));
                }

                int uid = GetLoginUserId();
                bool isAuthor = comment.CreatedBy == uid;

                if (!isAuthor)
                {
                    var permission = MapContentTypeToDeletePermission(comment.Content.ContentType);
                    if (permission == null)
                    {
                        return StatusCode(500, new ApiResponse<string>(0, "Unsupported content type for comment.", null));
                    }

                    if (!await _permissionService.HasPermissionAsync(uid, permission))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new ApiResponse<string>(0, "You do not have permission to delete this comment.", null));
                    }
                }

                comment.Status = "Deleted";
                comment.ModifiedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>(1, "Comment deleted successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        #endregion
    }
}
