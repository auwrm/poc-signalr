using FluentAssertions;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Data.Tests
{
    public class InMemoryRepositoryTests : XUnitTestBase
    {
        private readonly InMemoryRepository<SimpleDataModel, int> sut;

        public InMemoryRepositoryTests(ITestOutputHelper testOutput) : base(testOutput)
            => sut = new InMemoryRepository<SimpleDataModel, int>(it => it.Id);

        #region Insert

        [Theory]
        [InlineData(1, "One")]
        [InlineData(2, "")]
        [InlineData(3, null)]
        public async Task Insert_ShouldSaveDataAsItBe(int id, string name)
        {
            var data = new SimpleDataModel { Id = id, Name = name };
            await sut.InsertAsync(data);
            var actual = await sut.GetByIdAsync(id);
            actual.Should().Be(data);
            actual.Id.Should().Be(id);
            actual.Name.Should().Be(name);
            sut.Get().Should().HaveCount(1);
        }

        #endregion

        #region Update

        [Theory]
        [InlineData(1, "One", "1")]
        [InlineData(2, "", "empty")]
        [InlineData(3, null, "null")]
        public async Task Update_ShouldChangeTheRightObject(int id, string name, string newName)
        {
            await sut.InsertAsync(new SimpleDataModel { Id = id, Name = name });
            var operationResult = await sut.UpdateAsync(id, new SimpleDataModel { Id = id, Name = newName });
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync(id);
            actual.Id.Should().Be(id);
            actual.Name.Should().Be(newName);

            sut.Get().Should().HaveCount(1);
        }

        [Fact]
        public async Task Update_WithMismatchId_Then_NothingChanged()
        {
            const int Id = 1;
            await sut.InsertAsync(new SimpleDataModel { Id = Id, Name = "One" });

            const int TargetUpdateId = 999;
            var operationResult = await sut.UpdateAsync(TargetUpdateId, new SimpleDataModel
            {
                Id = Id,
                Name = "Anything"
            });
            operationResult.Should().BeFalse();

            var actual = await sut.GetByIdAsync(Id);
            actual.Id.Should().Be(Id);
            actual.Name.Should().Be("One");

            var notfound = await sut.GetByIdAsync(TargetUpdateId);
            notfound.Should().BeNull();

            sut.Get().Should().HaveCount(1);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_TheSelectedItem_MustDismiss()
        {
            const int Id = 1;
            var data = new SimpleDataModel { Id = Id, Name = "One" };
            await sut.InsertAsync(data);

            var operationResult = await sut.DeleteAsync(Id);
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync(Id);
            actual.Should().BeNull();

            sut.Get().Should().BeEmpty();
        }

        [Fact]
        public async Task Delete_WithMismatchId_Then_NothingChanged()
        {
            const int Id = 1;
            await sut.InsertAsync(new SimpleDataModel { Id = Id, Name = "One" });

            const int TargetUpdateId = 999;
            var operationResult = await sut.DeleteAsync(TargetUpdateId);
            operationResult.Should().BeFalse();

            var actual = await sut.GetByIdAsync(Id);
            actual.Id.Should().Be(Id);
            actual.Name.Should().Be("One");

            sut.Get().Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteMany_TheMatchedItems_MustDismiss()
        {
            await sut.InsertAsync(new SimpleDataModel { Id = 1, Name = "One" });
            await sut.InsertAsync(new SimpleDataModel { Id = 2, Name = "Two" });
            await sut.InsertAsync(new SimpleDataModel { Id = 3, Name = "Three" });

            var operationResult = await sut.DeleteManyAsync(it => it.Id < 3);
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync(3);
            actual.Id.Should().Be(3);
            actual.Name.Should().Be("Three");

            sut.Get().Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteMany_WithMismatchId_Then_NothingChanged()
        {
            await sut.InsertAsync(new SimpleDataModel { Id = 1, Name = "One" });
            await sut.InsertAsync(new SimpleDataModel { Id = 2, Name = "Two" });
            await sut.InsertAsync(new SimpleDataModel { Id = 3, Name = "Three" });

            var operationResult = await sut.DeleteManyAsync(it => it.Id > 100);
            operationResult.Should().BeFalse();

            sut.Get().Should().HaveCount(3);
        }

        #endregion

        #region Upsert

        [Fact]
        public async Task Upsert_ShouldWorkForBothExistAndNew()
        {
            await sut.InsertAsync(new SimpleDataModel { Id = 1, Name = "One" });

            await sut.UpsertAsync(1, new SimpleDataModel
            {
                Id = 1,
                Name = "1",
            });
            await sut.UpsertAsync(2, new SimpleDataModel
            {
                Id = 2,
                Name = "Two",
            });

            var d1 = await sut.GetByIdAsync(1);
            d1.Id.Should().Be(1);
            d1.Name.Should().Be("1");

            var d2 = await sut.GetByIdAsync(2);
            d2.Id.Should().Be(2);
            d2.Name.Should().Be("Two");

            sut.Get().Should().HaveCount(2);
        }

        #endregion

        #region Query

        [Fact]
        public async Task Query_UsingCondition_MustGetAllMatchedItems()
        {
            await sut.InsertAsync(new SimpleDataModel { Id = 1, Name = "One" });
            await sut.InsertAsync(new SimpleDataModel { Id = 2, Name = "Two" });
            await sut.InsertAsync(new SimpleDataModel { Id = 3, Name = "Three" });

            var actual = sut.Get(it => it.Id >= 2);
            actual.Should().HaveCount(2);

            actual.Select(it => it.Name).Should().BeEquivalentTo("Two", "Three");
        }

        [Fact]
        public async Task Query_UsingQuery_MustGetAllMatchedItems()
        {
            await sut.InsertAsync(new SimpleDataModel { Id = 1, Name = "One" });
            await sut.InsertAsync(new SimpleDataModel { Id = 2, Name = "Two" });
            await sut.InsertAsync(new SimpleDataModel { Id = 3, Name = "Three" });

            var qry = from it in sut.Query()
                      where it.Name.StartsWith("T")
                      select it.Name;

            qry.Should().BeEquivalentTo("Two", "Three");
        }

        #endregion
    }
}
