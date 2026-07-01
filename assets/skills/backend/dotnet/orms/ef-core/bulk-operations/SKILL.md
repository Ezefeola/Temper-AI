---
name: bulk-operations
description: >
  Canonical EF Core bulk and batch operation guidance.
  Load only for explicit high-volume insert or batch tasks.
requires: [backend-dotnet-csharp]
produces: [bulk-insert, batch-update]
---

# Bulk Operations — TemperAI

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ONLY load this skill when the task explicitly involves bulk or batch scale**
2. **ALWAYS preserve transactional correctness**
3. **ALWAYS measure whether batching is truly required before adding complexity**
4. **NEVER replace normal repository flows with bulk APIs for ordinary CRUD paths**

## Load when

- The task explicitly mentions bulk insert
- The task processes large datasets where ordinary per-row persistence is not acceptable

## Scope and trigger

Use this skill only for explicit high-volume flows, typically 1000+ rows, where standard `AddAsync()` plus `CompleteAsync()` is too slow.

For ordinary CRUD and small batches, stay on the standard repository flow.

## Canonical contract

```csharp
public interface IBulkInsertOperations
{
    Task<int> BulkInsertAsync(
        List<object> entities,
        CancellationToken cancellationToken,
        int batchSize = 10000,
        int bulkCopyTimeout = 600);
}
```

## Implementation pattern

`BulkInsertOperations` is responsible for:

- Requiring an active EF transaction before execution
- Resolving EF metadata from the entity type
- Choosing the insert path based on primary key behavior
- Streaming rows through `SqlBulkCopy`
- Preserving parent-child relationships for navigation collections
- Rehydrating database-generated IDs back into in-memory entities
- Recursing through child graphs with the same key-resolution strategy

```csharp
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
        if (entities is null || entities.Count == 0)
            return 0;

        IDbContextTransaction? currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is null)
            throw new InvalidOperationException("BulkInsertAsync requires an active transaction.");

        IEntityType entityType = _dbContext.Model.FindEntityType(entities[0].GetType())
            ?? throw new InvalidOperationException("Could not resolve entity type.");

        IKey? primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is null)
            return await BulkInsertHasNoKeyAsync(...);

        IProperty keyProperty = primaryKey.Properties[0];
        if (keyProperty.ValueGenerated == ValueGenerated.Never)
            return await BulkInsertValueGeneratedNeverAsync(...);

        return await BulkInsertValueGeneratedOnAddAsync(...);
    }
}
```

### Full workflow to preserve from legacy implementation

1. Validate `entities` and return `0` for empty input.
2. Require `_dbContext.Database.CurrentTransaction`; never open an implicit transaction inside the bulk component.
3. Resolve `IEntityType` from `entities[0].GetType()` and fail fast if metadata is missing.
4. Create a stable `Dictionary<object, int>` temp-id map with `i + 1` values for every input entity.
5. Resolve the entity primary key.
6. Route to one of three concrete paths:

| Condition | Path |
|---|---|
| No primary key | `BulkInsertHasNoKeyAsync(...)` |
| Key exists and `ValueGenerated.Never` | `BulkInsertValueGeneratedNeverAsync(...)` |
| Key exists and database generates it | `BulkInsertValueGeneratedOnAddAsync(...)` |

### Concrete method responsibilities

#### `BulkInsertHasNoKeyAsync`

- Resolve table name and schema from EF metadata.
- Select only non-shadow properties with `ValueGenerated == ValueGenerated.Never`.
- Reuse the current SQL connection and current transaction.
- Open the connection only if it was previously closed.
- Configure `SqlBulkCopy` with:
  - `DestinationTableName = $"[{schema}].[{tableName}]"`
  - `BatchSize = batchSize`
  - `BulkCopyTimeout = bulkCopyTimeout`
  - `EnableStreaming = true`
