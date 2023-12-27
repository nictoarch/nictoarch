using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Nictoarch.Modelling.Db
{
    public sealed class DbSourceFactory : ISourceFactory<DbSourceConfig, DbSource, DbExtractConfig>
    {
        string ISourceFactory.Name => "db";

        IEnumerable<ITypeDiscriminator> ISourceFactory.GetYamlTypeDiscriminators()
        {
            //nothing to do
            yield break;
        }

        async Task<ISource> ISourceFactory<DbSourceConfig, DbSource, DbExtractConfig>.GetSource(DbSourceConfig sourceConfig, CancellationToken cancellationToken)
        {
            DbConnection connection = this.CreateConnectionClass();
            connection.ConnectionString = sourceConfig.connection_string;
            await connection.OpenAsync(cancellationToken);
            DbSource source = new DbSource(connection);
            return source;
        }

        private DbConnection CreateConnectionClass()
        {
            //TODO: implement properly
            return new System.Data.SqlClient.SqlConnection();
        }
    }
}
