using webclothes.Policies;

namespace WebClothes.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void ReviewSubmissionPolicy_Rejects_When_UserHasNotOrdered()
        {
            var error = ReviewSubmissionPolicy.GetValidationError(
                hasOrdered: false,
                hasReviewed: false,
                rating: 5,
                comment: "San pham rat dep");

            Assert.Equal("Ban chi co the danh gia san pham sau khi da mua va nhan hang thanh cong!", error);
        }

        [Fact]
        public void ReviewSubmissionPolicy_Rejects_DuplicateReview()
        {
            var error = ReviewSubmissionPolicy.GetValidationError(
                hasOrdered: true,
                hasReviewed: true,
                rating: 5,
                comment: "Muon gui lai review");

            Assert.Equal("Ban da danh gia san pham nay roi.", error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void ReviewSubmissionPolicy_Rejects_InvalidRating(int rating)
        {
            var error = ReviewSubmissionPolicy.GetValidationError(
                hasOrdered: true,
                hasReviewed: false,
                rating: rating,
                comment: "Hop le");

            Assert.Equal("So sao danh gia phai tu 1 den 5.", error);
        }

        [Fact]
        public void ReviewSubmissionPolicy_Allows_ValidSubmission()
        {
            var error = ReviewSubmissionPolicy.GetValidationError(
                hasOrdered: true,
                hasReviewed: false,
                rating: 5,
                comment: "  Chat lieu tot  ");

            Assert.Null(error);
            Assert.True(ReviewSubmissionPolicy.CanSubmit(hasOrdered: true, hasReviewed: false));
            Assert.False(ReviewSubmissionPolicy.CanSubmit(hasOrdered: true, hasReviewed: true));
        }
    }
}
