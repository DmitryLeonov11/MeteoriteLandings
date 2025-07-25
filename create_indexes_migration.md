# Database Indexes Migration

To apply the new database indexes, run the following commands:

## Create Migration
```bash
dotnet ef migrations add AddPerformanceIndexes --project MeteoriteLandings.Infrastructure --startup-project MeteoriteLandings.API
```

## Apply Migration
```bash
dotnet ef database update --project MeteoriteLandings.Infrastructure --startup-project MeteoriteLandings.API
```

## Manual SQL (if needed)
If Entity Framework migration doesn't work, you can apply these indexes manually:

```sql
-- Performance indexes for filtering queries
CREATE INDEX IF NOT EXISTS "IX_MeteoriteLandings_Year" ON "MeteoriteLandings" ("Year");
CREATE INDEX IF NOT EXISTS "IX_MeteoriteLandings_RecClass" ON "MeteoriteLandings" ("RecClass");
CREATE INDEX IF NOT EXISTS "IX_MeteoriteLandings_Name" ON "MeteoriteLandings" ("Name");

-- Composite index for common filter combinations
CREATE INDEX IF NOT EXISTS "IX_MeteoriteLandings_Year_RecClass" ON "MeteoriteLandings" ("Year", "RecClass");

-- Index for data sync operations
CREATE INDEX IF NOT EXISTS "IX_MeteoriteLandings_UpdatedAt" ON "MeteoriteLandings" ("UpdatedAt");
```

## Index Explanations

1. **IX_MeteoriteLandings_Year**: Optimizes year-based filtering (StartYear, EndYear)
2. **IX_MeteoriteLandings_RecClass**: Optimizes meteorite class filtering
3. **IX_MeteoriteLandings_Name**: Optimizes name search functionality
4. **IX_MeteoriteLandings_Year_RecClass**: Composite index for combined year+class queries
5. **IX_MeteoriteLandings_UpdatedAt**: Optimizes data synchronization queries

These indexes will significantly improve query performance, especially for:
- Year range filtering (most common operation)
- Meteorite class filtering 
- Name searches with LIKE operations
- Data sync operations tracking recent updates
