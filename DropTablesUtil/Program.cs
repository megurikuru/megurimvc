using System;
using Npgsql;

var connectionString = "Server=localhost;Port=5432;Database=meguri;Username=meguri;Password=H62nDensa4vxds";

Console.WriteLine("Connecting to database...");
await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

Console.WriteLine("Dropping all tables in public schema...");
var sql = @"
DO $$ DECLARE
    r RECORD;
BEGIN
    -- DROP all tables
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS public.' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;

    -- DROP all sequences
    FOR r IN (SELECT sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public') LOOP
        EXECUTE 'DROP SEQUENCE IF EXISTS public.' || quote_ident(r.sequence_name) || ' CASCADE';
    END LOOP;

    -- DROP all types/enums
    FOR r IN (SELECT typname FROM pg_type t JOIN pg_namespace n ON t.typnamespace = n.oid WHERE n.nspname = 'public' AND t.typtype = 'e') LOOP
        EXECUTE 'DROP TYPE IF EXISTS public.' || quote_ident(r.typname) || ' CASCADE';
    END LOOP;
END $$;
";

await using (var cmd = new NpgsqlCommand(sql, conn))
{
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("Verifying remaining tables...");
await using (var cmd = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname = 'public';", conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    var count = 0;
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"Remaining table: {reader.GetString(0)}");
        count++;
    }
    if (count == 0)
    {
        Console.WriteLine("All tables dropped successfully. Database is now clean.");
    }
}
