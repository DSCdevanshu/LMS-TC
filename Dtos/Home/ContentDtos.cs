using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TCBackend.Dtos.Home
{
    // ===== Audience row -> maps to the dbo.ContentAudienceList table-valued parameter =====
    public class AudienceTargetDto
    {
        // ALL | COMPANY | LOCATION | DEPARTMENT | USER
        [Required(ErrorMessage = "TargetType is required.")]
        public string TargetType { get; set; } = "ALL";

        // Must be null for ALL, set for everything else (enforced by the SP).
        public int? TargetId { get; set; }
    }

    // ===== Input: sp_InsertAnnouncement =====
    public class CreateAnnouncementDto
    {
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = null!;
        public string? Body { get; set; }
        public int? CategoryId { get; set; }
        public string? Kind { get; set; }
        public string Priority { get; set; } = "Normal";
        public bool IsPinned { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public string Status { get; set; } = "Draft";

        // Empty/null => the SP defaults the audience to ALL.
        public List<AudienceTargetDto>? Audience { get; set; }

        // Optional files posted alongside the form (multipart/form-data).
        public List<IFormFile>? Attachments { get; set; }
    }

    // ===== Input: sp_InsertPolicy =====
    public class CreatePolicyDto
    {
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = null!;
        public string? Body { get; set; }
        public int? CategoryId { get; set; }
        public string? Kind { get; set; }
        public string Priority { get; set; } = "Normal";
        public bool IsPinned { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public string Status { get; set; } = "Draft";
        public int? OwnerDepartmentId { get; set; }

        // Empty/null => the SP defaults the audience to ALL.
        public List<AudienceTargetDto>? Audience { get; set; }

        // Optional files posted alongside the form (multipart/form-data).
        public List<IFormFile>? Attachments { get; set; }
    }

    // ===== Input: sp_InsertPost (global feed, no audience) =====
    public class CreatePostDto
    {
        [Required(ErrorMessage = "Body is required.")]
        public string Body { get; set; } = null!;
        public string? Title { get; set; }
        public string? Kind { get; set; }
        public int? CategoryId { get; set; }
        public bool IsPinned { get; set; }
        public string Status { get; set; } = "Published";

        // Optional files posted alongside the form (multipart/form-data).
        public List<IFormFile>? Attachments { get; set; }
    }

    // ===== Output: a saved attachment row =====
    public class ContentAttachmentDto
    {
        public int AttachmentId { get; set; }
        public int ContentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public string? ContentKind { get; set; }
        public long? FileSizeBytes { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; }
    }

    // ===== Output: result of a content-create call =====
    public class CreateContentResultDto
    {
        public int ContentId { get; set; }
        public List<ContentAttachmentDto> Attachments { get; set; } = new();
    }

    // ===== Implemented by list DTOs whose SP carries a per-row TotalCount window column =====
    public interface IPagedRow
    {
        int TotalCount { get; }
    }

    // ===== Implemented by list DTOs that expose attachments loaded separately (EF) =====
    public interface IAttachmentCarrier
    {
        int ContentId { get; }
        List<ContentAttachmentDto> Attachments { get; set; }
    }

    // ===== Generic paged result envelope for list/view endpoints =====
    public class PagedResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
    }

    // ===== Output: Announcement list view (maps to dbo.sp_GetAnnouncements) =====
    public class AnnouncementListItemDto : IPagedRow
    {
        public int ContentId { get; set; }
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Priority { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public int LikeCount { get; set; }
        public bool IsRead { get; set; }

        // Used by the EF draft/archived lists to resolve AuthorName; the SP path
        // populates AuthorName directly, so this stays out of the JSON payload.
        [JsonIgnore]
        public int AuthorUserId { get; set; }

        // Per-row window total from the SP; not part of the JSON payload.
        [JsonIgnore]
        public int TotalCount { get; set; }
    }

    // ===== Output: Policy list view (maps to dbo.sp_GetPolicies) =====
    public class PolicyListItemDto : IPagedRow
    {
        public int ContentId { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public int RevisionNo { get; set; }
        public int? OwnerDepartmentId { get; set; }
        public string? OwnerDepartmentName { get; set; }
        public string? CategoryName { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public DateTime CreatedOn { get; set; }

        // Acknowledgement is tracked against the CURRENT RevisionNo only.
        public bool HasAcknowledged { get; set; }
        public bool NeedsAck { get; set; }

        // Per-row window total from the SP; not part of the JSON payload.
        [JsonIgnore]
        public int TotalCount { get; set; }
    }

    // ===== Output: Post list view / feed item (maps to dbo.sp_GetPostFeed) =====
    public class PostListItemDto : IPagedRow, IAttachmentCarrier
    {
        public int ContentId { get; set; }
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorPhoto { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public string? MyReaction { get; set; }
        public bool ILiked { get; set; }

        // Attachments are loaded from ContentAttachment (EF) after the feed SP runs.
        public List<ContentAttachmentDto> Attachments { get; set; } = new();

        [JsonIgnore]
        public int TotalCount { get; set; }
    }

    // ===== Output: a single comment, with nested replies =====
    public class ContentCommentDto
    {
        public int CommentId { get; set; }
        public int ContentId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Body { get; set; } = null!;
        public int CreatedBy { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorPhotoUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public List<ContentCommentDto> Replies { get; set; } = new();
    }

    // ===== Input: add a comment (or a reply when ParentCommentId is set) =====
    public class CreateCommentDto
    {
        [Required(ErrorMessage = "Body is required.")]
        public string Body { get; set; } = null!;
        public int? ParentCommentId { get; set; }
    }

    // ===== Input: edit an existing comment (author only) =====
    public class EditCommentDto
    {
        [Required(ErrorMessage = "Body is required.")]
        public string Body { get; set; } = null!;
    }

    // ===== Output: result of a comment edit (author is unchanged, so it is omitted) =====
    public class EditCommentResultDto
    {
        public int CommentId { get; set; }
        public string Body { get; set; } = null!;
        public DateTime? ModifiedOn { get; set; }
    }

    // ===== Output: result of an engagement toggle (like/read/ack) =====
    public class EngagementStateDto
    {
        public int ContentId { get; set; }
        public string EngagementType { get; set; } = null!;
        public bool IsEngaged { get; set; }
        public int Count { get; set; }
    }

    // ===== Output: full content detail (maps to dbo.sp_GetContentDetail, 4 result sets) =====
    public class ContentDetailDto
    {
        public ContentDetailHeaderDto Header { get; set; } = null!;
        public List<ContentDetailAttachmentDto> Attachments { get; set; } = new();
        public List<ContentDetailCommentDto> Comments { get; set; } = new();
        public List<ContentReactionBreakdownDto> Reactions { get; set; } = new();
    }

    // Result set 1: header
    public class ContentDetailHeaderDto
    {
        public int ContentId { get; set; }
        public string ContentType { get; set; } = null!;
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Priority { get; set; }
        public bool IsPinned { get; set; }
        public string Status { get; set; } = null!;
        public int RevisionNo { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public int? OwnerDepartmentId { get; set; }
        public string? OwnerDepartmentName { get; set; }
        public int AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorPhoto { get; set; }
        public DateTime CreatedOn { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public string? MyReaction { get; set; }
    }

    // Result set 2: attachments
    public class ContentDetailAttachmentDto
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public string? ContentKind { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime UploadedOn { get; set; }
    }

    // Result set 3: comments (flat from SP; assembled into a tree by the controller)
    public class ContentDetailCommentDto
    {
        public int CommentId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Body { get; set; } = null!;
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorPhoto { get; set; }
        public List<ContentDetailCommentDto> Replies { get; set; } = new();
    }

    // Result set 4: reaction breakdown
    public class ContentReactionBreakdownDto
    {
        public string? ReactionType { get; set; }
        public int Cnt { get; set; }
    }
}
