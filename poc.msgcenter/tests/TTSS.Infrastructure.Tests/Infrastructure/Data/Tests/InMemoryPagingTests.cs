using FluentAssertions;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Data.Tests
{
    public class InMemoryPagingTests : XUnitTestBase
    {
        private readonly InMemoryRepository<SimpleDataModel, int> sut;

        public InMemoryPagingTests(ITestOutputHelper testOutput) : base(testOutput)
            => sut = new InMemoryRepository<SimpleDataModel, int>(it => it.Id);

        #region No contents

        [Fact]
        public async Task GetPaging_WhenNoData_ThenTheSystemShouldNotError()
            => await validatePagingResult(0, 5, 0, 0, 0, false, false, 0, 0);

        [Fact]
        public async Task GetPaging_WhenNoData_WithTheSecondPage_ThenTheSystemShouldNotError()
            => await validatePagingResult(0, 5, 1, 0, 0, true, false, 0, 0);

        [Fact]
        public async Task GetPaging_WhenNoData_WithTheThirdPage_ThenTheSystemShouldNotError()
            => await validatePagingResult(0, 5, 2, 0, 0, true, false, 0, 0);

        #endregion

        #region Get 1st page

        [Fact]
        public async Task GetPaging_WhenDataAreLessThanPageSize()
            => await validatePagingResult(3, 5, 0, 0, 0, false, false, 1, 3);

        [Fact]
        public async Task GetPaging_WhenDataAreEqualWithPageSize()
            => await validatePagingResult(5, 5, 0, 0, 0, false, false, 1, 5);

        [Fact]
        public async Task GetPaging_WhenDataAreMoreThanPageSize()
            => await validatePagingResult(7, 5, 0, 0, 1, false, true, 2, 5);

        #endregion

        #region Get 2nd page

        [Fact]
        public async Task GetPaging_WithTheSecondPage_ThatHasLessThanPageSize()
           => await validatePagingResult(7, 5, 1, 0, 1, true, false, 2, 2);

        [Fact]
        public async Task GetPaging_WithTheSecondPage_ThatHasEqualWithPageSize()
            => await validatePagingResult(10, 5, 1, 0, 1, true, false, 2, 5);

        [Fact]
        public async Task GetPaging_WithTheSecondPage_ThatHasMoreThanPageSize()
            => await validatePagingResult(13, 5, 1, 0, 2, true, true, 3, 5);

        #endregion

        #region Get 3rd page

        [Fact]
        public async Task GetPaging_WithTheThirdPage_ThatHasLessThanPageSize()
            => await validatePagingResult(13, 5, 2, 1, 2, true, false, 3, 3);

        [Fact]
        public async Task GetPaging_WithTheThirdPage_ThatHasEqualWithPageSize()
            => await validatePagingResult(15, 5, 2, 1, 2, true, false, 3, 5);

        [Fact]
        public async Task GetPaging_WithTheThirdPage_ThatHasMoreThanPageSize()
            => await validatePagingResult(30, 5, 2, 1, 3, true, true, 6, 5);

        #endregion

        private async Task validatePagingResult(int contents, int pageSize, int getPageNo,
            int expectedPrevPage, int expectedNextPage,
            bool expectedHasPrevPage, bool expectedHasNextPage,
            int expectedPageCount, int expectedDataElements)
        {
            var records = Enumerable.Range(1, contents)
                .Select(it => new SimpleDataModel { Id = it, Name = it.ToString() });

            foreach (var item in records)
            {
                await sut.InsertAsync(item);
            }

            var paging = sut.Get().ToPaging(totalCount: true, pageSize);
            var pagingResult = paging.GetPage(getPageNo);
            pagingResult.CurrentPage.Should().Be(getPageNo);
            pagingResult.PreviousPage.Should().Be(expectedPrevPage);
            pagingResult.NextPage.Should().Be(expectedNextPage);
            pagingResult.HasPreviousPage.Should().Be(expectedHasPrevPage);
            pagingResult.HasNextPage.Should().Be(expectedHasNextPage);
            pagingResult.TotalCount.Should().Be(contents);
            pagingResult.PageCount.Should().Be(expectedPageCount);
            (await pagingResult.GetDataAsync()).Should().HaveCount(expectedDataElements);

            var pagingData = await pagingResult.ToPagingData();
            pagingData.CurrentPage.Should().Be(getPageNo);
            pagingData.PreviousPage.Should().Be(expectedPrevPage);
            pagingData.NextPage.Should().Be(expectedNextPage);
            pagingData.HasPreviousPage.Should().Be(expectedHasPrevPage);
            pagingData.HasNextPage.Should().Be(expectedHasNextPage);
            pagingData.TotalCount.Should().Be(contents);
            pagingData.PageCount.Should().Be(expectedPageCount);
            pagingData.Result.Should().HaveCount(expectedDataElements);
            pagingData.Result.Should().BeEquivalentTo((await pagingResult.GetDataAsync()).ToList());
        }
    }
}