- Map every property by column name.
- Stream rows using `EntityDataReader`.
- Log the inserted row count.
- Use `ParentKeyResolver.NoKey()` and still call `ProcessNavigationsAsync(...)` so child graphs continue to be processed when possible.
- Close the connection only if this component opened it.

```csharp
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

        _logger?.LogInformation(
            "BulkInsert (HasNoKey) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
            entities.Count,
            schema,
            tableName);

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
```

#### `BulkInsertValueGeneratedNeverAsync`

- Use the same direct `SqlBulkCopy` path as the no-key case.
- Include the explicit key column in the streamed property set.
- Build `Func<object, object?> getParentIdFromEntity = entity => keyProperty.PropertyInfo?.GetValue(entity)`.
- Build `ParentKeyResolver.FromMemoryGenerated(...)`.
- Process child navigations immediately after inserting the parent set.

```csharp
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

        _logger?.LogInformation(
            "BulkInsert (ValueGenerated.Never) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
            entities.Count,
            schema,
            tableName);

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
```

#### `BulkInsertValueGeneratedOnAddAsync`

- Delegate the parent insert to `BulkInsertWithTempTableAsync(...)`.
- Read back the `Dictionary<int, object>` temp-id to real-id mapping.
- Reassign the generated database key onto each in-memory entity using EF key metadata.
- Create `ParentKeyResolver.FromDatabaseGenerated(mapping, tempIds)`.
- Continue with `ProcessNavigationsAsync(...)` so child rows receive the real parent IDs.

```csharp
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
```

## Insert strategies

### 1. No primary key metadata

- Bulk-copy the scalar columns directly
- Resolve child navigations afterward with a no-key resolver

### 2. `ValueGenerated.Never`

- Bulk-copy the explicit key value already present in memory
- Reuse that in-memory key to resolve child foreign keys

### 3. `ValueGenerated.OnAdd`

- Create a temp table with `__TempId`
- Bulk-copy source rows into the temp table
- `MERGE` into the real table with `OUTPUT INSERTED.[Key], source.__TempId`
- Rehydrate generated IDs back into the in-memory entities
- Use the temp-id mapping to propagate parent IDs into child rows

### `BulkInsertWithTempTableAsync` contract

- Resolve table name, schema, and the first primary-key property.
- Select only non-shadow properties where `ValueGenerated == ValueGenerated.Never`; generated keys must not be sent as source columns.
- Create a temp table name like `#Temp_{tableName}_{Guid.NewGuid():N}`.
- Generate temp-table DDL from EF relational type metadata.
- Bulk-copy `__TempId` plus scalar columns into the temp table.
- Execute a `MERGE`/`OUTPUT` statement to capture database-generated IDs.
- Always drop the temp table in a `finally` block, even if the `MERGE` fails.
- Return the temp-id mapping to callers so they can restore graph identity.

```csharp
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

        _logger?.LogInformation(
            "BulkInsert (ValueGenerated.OnAdd) completed: {RowCount} rows inserted in [{Schema}].[{Table}]",
            entities.Count,
            schema,
            tableName);

        return mapping;
    }
    finally
    {
        if (!wasOpen && connection.State == ConnectionState.Open)
            await connection.CloseAsync();
    }
}
```

## Temp table mapping pattern

```csharp
string insertSql = $@"
    MERGE INTO [{schema}].[{table}] AS target
    USING {tempTable} AS source
    ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT ({string.Join(",", columns)})
        VALUES ({string.Join(",", sourceColumns)})
    OUTPUT INSERTED.[{keyColumn}], source.__TempId;";
```

## EntityDataReader

Use a custom `DbDataReader` to stream entity property values into `SqlBulkCopy` without materializing a `DataTable`.

Critical behavior preserved from legacy guidance:

- Support a synthetic `__TempId` column when the strategy needs ID remapping
- Return `DBNull.Value` for null CLR values
- Handle enum conversion correctly because `SqlBulkCopy` bypasses EF Core converters
- Preserve the lightweight `DbDataReader` contract members that `SqlBulkCopy` still expects during schema and value resolution

