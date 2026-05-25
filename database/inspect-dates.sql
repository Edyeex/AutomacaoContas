SELECT
  'users' AS table_name,
  "CreatedAt",
  "UpdatedAt"
FROM users;

SELECT
  'accounts' AS table_name,
  "CreatedAt",
  "UpdatedAt",
  "LastRunAt",
  "NextRunAt"
FROM accounts
ORDER BY "CreatedAt";

SELECT
  "Reference",
  "DueDate",
  "DownloadedAt",
  "FileName"
FROM bills
ORDER BY "DownloadedAt" DESC;

SELECT
  "StartedAt",
  "FinishedAt",
  "Status",
  "FileName"
FROM automation_runs
ORDER BY "StartedAt" DESC;
