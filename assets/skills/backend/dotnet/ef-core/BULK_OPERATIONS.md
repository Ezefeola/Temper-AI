---
name: bulk-operations
description: >
  High-performance bulk insert operations using SqlBulkCopy.
  Load only when implementing batch inserts for 1000+ rows.
---

# Bulk Operations — TemperAI

EF Core lacks native bulk operations. Use `BulkInsertOperations` for inserting large datasets efficiently. Located in `Infrastructure/Persistence/BulkInsert/`.

> **Important**: Load this file only when implementing batch inserts for 1000+ rows. For single inserts or small batches, use standard `AddAsync()`.

## IBulkInsertOperations

```csharp
// Infrastructure/Persistence/BulkInsert/IBulkInsertOperations.cs
public interface IBulkInsertOperations
{
    Task<int> BulkInsertAsync(
        List<object> entities,
        CancellationToken cancellationToken,
        int batchSize = 10000,
        int bulkCopyTimeout = 600);
}
```

## BulkInsertOperations implementation

```csharp
// Infrastructure/Persistence/BulkInsert/BulkInsertOperations.cs
public sealed class BulkInsertOperations : IBulkInsertOperations
{
    private readonly DbContext _dbContext;
    private readonly ILogger<BulkInsertOperations>? _logger;

    public BulkInsertOperations(DbContext context, ILoggerFactory? loggerFactory = null)
    {
        _dbContext = context;
        _logger = loggerFactory?.CreateLogger<BulkInsertOperations>();
    }

    public async Task<int> BulkInsertAsync(
        List<object> entities,
        CancellationToken cancellationToken,
        int batchSize = 10000,
        int bulkCopyTimeout = 600)
    {
        if (entities == null || entities.Count == 0)
            return 0;

        IDbContextTransaction? currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is null)
            throw new InvalidOperationException("BulkInsertAsync requires an active transaction.");

        IEntityType? entityType = _dbContext.Model.FindEntityType(entities[0].GetType());
        if (entityType is null)
            throw new InvalidOperationException($"Could not find entity type for {entities[0].GetType().Name}.");

        Dictionary<object, int> tempIds = entities.Select((e, i) => new { e, id = i + 1 })
            .ToDictionary(x => x.e, x => x.id);

        IKey? primaryKey = entityType.FindPrimaryKey();

        if (primaryKey is null)
            return await BulkInsertHasNoKeyAsync(entities, entityType, tempIds, batchSize, bulkCopyTimeout, cancellationToken);

        IProperty keyProperty = primaryKey.Properties[0];

        if (keyProperty.ValueGenerated == ValueGenerated.Never)
            return await BulkInsertValueGeneratedNeverAsync(entities, entityType, tempIds, keyProperty, batchSize, bulkCopyTimeout, cancellationToken);

        return await BulkInsertValueGeneratedOnAddAsync(entities, entityType, tempIds, batchSize, bulkCopyTimeout, cancellationToken);
    }

    private async Task<int> BulkInsertHasNoKeyAsync(
        List<object> entities,
        IEntityType entityType,
        Dictionary<object, int> parentTempIds,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return 0;

        string? tableName = entityType.GetTableName();
        if (tableName is null)
            throw new InvalidOperationException($"Could not find table name for {entityType.Name}.");

        string schema = entityType.GetSchema() ?? "dbo";

        List<IProperty> properties = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && p.ValueGenerated == ValueGenerated.Never)
            .ToList();

        SqlConnection connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        SqlTransaction transaction = (SqlTransaction)_dbContext.Database.CurrentTransaction!.GetDbTransaction();
        bool wasOpen = connection.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
                await connection.OpenAsync(cancellationToken);

            using SqlBulkCopy bulkCopy = new(connection, SqlBulkCopyOptions.Default, transaction)
            {
                DestinationTableName = $"[{schema}].[{tableName}]",
                BatchSize = batchSize,
                BulkCopyTimeout = bulkCopyTimeout,
                EnableStreaming = true
            };

            foreach (IProperty property in properties)
                bulkCopy.ColumnMappings.Add(property.GetColumnName(), property.GetColumnName());

            using EntityDataReader reader = new(entities.Cast<object>().ToList(), properties);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken);

            _logger?.LogInformation("BulkInsert (HasNoKey) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
                entities.Count, schema, tableName);

            ParentKeyResolver keyResolver = ParentKeyResolver.NoKey();
            await ProcessNavigationsAsync(entities, entityType, keyResolver, batchSize, bulkCopyTimeout, cancellationToken);
        }
        finally
        {
            if (!wasOpen && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }

        return entities.Count;
    }

    private async Task<int> BulkInsertValueGeneratedNeverAsync(
        List<object> entities,
        IEntityType entityType,
        Dictionary<object, int> parentTempIds,
        IProperty keyProperty,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return 0;

        string? tableName = entityType.GetTableName();
        if (tableName is null)
            throw new InvalidOperationException($"Could not find table name for {entityType.Name}.");

        string schema = entityType.GetSchema() ?? "dbo";

        List<IProperty> properties = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && p.ValueGenerated == ValueGenerated.Never)
            .ToList();

        SqlConnection connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        SqlTransaction transaction = (SqlTransaction)_dbContext.Database.CurrentTransaction!.GetDbTransaction();
        bool wasOpen = connection.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
                await connection.OpenAsync(cancellationToken);

            using SqlBulkCopy bulkCopy = new(connection, SqlBulkCopyOptions.Default, transaction)
            {
                DestinationTableName = $"[{schema}].[{tableName}]",
                BatchSize = batchSize,
                BulkCopyTimeout = bulkCopyTimeout,
                EnableStreaming = true
            };

            foreach (IProperty property in properties)
                bulkCopy.ColumnMappings.Add(property.GetColumnName(), property.GetColumnName());

            using EntityDataReader reader = new(entities.Cast<object>().ToList(), properties);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken);

            _logger?.LogInformation("BulkInsert (ValueGenerated.Never) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
                entities.Count, schema, tableName);

            Func<object, object?> getParentIdFromEntity = entity => keyProperty.PropertyInfo?.GetValue(entity);
            ParentKeyResolver keyResolver = ParentKeyResolver.FromMemoryGenerated(getParentIdFromEntity);
            await ProcessNavigationsAsync(entities, entityType, keyResolver, batchSize, bulkCopyTimeout, cancellationToken);
        }
        finally
        {
            if (!wasOpen && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }

        return entities.Count;
    }

    private async Task<int> BulkInsertValueGeneratedOnAddAsync(
        List<object> entities,
        IEntityType entityType,
        Dictionary<object, int> tempIds,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return 0;

        Dictionary<int, object> mapping = await BulkInsertWithTempTableAsync(
            entities,
            entityType,
            tempIds,
            batchSize,
            bulkCopyTimeout,
            cancellationToken);

        foreach (KeyValuePair<int, object> idMappingEntry in mapping)
        {
            object entity = entities[idMappingEntry.Key - 1];
            IKey? key = entityType.FindPrimaryKey();
            if (key != null)
                key.Properties[0].PropertyInfo?.SetValue(entity, idMappingEntry.Value);
        }

        ParentKeyResolver keyResolver = ParentKeyResolver.FromDatabaseGenerated(mapping, tempIds);
        await ProcessNavigationsAsync(entities, entityType, keyResolver, batchSize, bulkCopyTimeout, cancellationToken);

        return entities.Count;
    }

    private async Task ProcessNavigationsAsync(
        List<object> parentEntities,
        IEntityType parentEntityType,
        ParentKeyResolver keyResolver,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        List<INavigation> navigations = parentEntityType.GetNavigations().ToList();

        foreach (INavigation navigation in navigations)
        {
            IProperty foreignKeyProperty = navigation.ForeignKey.Properties.First();
            IEntityType childEntityType = navigation.TargetEntityType;
            IKey? childKey = childEntityType.FindPrimaryKey();
            IProperty? childKeyProperty = childKey?.Properties.FirstOrDefault();

            if (navigation.IsCollection)
            {
                List<object> children = [];

                foreach (object parent in parentEntities)
                {
                    IEnumerable<object>? collection = navigation.PropertyInfo?.GetValue(parent) as IEnumerable<object>;
                    if (collection == null) continue;

                    object? parentRealId = GetParentRealId(parent, keyResolver);
                    if (parentRealId == null) continue;

                    foreach (object child in collection)
                    {
                        ResolveForeignKeyForChild(child, childEntityType, childKeyProperty, foreignKeyProperty, parentRealId);
                        children.Add(child);
                    }
                }

                if (children.Count > 0)
                    await InsertChildEntitiesAsync(children, childEntityType, batchSize, bulkCopyTimeout, cancellationToken);
            }
            else
            {
                List<object> children = [];

                foreach (object parent in parentEntities)
                {
                    object? child = navigation.PropertyInfo?.GetValue(parent);
                    if (child == null) continue;

                    object? parentRealId = GetParentRealId(parent, keyResolver);
                    if (parentRealId == null) continue;

                    ResolveForeignKeyForChild(child, childEntityType, childKeyProperty, foreignKeyProperty, parentRealId);
                    children.Add(child);
                }

                if (children.Count > 0)
                    await InsertChildEntitiesAsync(children, childEntityType, batchSize, bulkCopyTimeout, cancellationToken);
            }
        }
    }

    private void ResolveForeignKeyForChild(
        object child,
        IEntityType childEntityType,
        IProperty? childKeyProperty,
        IProperty foreignKeyProperty,
        object parentRealId)
    {
        if (childKeyProperty is null)
            return;

        if (childKeyProperty.ValueGenerated == ValueGenerated.OnAdd)
            foreignKeyProperty.PropertyInfo?.SetValue(child, parentRealId);
    }

    private object? GetParentRealId(object parent, ParentKeyResolver keyResolver)
    {
        if (keyResolver.IdMapping != null && keyResolver.TempIds != null)
        {
            if (keyResolver.TempIds.TryGetValue(parent, out int tempId))
                return keyResolver.IdMapping[tempId];
        }

        if (keyResolver.GetKeyFromEntity != null)
            return keyResolver.GetKeyFromEntity(parent);

        return null;
    }

    private async Task InsertChildEntitiesAsync(
        List<object> entities,
        IEntityType entityType,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return;

        Dictionary<object, int> tempIds = entities.Select((e, i) => new { e, id = i + 1 })
            .ToDictionary(x => x.e, x => x.id);

        IKey? key = entityType.FindPrimaryKey();

        if (key is null)
        {
            await BulkInsertHasNoKeyAsync(entities, entityType, tempIds, batchSize, bulkCopyTimeout, cancellationToken);
            return;
        }

        IProperty keyProperty = key.Properties[0];

        if (keyProperty.ValueGenerated == ValueGenerated.Never)
        {
            await BulkInsertValueGeneratedNeverAsync(entities, entityType, tempIds, keyProperty, batchSize, bulkCopyTimeout, cancellationToken);
            return;
        }

        Dictionary<int, object> mapping = await BulkInsertWithTempTableAsync(
            entities,
            entityType,
            tempIds,
            batchSize,
            bulkCopyTimeout,
            cancellationToken);

        foreach (KeyValuePair<int, object> idMappingEntry in mapping)
        {
            if (idMappingEntry.Key >= 1 && idMappingEntry.Key <= entities.Count)
            {
                object entity = entities[idMappingEntry.Key - 1];
                keyProperty.PropertyInfo?.SetValue(entity, idMappingEntry.Value);
            }
        }

        ParentKeyResolver keyResolver = ParentKeyResolver.FromDatabaseGenerated(mapping, tempIds);
        await ProcessNavigationsAsync(entities, entityType, keyResolver, batchSize, bulkCopyTimeout, cancellationToken);
    }

    private async Task<Dictionary<int, object>> BulkInsertWithTempTableAsync(
        List<object> entities,
        IEntityType entityType,
        Dictionary<object, int> tempIds,
        int batchSize,
        int bulkCopyTimeout,
        CancellationToken cancellationToken)
    {
        string? tableName = entityType.GetTableName();
        if (tableName is null)
            throw new InvalidOperationException($"No table name for {entityType.Name}.");

        string schema = entityType.GetSchema() ?? "dbo";

        IKey primaryKey = entityType.FindPrimaryKey() ?? throw new Exception("No primary key found");
        IProperty keyProperty = primaryKey.Properties[0];

        List<IProperty> properties = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && p.ValueGenerated == ValueGenerated.Never)
            .ToList();

        string tempTableName = $"#Temp_{tableName}_{Guid.NewGuid():N}";

        SqlConnection connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        SqlTransaction transaction = (SqlTransaction)_dbContext.Database.CurrentTransaction!.GetDbTransaction();
        bool wasOpen = connection.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
                await connection.OpenAsync(cancellationToken);

            string createTempTableSql = GenerateCreateTempTableSql(tempTableName, properties);
            await using (SqlCommand createTempTableCommand = new(createTempTableSql, connection, transaction))
            {
                await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            using (SqlBulkCopy bulkCopy = new(connection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = tempTableName;
                bulkCopy.BatchSize = batchSize;
                bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
                bulkCopy.EnableStreaming = true;

                bulkCopy.ColumnMappings.Add("__TempId", "__TempId");
                foreach (IProperty property in properties)
                    bulkCopy.ColumnMappings.Add(property.GetColumnName(), property.GetColumnName());

                using EntityDataReader reader = new(entities, properties, tempIds);
                await bulkCopy.WriteToServerAsync(reader, cancellationToken);
            }

            string insertSql = GenerateInsertWithOutputSql(tempTableName, schema, tableName, properties, keyProperty);

            Dictionary<int, object> mapping = new(entities.Count);
            try
            {
                await using (SqlCommand sqlCommand = new(insertSql, connection, transaction))
                await using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken))
                {
                    while (await sqlDataReader.ReadAsync(cancellationToken))
                    {
                        object realId = sqlDataReader.GetValue(0);
                        int tempIdValue = sqlDataReader.GetInt32(1);
                        mapping[tempIdValue] = realId;
                    }
                }
            }
            finally
            {
                await using var dropCmd = new SqlCommand(
                    $"IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}",
                    connection,
                    transaction);
                await dropCmd.ExecuteNonQueryAsync(CancellationToken.None);
            }

            _logger?.LogInformation("BulkInsert (ValueGenerated.OnAdd) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
                entities.Count, schema, tableName);

            return mapping;
        }
        finally
        {
            if (!wasOpen && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static string GenerateCreateTempTableSql(string tableName, List<IProperty> properties)
    {
        List<string> columns = ["[__TempId] INT NOT NULL"];
        foreach (IProperty property in properties)
        {
            string columnName = property.GetColumnName();
            string sqlType = GetSqlType(property);
            string isNullable = property.IsNullable ? "NULL" : "NOT NULL";
            columns.Add($"[{columnName}] {sqlType} {isNullable}");
        }

        return $"CREATE TABLE {tableName} ({string.Join(",", columns)})";
    }

    private static string GenerateInsertWithOutputSql(
        string tempTable,
        string schema,
        string table,
        List<IProperty> properties,
        IProperty keyProperty)
    {
        List<string> columns = properties.Select(p => $"[{p.GetColumnName()}]").ToList();
        List<string> sourceColumns = properties.Select(p => $"source.[{p.GetColumnName()}]").ToList();
        string keyColumn = keyProperty.GetColumnName();

        return $@"
            MERGE INTO [{schema}].[{table}] AS target
            USING {tempTable} AS source
            ON 1 = 0
            WHEN NOT MATCHED THEN
                INSERT ({string.Join(",", columns)})
                VALUES ({string.Join(",", sourceColumns)})
            OUTPUT INSERTED.[{keyColumn}], source.__TempId;";
    }

    private static string GetSqlType(IProperty property)
    {
        RelationalTypeMapping? typeMapping = property.FindRelationalTypeMapping();
        return typeMapping?.StoreType ?? throw new InvalidOperationException($"Could not determine SQL type for property '{property.Name}'.");
    }
}
```

