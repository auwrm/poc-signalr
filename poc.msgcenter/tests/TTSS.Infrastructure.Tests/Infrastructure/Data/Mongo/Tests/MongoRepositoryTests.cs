using AutoFixture.Xunit2;
using FluentAssertions;
using Mongo2Go;
using MongoDB.Driver;
using TTSS.Infrastructure.Models;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Data.Mongo.Tests
{
    public class MongoRepositoryTests : XUnitTestBase, IDisposable
    {
        private readonly MongoDbRunner dbRunner;

        public MongoRepositoryTests(ITestOutputHelper testOutput) : base(testOutput)
            => dbRunner = MongoDbRunner.Start();

        #region Insert

        [Theory]
        [InlineAutoData(1, "One")]
        [InlineAutoData(2, "")]
        [InlineAutoData(3, null)]
        public async Task Insert_WithDefaultCollectionName_ShouldSaveDataAsItBe(string id, string name)
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);

            var data = new SimpleMongoDocument
            {
                Id = id,
                Name = name,
            };
            await sut.InsertAsync(data);
            var actual = await sut.GetByIdAsync(id);
            actual.Should().BeEquivalentTo(data);
            actual.Id.Should().Be(id);
            actual.Name.Should().Be(name);

            sut.Get().Should().HaveCount(1);
        }

        [Theory]
        [InlineAutoData(1, "One")]
        [InlineAutoData(2, "")]
        [InlineAutoData(3, null)]
        public async Task Insert_WithCustomCollectionName_ShouldSaveDataAsItBe(string id, string name)
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>("simple", noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            var data = new SimpleMongoDocument
            {
                Id = id,
                Name = name,
            };
            await insertAndValidate(data, id, sut, 1);
        }

        [Theory]
        [InlineAutoData(1, 2)]
        public async Task Insert_Discriminator_ShouldSaveDataAsItBe(string firstId, string secondId, string name, int schoolId)
        {
            var collectionName = Guid.NewGuid().ToString();
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<StudentMongoDocument>(collectionName, isChild: true)
                .RegisterCollection<SimpleMongoDocument>(collectionName)
                .Build();

            var simpleSut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            var simpleData = new SimpleMongoDocument
            {
                Id = firstId,
                Name = name,
            };
            await insertAndValidate(simpleData, firstId, simpleSut, 1);

            var studentSut = new MongoRepository<StudentMongoDocument, string>(store, it => it.Id);
            var studentData = new StudentMongoDocument
            {
                Id = secondId,
                Name = name,
                SchoolId = schoolId,
            };
            await insertAndValidate(studentData, secondId, studentSut, 1);

            simpleSut.Get().Should().HaveCount(2);
        }

        [Fact]
        public async Task Insert_WithNullId_ThenItMustNotThrowAnError()
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>()
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = null, Name = Guid.NewGuid().ToString() });
        }

        [Fact]
        public async Task Insert_WithDuplicateKey_ThenSystemMustThrowAnException()
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>()
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            var insertion = async () =>
            {
                var data = new SimpleMongoDocument
                {
                    Id = "DUPLICATE_KEY",
                    Name = Guid.NewGuid().ToString(),
                };
                await sut.InsertAsync(data);
                await sut.InsertAsync(data);
            };
            await insertion.Should()
                .ThrowAsync<MongoWriteException>()
                .Where(it => it.Message.Contains("DuplicateKey"));
        }

        private async Task insertAndValidate<T, K>(T data, K id, MongoRepository<T, K> sut, int expectedCount)
            where T : IDbModel<K>
        {
            await sut.InsertAsync(data);
            var actual = await sut.GetByIdAsync(id);
            actual.Should().BeEquivalentTo(data);
            sut.Get().Should().HaveCount(expectedCount);
        }

        #endregion

        #region Update

        [Theory]
        [InlineAutoData(1, "One", "1")]
        [InlineAutoData(2, "", "empty")]
        [InlineAutoData(3, null, "null")]
        public async Task Update_ShouldChangeTheRightObject(string id, string name, string newName)
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = id, Name = name, });

            var operationResult = await sut.UpdateAsync(id, new SimpleMongoDocument { Id = id, Name = newName });
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync(id);
            actual.Id.Should().Be(id);
            actual.Name.Should().Be(newName);

            sut.Get().Should().HaveCount(1);
        }

        [Fact]
        public async Task Update_WithMismatchId_Then_NothingChanged()
        {
            const string Id = "1";
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = Id, Name = "One" });

            const string TargetUpdateId = "999";
            var operationResult = await sut.UpdateAsync(TargetUpdateId, new SimpleMongoDocument
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
            const string Id = "1";
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = Id, Name = "One" });

            var operationResult = await sut.DeleteAsync(Id);
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync(Id);
            actual.Should().BeNull();

            sut.Get().Should().BeEmpty();
        }

        [Fact]
        public async Task Delete_WithMismatchId_Then_NothingChanged()
        {
            const string Id = "1";
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = Id, Name = "One" });


            const string TargetUpdateId = "999";
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
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
                .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
                .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = "1", Name = "One" });
            await sut.InsertAsync(new SimpleMongoDocument { Id = "2", Name = "Two" });
            await sut.InsertAsync(new SimpleMongoDocument { Id = "3", Name = "Three" });

            var operationResult = await sut.DeleteManyAsync(it => it.Name.ToLower().Contains("o"));
            operationResult.Should().BeTrue();

            var actual = await sut.GetByIdAsync("3");
            actual.Id.Should().Be("3");
            actual.Name.Should().Be("Three");

            sut.Get().Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteMany_WithMismatchId_Then_NothingChanged()
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
               .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
               .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = "1", Name = "One" });
            await sut.InsertAsync(new SimpleMongoDocument { Id = "2", Name = "Two" });
            await sut.InsertAsync(new SimpleMongoDocument { Id = "3", Name = "Three" });

            var operationResult = await sut.DeleteManyAsync(it => it.Name.Contains("NONE"));
            operationResult.Should().BeFalse();

            sut.Get().Should().HaveCount(3);
        }

        #endregion

        #region Upsert

        [Fact]
        public async Task Upsert_ShouldWorkForBothExistAndNew()
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
               .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
               .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            await sut.InsertAsync(new SimpleMongoDocument { Id = "1", Name = "One" });

            await sut.UpsertAsync("1", new SimpleMongoDocument
            {
                Id = "1",
                Name = "1",
            });
            await sut.UpsertAsync("2", new SimpleMongoDocument
            {
                Id = "2",
                Name = "Two",
            });

            var d1 = await sut.GetByIdAsync("1");
            d1.Id.Should().Be("1");
            d1.Name.Should().Be("1");

            var d2 = await sut.GetByIdAsync("2");
            d2.Id.Should().Be("2");
            d2.Name.Should().Be("Two");

            sut.Get().Should().HaveCount(2);
        }

        #endregion

        #region InsertBulk

        [Fact]
        public async Task InsertBulk_ShouldWorkAsExpected()
        {
            var store = new MongoConnectionStoreBuilder(Guid.NewGuid().ToString(), dbRunner.ConnectionString)
               .RegisterCollection<SimpleMongoDocument>(noDiscriminator: true)
               .Build();
            var sut = new MongoRepository<SimpleMongoDocument, string>(store, it => it.Id);
            var qry = Enumerable.Range(1, 100)
                .Select(it => new SimpleMongoDocument
                {
                    Id = it.ToString(),
                    Name = it.ToString()
                });
            await sut.InsertBulkAsync(qry);
            var actual = sut.Get();
            actual.Should().HaveCount(100);
            actual.Should().BeEquivalentTo(qry);
        }

        #endregion

        public void Dispose()
            => dbRunner.Dispose();
    }
}
