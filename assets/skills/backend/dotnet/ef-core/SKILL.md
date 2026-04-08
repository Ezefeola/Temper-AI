---
name: dotnet-ef-core
description: >
  Entity Framework Core standards for .NET 10 projects. Covers entity
  configuration, repository patterns, UnitOfWork, DbContext, migrations,
  and query patterns. Use when creating or modifying any EF Core component.
---

# EF Core — TemperAI Standards

## Entity configuration — Fluent API only

- Never `DataAnnotations` on entities or Value Objects.
- One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations/`.
- Never `nvarchar(max)` or `varchar(max)` — always length from `Entity.Rules`.
- `varchar` for ASCII, `nvarchar` for Unicode.
- Value Objects configured with `OwnsOne` in the entity configuration.

```csharp
// ProductConfiguration.cs
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.Rules.NAME_MAX_LENGTH)
            .HasColumnType("varchar");

        builder.Property(product => product.Description)
            .HasMaxLength(Product.Rules.DESCRIPTION_MAX_LENGTH)
            .HasColumnType("nvarchar");

        builder.Property(product => product.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar");

        builder.Property(product => product.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(product => product.UpdatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(product => product.Name)
            .IsUnique();
    }
}
```

### Value Object configuration with OwnsOne

```csharp
// OrderConfiguration.cs
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        builder.OwnsOne(order => order.Total, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");

            moneyBuilder.Property(money => money.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .HasColumnType("varchar");
        });
    }
}
```

## DbContext

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Repositories — no .Update(), explicit tracking naming

```csharp
public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _appDbContext;

    public ProductRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    // With tracking — EF detects changes, use for updates
    public async Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    // Without tracking — more performant, read-only
    public async Task<Product?> GetByIdAsNoTrackingAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == productId,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.Name == productName,
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _appDbContext.Products.AddAsync(product, cancellationToken);
    }

    // No .Update() — EF detects changes via change tracker automatically
    // Just call CompleteAsync after modifying the tracked entity
}
```

## UnitOfWork — single entry point to repositories

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _appDbContext;

    public IProductRepository ProductRepository { get; }

    public UnitOfWork(
        AppDbContext appDbContext,
        IProductRepository productRepository)
    {
        _appDbContext = appDbContext;
        ProductRepository = productRepository;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);
    }

    public async Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            int rowsAffected = await _appDbContext.SaveChangesAsync(cancellationToken);

            return new SaveResult
            {
                IsSuccess = true,
                RowsAffected = rowsAffected
            };
        }
        catch (DbUpdateException dbUpdateException)
        {
            return new SaveResult
            {
                IsSuccess = false,
                ErrorMessage = dbUpdateException.InnerException?.Message
                    ?? dbUpdateException.Message
            };
        }
    }

    public void Dispose()
    {
        _appDbContext.Dispose();
    }
}

// SaveResult.cs
public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
```

## Bulk Insert Operations — for high-performance batch inserts

EF Core lacks native bulk operations. Use `BulkInsertOperations` for inserting large datasets efficiently. Located in `Infrastructure/Persistence/BulkInsert/`.

### IBulkInsertOperations

```csharp
// IBulkInsertOperations.cs — in Infrastructure/Persistence/BulkInsert/
public interface IBulkInsertOperations
{
    Task<int> BulkInsertAsync(
        List<object> entities,
        CancellationToken cancellationToken,
        int batchSize = 10000,
        int bulkCopyTimeout = 600);
}
```

### BulkInsertOperations