## EntityDataReader

```csharp
// Infrastructure/Persistence/BulkInsert/EntityDataReader.cs
internal sealed class EntityDataReader : DbDataReader
{
    private readonly IReadOnlyList<object> _entities;
    private readonly IReadOnlyList<IProperty> _properties;
    private readonly Dictionary<object, int>? _tempIds;
    private readonly bool _hasTempId;
    private int _currentIndex = -1;
    private bool _closed = false;

    public EntityDataReader(IReadOnlyList<object> entities, IReadOnlyList<IProperty> properties)
    {
        _entities = entities;
        _properties = properties;
        _hasTempId = false;
    }

    public EntityDataReader(IReadOnlyList<object> entities, IReadOnlyList<IProperty> properties, Dictionary<object, int> tempIds)
    {
        _entities = entities;
        _properties = properties;
        _tempIds = tempIds;
        _hasTempId = true;
    }

    public override int FieldCount => _hasTempId ? _properties.Count + 1 : _properties.Count;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _entities.Count;
    }

    public override object GetValue(int i)
    {
        object entity = _entities[_currentIndex];

        if (_hasTempId)
        {
            if (i == 0) return _tempIds![entity];
            return GetPropertyValue(_properties[i - 1], entity);
        }

        return GetPropertyValue(_properties[i], entity);
    }

    private static object GetPropertyValue(IProperty property, object entity)
    {
        object? value = property.PropertyInfo?.GetValue(entity);
        if (value is null) return DBNull.Value;

        // Handle enum types — convert to the appropriate SQL representation
        if (property.ClrType.IsEnum)
        {
            string? columnType = property.GetColumnType();
            bool storedAsString = columnType is not null &&
                (columnType.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("char", StringComparison.OrdinalIgnoreCase));

            if (storedAsString)
            {
                // String-backed enum: convert to enum name (e.g., ActionTypeEnum.Buy -> "Buy")
                return value.ToString() ?? string.Empty;
            }
            else
            {
                // Int-backed enum: convert to underlying integer value
                return Convert.ToInt32(value);
            }
        }

        return value;
    }

    public override string GetName(int i)
    {
        if (_hasTempId)
            return i == 0 ? "__TempId" : _properties[i - 1].GetColumnName();

        return _properties[i].GetColumnName();
    }

    public override bool IsDBNull(int i) => GetValue(i) is DBNull;

    public override Type GetFieldType(int i)
    {
        IProperty property;
        if (_hasTempId)
        {
            if (i == 0) return typeof(int);
            property = _properties[i - 1];
        }
        else
        {
            property = _properties[i];
        }

        // If enum stored as string, return string type
        if (property.ClrType.IsEnum)
        {
            string? columnType = property.GetColumnType();
            bool storedAsString = columnType is not null &&
                (columnType.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("char", StringComparison.OrdinalIgnoreCase));

            if (storedAsString) return typeof(string);
        }

        return Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
    }

    public override int GetOrdinal(string name)
    {
        if (_hasTempId && name == "__TempId") return 0;
        int offset = _hasTempId ? 1 : 0;
        for (int i = 0; i < _properties.Count; i++)
            if (_properties[i].GetColumnName() == name) return i + offset;
        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override bool GetBoolean(int i) => (bool)GetValue(i);
    public override byte GetByte(int i) => (byte)GetValue(i);
    public override long GetBytes(int i, long fo, byte[]? b, int bo, int l) => 0;
    public override char GetChar(int i) => (char)GetValue(i);
    public override long GetChars(int i, long fo, char[]? b, int bo, int l) => 0;
    public override string GetDataTypeName(int i) => GetFieldType(i).Name;
    public override DateTime GetDateTime(int i) => (DateTime)GetValue(i);
    public override decimal GetDecimal(int i) => (decimal)GetValue(i);
    public override double GetDouble(int i) => (double)GetValue(i);
    public override float GetFloat(int i) => (float)GetValue(i);
    public override Guid GetGuid(int i) => (Guid)GetValue(i);
    public override short GetInt16(int i) => (short)GetValue(i);
    public override int GetInt32(int i) => (int)GetValue(i);
    public override long GetInt64(int i) => (long)GetValue(i);
    public override string GetString(int i) => (string)GetValue(i);
    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }

    public override object this[int i] => GetValue(i);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override int Depth => 0;
    public override bool IsClosed => _closed;
    public override bool HasRows => _entities.Count > 0;
    public override int RecordsAffected => -1;
    public override bool NextResult() => false;
    public override void Close() => _closed = true;
    public override DataTable GetSchemaTable() => throw new NotSupportedException();
    public override Task<bool> ReadAsync(CancellationToken ct) => Task.FromResult(Read());

    // Override DbDataReader.GetEnumerator() since it's abstract
    public override System.Collections.IEnumerator GetEnumerator()
    {
        return new System.Data.Common.DbEnumerator(this, closeReader: false);
    }
}
```

