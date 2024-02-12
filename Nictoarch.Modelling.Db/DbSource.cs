using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using Nictoarch.Modelling.Core;

namespace Nictoarch.Modelling.Db
{
    internal sealed class DbSource: ISource<DbExtractConfig>
    {
        private readonly DbConnection m_connection;

        internal DbSource(DbConnection connection)
        {
            this.m_connection = connection;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return this.m_connection.DisposeAsync();
        }

        async Task<JToken> ISource<DbExtractConfig>.Extract(DbExtractConfig extractConfig, CancellationToken cancellationToken)
        {

            JArray results = new JArray();
            using (DbCommand command = this.m_connection.CreateCommand())
            {
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = extractConfig.query;
                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    List<string> columns = new List<string>(reader.FieldCount);
                    for (int i = 0; i < reader.FieldCount; ++i) 
                    { 
                        columns.Add(reader.GetName(i));
                    }
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        JObject row = new JObject();
                        for (int i = 0; i < columns.Count; ++i)
                        {
                            object? value = reader.GetValue(i);
                            if (value == DBNull.Value)
                            {
                                value = null;
                            }
                            JToken jValue = JToken.FromObject(value);
                            row.Add(columns[i], jValue);
                        }
                        results.Add(row);
                    };
                }
            }
            return results;
        }
    }
}