```csharp
// BulkInsertOperations.cs — in Infrastructure/Persistence/BulkInsert/
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

        // Require active transaction
        IDbContextTransaction? currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is null)
            throw new InvalidOperationException("BulkInsertAsync requiere una transacción activa.");

        // Get entity type from first entity (assuming homogeneous or graph)
        IEntityType? entityType = _dbContext.Model.FindEntityType(entities[0].GetType());
        if (entityType is null)
            throw new InvalidOperationException($"No se pudo encontrar el tipo de entidad para {entities[0].GetType().Name}.");

        // Build temp IDs for tracking (only needed for ValueGenerated.OnAdd)
        Dictionary<object, int> tempIds = entities.Select((e, i) => new { e, id = i + 1 })
            .ToDictionary(x => x.e, x => x.id);

        // Determine key type and handle accordingly
        IKey? primaryKey = entityType.FindPrimaryKey();

        if (primaryKey is null)
        {
            // HasNoKey: Direct bulk insert, no IDs - but still process navigations
            return await BulkInsertHasNoKeyAsync(entities, entityType, tempIds, batchSize, bulkCopyTimeout, cancellationToken);
        }

        IProperty keyProperty = primaryKey.Properties[0];

        if (keyProperty.ValueGenerated == ValueGenerated.Never)
        {
            // ValueGenerated.Never (e.g., Guid): Direct bulk insert, use existing IDs - but still process navigations
            return await BulkInsertValueGeneratedNeverAsync(entities, entityType, tempIds, keyProperty, batchSize, bulkCopyTimeout, cancellationToken);
        }

        // ValueGenerated.OnAdd: Use temp table and SET IDs in entities
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
            throw new InvalidOperationException($"No se pudo encontrar el nombre de la tabla para {entityType.Name}.");

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
            {
                bulkCopy.ColumnMappings.Add(property.GetColumnName(), property.GetColumnName());
            }

            using EntityDataReader reader = new(entities.Cast<object>().ToList(), properties);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken);

            _logger?.LogInformation("BulkInsert (HasNoKey) completado: {RowCount} filas insertadas en [{Schema}].[{Table}]",
                entities.Count, schema, tableName);

            // Process navigations (graph support for HasNoKey - no IDs to propagate, but still process children)
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
            throw new InvalidOperationException($"No se pudo encontrar el nombre de la tabla para {entityType.Name}.");

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
            {
                bulkCopy.ColumnMappings.Add(property.GetColumnName(), property.GetColumnName());
            }

            using EntityDataReader reader = new(entities.Cast<object>().ToList(), properties);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken);

            _logger?.LogInformation("BulkInsert (ValueGenerated.Never) completado: {RowCount} filas insertadas en [{Schema}].[{Table}]",
                entities.Count, schema, tableName);

            // Process navigations (graph support for ValueGenerated.Never - parent IDs are in memory)
            // Pass a function to get parent ID from the entity itself
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

        // Insert main entities with temp table and get mapping
        Dictionary<int, object> mapping = await BulkInsertWithTempTableAsync(
            entities,
            entityType,
            tempIds,
            batchSize,
            bulkCopyTimeout,
            cancellationToken);

        // Set real IDs back into entities
        foreach (KeyValuePair<int, object> idMappingEntry in mapping)
        {
            object entity = entities[idMappingEntry.Key - 1]; // tempIds are 1-based
            IKey? key = entityType.FindPrimaryKey();
            if (key != null)
            {
                key.Properties[0].PropertyInfo?.SetValue(entity, idMappingEntry.Value);
            }
        }

        // Process navigations (graph support) - parent has ValueGenerated.OnAdd with mapping
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
                // Collection navigation (one-to-many)
                List<object> children = [];

                foreach (object parent in parentEntities)
                {
                    IEnumerable<object>? collection = navigation.PropertyInfo?.GetValue(parent) as IEnumerable<object>;
                    if (collection == null) continue;

                    // Get parent's real ID based on key type
                    object? parentRealId = GetParentRealId(parent, keyResolver);
                    if (parentRealId == null) continue;

                    foreach (object child in collection)
                    {
                        ResolveForeignKeyForChild(child, childEntityType, childKeyProperty, foreignKeyProperty, parentRealId);
                        children.Add(child);
                    }
                }

                if (children.Count > 0)
                {
                    await InsertChildEntitiesAsync(children, childEntityType, batchSize, bulkCopyTimeout, cancellationToken);
                }
            }
            else
            {
                // Reference navigation (one-to-one)
                List<object> children = [];

                foreach (object parent in parentEntities)
                {
                    object? child = navigation.PropertyInfo?.GetValue(parent);
                    if (child == null) continue;

                    // Get parent's real ID based on key type
                    object? parentRealId = GetParentRealId(parent, keyResolver);
                    if (parentRealId == null) continue;

                    ResolveForeignKeyForChild(child, childEntityType, childKeyProperty, foreignKeyProperty, parentRealId);
                    children.Add(child);
                }

                if (children.Count > 0)
                {
                    await InsertChildEntitiesAsync(children, childEntityType, batchSize, bulkCopyTimeout, cancellationToken);
                }
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
        {
            // HasNoKey child - no FK needed
            return;
        }

        if (childKeyProperty.ValueGenerated == ValueGenerated.OnAdd)
        {
            // Child has DB-generated ID, use parent's real ID as FK
            foreignKeyProperty.PropertyInfo?.SetValue(child, parentRealId);
        }
        else if (childKeyProperty.ValueGenerated == ValueGenerated.Never)
        {
            // Child has manual ID (ValueGenerated.Never), FK already in memory - don't touch
        }
        // If HasNoKey (childKey is null), no FK needed - handled above
    }

    private object? GetParentRealId(object parent, ParentKeyResolver keyResolver)
    {
        // Case: Parent is ValueGenerated.OnAdd (ID from DB via mapping)
        if (keyResolver.IdMapping != null && keyResolver.TempIds != null)
        {
            if (keyResolver.TempIds.TryGetValue(parent, out int tempId))
            {
                return keyResolver.IdMapping[tempId];
            }
        }

        // Case: Parent is ValueGenerated.Never (ID from memory)
        if (keyResolver.GetKeyFromEntity != null)
        {
            return keyResolver.GetKeyFromEntity(parent);
        }

        // Case: Parent is HasNoKey - no ID to propagate
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

        // Build temp IDs for tracking (only needed for ValueGenerated.OnAdd)
        Dictionary<object, int> tempIds = entities.Select((e, i) => new { e, id = i + 1 })
            .ToDictionary(x => x.e, x => x.id);

        IKey? key = entityType.FindPrimaryKey();

        if (key is null)
        {
            // HasNoKey: Direct bulk insert
            await BulkInsertHasNoKeyAsync(entities, entityType, tempIds, batchSize, bulkCopyTimeout, cancellationToken);
            return;
        }

        IProperty keyProperty = key.Properties[0];

        if (keyProperty.ValueGenerated == ValueGenerated.Never)
        {
            // ValueGenerated.Never: Direct bulk insert
            // Parent IDs are already in memory - no need to set FKs here as they're already set in ProcessNavigationsAsync
            await BulkInsertValueGeneratedNeverAsync(entities, entityType, tempIds, keyProperty, batchSize, bulkCopyTimeout, cancellationToken);
            return;
        }

        // ValueGenerated.OnAdd: Use temp table
        Dictionary<int, object> mapping = await BulkInsertWithTempTableAsync(
            entities,
            entityType,
            tempIds,
            batchSize,
            bulkCopyTimeout,
            cancellationToken);

        // Set real IDs back into entities
        foreach (KeyValuePair<int, object> idMappingEntry in mapping)
        {
            if (idMappingEntry.Key >= 1 && idMappingEntry.Key <= entities.Count)
            {
                object entity = entities[idMappingEntry.Key - 1];
                keyProperty.PropertyInfo?.SetValue(entity, idMappingEntry.Value);
            }
        }

        // Process nested navigations recursively - parent has ValueGenerated.OnAdd with mapping
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

            // Create temp table
            string createTempTableSql = GenerateCreateTempTableSql(tempTableName, properties);
            await using (SqlCommand createTempTableCommand = new(createTempTableSql, connection, transaction))
            {
                await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Bulk copy to temp table
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

            // MERGE + OUTPUT to get generated IDs
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

            _logger?.LogInformation("BulkInsert (ValueGenerated.OnAdd) completado: {RowCount} filas insertadas en [{Schema}].[{Table}]",
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
        return typeMapping?.StoreType ?? throw new InvalidOperationException($"No se pudo determinar el tipo SQL para la propiedad '{property.Name}'.");
    }
}
```

