# Database upgrades

Place ordered, repeat-safe production upgrade scripts in this directory using names such as `V1.0.1_AddTenants.sql`.

Every successful upgrade must write one unique row to `dbo.DatabaseVersions`. Scripts must never drop the database automatically and must not depend on EF Core migrations.
