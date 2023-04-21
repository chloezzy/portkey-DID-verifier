﻿using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace CAVerifierServer.MongoDB;

[DependsOn(
    typeof(CAVerifierServerTestBaseModule),
    typeof(CAVerifierServerMongoDbModule)
    )]
public class CAVerifierServerMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var stringArray = CAVerifierServerMongoDbFixture.ConnectionString.Split('?');
        var connectionString = stringArray[0].EnsureEndsWith('/') +
                                   "Db_" +
                               Guid.NewGuid().ToString("N") + "/?" + stringArray[1];

        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = connectionString;
        });
    }
}