### Enum Handling in EntityDataReader

`EntityDataReader` must handle enum properties correctly regardless of their EF Core storage configuration. Since `SqlBulkCopy` reads values directly from entity properties (bypassing EF Core's type conversions), the reader must apply the appropriate conversion based on the column type.

**String-backed enums** (configured with `.HasConversion<string>()` in EF Core):
- Stored as `varchar`/`nvarchar` in SQL
- Example: `ActionTypeEnum.Buy` → `"Buy"`
- `GetPropertyValue` returns `value.ToString()`

**Int-backed enums** (no conversion configured, stored as `tinyint`/`int`):
- Stored as integer in SQL
- Example: `DayOfWeekEnum.Monday` → `1`
- `GetPropertyValue` returns `Convert.ToInt32(value)`

**GetFieldType behavior**:
- For string-backed enums, returns `typeof(string)` so `SqlBulkCopy` uses the correct data type mapper
- For int-backed enums, returns the enum's underlying type

## ParentKeyResolver

```csharp
// Infrastructure/Persistence/BulkInsert/ParentKeyResolver.cs
public sealed class ParentKeyResolver
{
    public Func<object, object?>? GetKeyFromEntity { get; private set; }
    public Dictionary<int, object>? IdMapping { get; private set; }
    public Dictionary<object, int>? TempIds { get; private set; }

    private ParentKeyResolver() { }

    public static ParentKeyResolver FromDatabaseGenerated(Dictionary<int, object> mapping, Dictionary<object, int> tempIds)
    {
        return new ParentKeyResolver
        {
            IdMapping = mapping,
            TempIds = tempIds
        };
    }

    public static ParentKeyResolver FromMemoryGenerated(Func<object, object?> getKeyFunc)
    {
        return new ParentKeyResolver
        {
            GetKeyFromEntity = getKeyFunc
        };
    }

    public static ParentKeyResolver NoKey()
    {
        return new ParentKeyResolver();
    }
}
```

## Usage

```csharp
await _unitOfWork.BeginTransactionAsync(cancellationToken);

try
{
    List<object> entities = GenerateLargeDataset();
    
    BulkInsertOperations bulkInsert = new(_dbContext);
    int insertedCount = await bulkInsert.BulkInsertAsync(entities, cancellationToken);
    
    await _unitOfWork.CommitTransactionAsync(cancellationToken);
}
catch
{
    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

## Rules

| Rule | Explanation |
|---|---|
| **Active transaction required** | Always call `BeginTransactionAsync` before `BulkInsertAsync` |
| **Use for 1000+ rows** | For smaller batches, use standard `AddAsync()` |
| **Handle DbUpdateException** | Wrap in try/catch with rollback |
| **Commit after success** | Call `CommitTransactionAsync` after successful insert |