### Reader contract that must remain implemented

- `GetOrdinal(string name)` must resolve `__TempId` first when present, then mapped column names
- `GetValues(object[] values)` must copy the current row sequentially through `GetValue(i)`
- Indexers `this[int i]` and `this[string name]` must delegate to `GetValue(...)`
- `Depth`, `IsClosed`, `HasRows`, `RecordsAffected`, `NextResult()`, `Close()`, `ReadAsync(...)`, and `GetEnumerator()` must keep the simple streaming behavior from the legacy implementation
- Primitive getters like `GetBoolean`, `GetInt32`, `GetString`, `GetGuid`, and `GetDateTime` should remain thin casts over `GetValue(i)` so `SqlBulkCopy` can access typed values without extra conversion layers

### Enum handling

- If the mapped column type is `varchar`/`nvarchar`/`char`/`nchar`, emit `value.ToString()`
- Otherwise emit the enum underlying numeric value with `Convert.ToInt32(value)`
- `GetFieldType()` must return `typeof(string)` for string-backed enums so `SqlBulkCopy` uses the correct mapper

```csharp
internal sealed class EntityDataReader : DbDataReader
{
    private readonly IReadOnlyList<object> _entities;
    private readonly IReadOnlyList<IProperty> _properties;
    private readonly Dictionary<object, int>? _tempIds;
    private readonly bool _hasTempId;
    private int _currentIndex = -1;
    private bool _closed;

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
            if (i == 0)
                return _tempIds![entity];

            return GetPropertyValue(_properties[i - 1], entity);
        }

        return GetPropertyValue(_properties[i], entity);
    }

    private static object GetPropertyValue(IProperty property, object entity)
    {
        object? value = property.PropertyInfo?.GetValue(entity);
        if (value is null)
            return DBNull.Value;

        if (property.ClrType.IsEnum)
        {
            string? columnType = property.GetColumnType();
            bool storedAsString = columnType is not null &&
                (columnType.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("char", StringComparison.OrdinalIgnoreCase));

            if (storedAsString)
                return value.ToString() ?? string.Empty;

            return Convert.ToInt32(value);
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
            if (i == 0)
                return typeof(int);

            property = _properties[i - 1];
        }
        else
        {
            property = _properties[i];
        }

        if (property.ClrType.IsEnum)
        {
            string? columnType = property.GetColumnType();
            bool storedAsString = columnType is not null &&
                (columnType.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("nchar", StringComparison.OrdinalIgnoreCase) ||
                 columnType.Contains("char", StringComparison.OrdinalIgnoreCase));

            if (storedAsString)
                return typeof(string);
        }

    return Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
}

public override int GetOrdinal(string name)
{
    if (_hasTempId && name == "__TempId")
        return 0;

    int offset = _hasTempId ? 1 : 0;
    for (int i = 0; i < _properties.Count; i++)
    {
        if (_properties[i].GetColumnName() == name)
            return i + offset;
    }

    throw new IndexOutOfRangeException($"Column '{name}' not found.");
}

public override bool GetBoolean(int i) => (bool)GetValue(i);
public override byte GetByte(int i) => (byte)GetValue(i);
public override long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length) => 0;
public override char GetChar(int i) => (char)GetValue(i);
public override long GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length) => 0;
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
    for (int i = 0; i < count; i++)
        values[i] = GetValue(i);

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
public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

public override System.Collections.IEnumerator GetEnumerator()
{
    return new DbEnumerator(this, closeReader: false);
}
}
```

### Supporting helpers that must exist

```csharp
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
    return typeMapping?.StoreType
        ?? throw new InvalidOperationException($"Could not determine SQL type for property '{property.Name}'.");
}
```

## ParentKeyResolver

Use a resolver abstraction to unify the three parent-key sources:

- Database-generated key mapping from temp table output
- In-memory keys for `ValueGenerated.Never`
- No-key mode for entities with no primary key