### EntityDataReader — required by SqlBulkCopy

```csharp
// EntityDataReader.cs — in Infrastructure/Persistence/BulkInsert/
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

    // Los únicos 5 que SqlBulkCopy realmente invoca
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
            object? value = _properties[i - 1].PropertyInfo?.GetValue(entity);
            return value ?? DBNull.Value;
        }

        object? simpleValue = _properties[i].PropertyInfo?.GetValue(entity);
        return simpleValue ?? DBNull.Value;
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
        if (_hasTempId)
            return i == 0
                ? typeof(int)
                : Nullable.GetUnderlyingType(_properties[i - 1].ClrType) ?? _properties[i - 1].ClrType;

        return Nullable.GetUnderlyingType(_properties[i].ClrType) ?? _properties[i].ClrType;
    }

    // Los que DbDataReader requiere como abstract pero SqlBulkCopy no llama
    public override int GetOrdinal(string name)
    {
        if (_hasTempId && name == "__TempId") return 0;
        int offset = _hasTempId ? 1 : 0;
        for (int i = 0; i < _properties.Count; i++)
            if (_properties[i].GetColumnName() == name) return i + offset;
        throw new IndexOutOfRangeException($"Columna '{name}' no encontrada.");
    }

    // DbDataReader ya implementa todo lo demás con throws o valores por defecto.
    // Solo sobreescribimos lo mínimo requerido por la clase abstracta:
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

    // DbDataReader tiene estos como abstract también
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
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);
}
```

