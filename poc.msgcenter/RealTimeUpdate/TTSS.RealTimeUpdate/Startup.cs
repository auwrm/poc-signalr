using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using System;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.RealTimeUpdate.Services.DbModels;

namespace TTSS.RealTimeUpdate
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
            builder.Services.AddSingleton<IMongoRepository<MessageInfo, string>, MongoRepository<MessageInfo, string>>(pvd =>
            {
                // HACK: Local MongoDB just for test purposes. (MUST BE DESIGN FOR Dev/Release LATER)
                var dbRunner = MongoDbRunner.Start();
                var dbName = Guid.NewGuid().ToString();
                var store = new MongoConnectionStoreBuilder(dbName, dbRunner.ConnectionString)
                    .RegisterCollection<MessageInfo>(noDiscriminator: true)
                    .Build();
                return new MongoRepository<MessageInfo, string>(store, it => it.Id);
            });
        }
    }
}