```csharp
public sealed class ParentKeyResolver
{
    public Func<object, object?>? GetKeyFromEntity { get; private set; }
    public Dictionary<int, object>? IdMapping { get; private set; }
    public Dictionary<object, int>? TempIds { get; private set; }

    private ParentKeyResolver() { }

    public static ParentKeyResolver FromDatabaseGenerated(Dictionary<int, object> mapping, Dictionary<object, int> tempIds)
        => new() { IdMapping = mapping, TempIds = tempIds };

    public static ParentKeyResolver FromMemoryGenerated(Func<object, object?> getKeyFunc)
        => new() { GetKeyFromEntity = getKeyFunc };

    public static ParentKeyResolver NoKey() => new();
}
```

## Navigation graph workflow

The legacy skill did more than insert the root rows. The canonical skill must also teach the recursive child-graph workflow:

1. Enumerate `parentEntityType.GetNavigations()`.
2. For each navigation, resolve:
   - `foreignKeyProperty = navigation.ForeignKey.Properties.First()`
   - `childEntityType = navigation.TargetEntityType`
   - `childKeyProperty = childEntityType.FindPrimaryKey()?.Properties.FirstOrDefault()`
3. If `navigation.IsCollection`:
   - Read the collection from each parent.
   - Resolve the real parent ID through `GetParentRealId(...)`.
   - Call `ResolveForeignKeyForChild(...)` for every child.
   - Accumulate child rows and insert them in a single bulk call.
4. If the navigation is a reference:
   - Read the single child object.
   - Resolve the real parent ID.
   - Set the child FK.
   - Insert the child set in bulk.
5. Recurse by calling `InsertChildEntitiesAsync(...)`, which repeats the same key strategy selection for the child entity type.

```csharp
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
                if (collection == null)
                    continue;

                object? parentRealId = GetParentRealId(parent, keyResolver);
                if (parentRealId == null)
                    continue;

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
                if (child == null)
                    continue;

                object? parentRealId = GetParentRealId(parent, keyResolver);
                if (parentRealId == null)
                    continue;

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
```

### Child insertion contract

`InsertChildEntitiesAsync(...)` must repeat the same routing logic used by the root insert:

- No key: `BulkInsertHasNoKeyAsync(...)`
- `ValueGenerated.Never`: `BulkInsertValueGeneratedNeverAsync(...)`
- `ValueGenerated.OnAdd`: `BulkInsertWithTempTableAsync(...)` plus generated-ID rehydration plus recursive navigation processing

This preserved recursive behavior is what lets the legacy implementation handle full parent/child graphs instead of flat tables only.

Legacy `EntityDataReader` behavior preserved here intentionally: column ordinal resolution, typed accessors, async read shim, and reader enumeration are part of the canonical implementation, not optional sample details.

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

## Operational rules

| Rule | Explanation |
|---|---|
| Active transaction required | Always call `BeginTransactionAsync` before `BulkInsertAsync` |
| Use for 1000+ rows | Smaller batches should use standard EF flows |
| Keep batch size configurable | Default `10000`, tune per workload |
| Keep timeout configurable | Default `600`, tune for large imports |
| Keep temp-table cleanup guaranteed | Always drop the temp table in `finally` |
| Rehydrate generated IDs | Update in-memory entities before processing children |
| Preserve navigation recursion | Child collections and references must be bulk inserted too |
| Commit after success | `CommitTransactionAsync()` only after the full insert finishes |
| Roll back on failure | Never leave partial bulk writes committed |

## Anti-patterns — never do this

```csharp
// ❌ NEVER call bulk insert without an active transaction
await bulkInsert.BulkInsertAsync(entities, cancellationToken);

// ❌ NEVER replace ordinary CRUD paths with bulk APIs
await bulkInsert.BulkInsertAsync([singleProduct], cancellationToken);

// ❌ NEVER ignore enum storage differences when building the reader
return value; // wrong for string-backed enums
```
