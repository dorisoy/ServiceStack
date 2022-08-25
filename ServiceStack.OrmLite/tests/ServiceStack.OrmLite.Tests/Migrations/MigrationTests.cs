#nullable enable
using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Migrations;

public class MigrationTests : OrmLiteTestBase
{
    // public MigrateBookings() => OrmLiteUtils.PrintSql();

    IDbConnection Create()
    {
        var db = DbFactory.Open();
        Migrator.Clear(db);
        Migrator.Down(DbFactory, new[]{ typeof(Migration1000), typeof(Migration1002) });
        return db;
    }

    [Test]
    public void Runs_all_migrations()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1000), typeof(Migration1001), typeof(Migration1002) }));
    }

    [Test]
    public void Runs_only_remaining_migrations()
    {
        using var db = Create();
        Migrator.Up(DbFactory, typeof(Migration1000));

        db.Insert(new Migration {
            Name = nameof(Migration1000), 
            CreatedDate = DateTime.UtcNow, 
            CompletedDate = DateTime.UtcNow
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1001), typeof(Migration1002) }));
    }

    [Test]
    public void Runs_no_migrations_if_last_migration_has_been_run()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1002), 
            CreatedDate = DateTime.UtcNow, 
            CompletedDate = DateTime.UtcNow
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.Empty);
    }

    [Test]
    public void Runs_no_migrations_if_last_migration_has_not_completed_within_timeout()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1002), 
            CreatedDate = DateTime.UtcNow, 
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run(throwIfError:false);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.TypesCompleted, Is.Empty);
    }

    [Test]
    public void Runs_no_migrations_if_last_migration_has_not_completed_within_timeout_throwIfError()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1001), 
            CreatedDate = DateTime.UtcNow, 
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        Assert.Throws<InfoException>(() => migrator.Run(throwIfError:true));
    }

    [Test]
    public void Runs_migration_if_has_not_completed_after_timeout()
    {
        using var db = Create();
        Migrator.Up(DbFactory, new[] { typeof(Migration1000), typeof(Migration1001) });

        db.Insert(new Migration {
            Name = nameof(Migration1002), 
            CreatedDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(11)),
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly) {
            Timeout = TimeSpan.FromMinutes(10)
        };
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1002) }));
    }

    [Test]
    public void Can_run_and_revert_all_migrations()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1000), typeof(Migration1001), typeof(Migration1002) }));
        
        result = migrator.Revert(Migrator.All);
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1002), typeof(Migration1001), typeof(Migration1000) }));
    }    

    [Test]
    public void Can_run_and_revert_last_migration()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1000), typeof(Migration1001), typeof(Migration1002) }));
        
        result = migrator.Revert(Migrator.Last);
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1002) }));
    }    

    [Test]
    public void Can_run_and_revert_migration_by_name()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1000), typeof(Migration1001), typeof(Migration1002) }));
        
        result = migrator.Revert(nameof(Migration1001));
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(new[]{ typeof(Migration1001), typeof(Migration1002) }));
    }    
}