### ParentKeyResolver — for graph support

```csharp
// ParentKeyResolver.cs — in Infrastructure/Persistence/BulkInsert/
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

### Usage

```csharp
// Usage example in a use case or handler
await _unitOfWork.BeginTransactionAsync(cancellationToken);

try
{
    var entities = GenerateLargeDataset(); // List<object>
    
    var bulkInsert = new BulkInsertOperations(_dbContext);
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

For general C# conventions (syntax, usings, naming, async, DTOs), see `dotnet-csharp`.

- Never `DataAnnotations` on entities or Value Objects.
- Never `nvarchar(max)` or `varchar(max)` — always specify lengths from `Entity.Rules`.
- Never `.Update()` from EF Core — change tracker detects changes automatically.
- Always `GetByIdAsync` with tracking (for modifications) and `GetByIdAsNoTrackingAsync` without tracking (for reads).
- Always `AsNoTracking()` on read-only queries.
- Always one `IEntityTypeConfiguration<T>` per entity.
- Always `ApplyConfigurationsFromAssembly` in `OnModelCreating`.
- Always handle `DbUpdateException` in `CompleteAsync` — never let it bubble up.
- Never lazy loading — explicit includes always.
- Value Objects configured with `OwnsOne` — no `[ComplexType]` or DataAnnotations.
- **Never call `builder.ToTable()`** — the table name must be the plural of the entity name, which EF Core infers automatically from the `DbSet<T>` property name. Explicit `ToTable` calls are redundant and error-prone.
- **Never call `HasDefaultValueSql()` or `ValueGeneratedOnAdd()` for primary keys** — EF Core handles this automatically for `int` and `Guid` keys.
- **Always use `BulkInsertOperations` for batch inserts** — never loop `AddAsync()` for large datasets (1000+ rows). Use `BulkInsertAsync` with an active transaction.
- **BulkInsertOperations requires active transaction** — always call `BeginTransactionAsync` before and `CommitTransactionAsync`/`RollbackTransactionAsync` after.
