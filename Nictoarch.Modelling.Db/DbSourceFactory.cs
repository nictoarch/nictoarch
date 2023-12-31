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
            DbConnection connection = this.CreateConnectionClass(sourceConfig);
            try
            {
                connection.ConnectionString = sourceConfig.connection_string;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set connection string: {ex.Message}.\nString was: '{sourceConfig.connection_string}'", ex);
            }

            try
            {
                await connection.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to {sourceConfig.connection}: {ex.Message}.\nConnection string string was: '{sourceConfig.connection_string}'", ex);
            }
            DbSource source = new DbSource(connection);
            return source;
        }

        private DbConnection CreateConnectionClass(DbSourceConfig sourceConfig)
        {
            //TODO: maybe use some dll loading or other way to get other connections
            // currently, don't want to enumerate all possible dlls in folder just to get all DbConnection implementations
            
            switch (sourceConfig.connection)
            {
            case nameof(Npgsql.NpgsqlConnection):
                return new Npgsql.NpgsqlConnection();
            case nameof(MySqlConnector.MySqlConnection):
                return new MySqlConnector.MySqlConnection();
            case nameof(System.Data.SqlClient.SqlConnection):
                return new System.Data.SqlClient.SqlConnection();
            default:
                throw new Exception($"Unexpected '{nameof(sourceConfig.connection)}' value '{sourceConfig.connection}'. Only following connections are currently supported: {nameof(Npgsql.NpgsqlConnection)}, {nameof(MySqlConnector.MySqlConnection)}, {nameof(System.Data.SqlClient.SqlConnection)}");
            }
        }
    }
}
