using sobee_API.Domain;
using sobee_API.DTOs.Reviews;

namespace sobee_API.Services.Interfaces;

public interface IReviewService
{
    Task<ServiceResult<ReviewListResponseDto>> GetReviewsAsync(int productId, int page, int pageSize);
    Task<ServiceResult<ReviewCreatedResponseDto>> CreateReviewAsync(
        int productId,
        string? userId,
        string? sessionId,
        CreateReviewRequest request);
    Task<ServiceResult<ReplyCreatedResponseDto>> CreateReplyAsync(
        int reviewId,
        string? userId,
        bool isAdmin,
        CreateReplyRequest request);
    Task<ServiceResult<ReviewDeletedResponseDto>> DeleteReviewAsync(int reviewId, string? userId, bool isAdmin);
    Task<ServiceResult<ReplyDeletedResponseDto>> DeleteReplyAsync(int replyId, string? userId, bool isAdmin);
}
