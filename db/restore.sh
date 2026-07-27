#!/usr/bin/env bash
# Restore a Rainmaker database backup into the local SQL Server container.
#
#   ./db/restore.sh db/backups/InternDB.bak [DatabaseName]
#
# Handles the two things that trip people up on macOS:
#   1. The .bak was written on Windows, so its internal file paths are
#      D:\...\InternDB.mdf. Linux SQL Server needs MOVE ... TO /var/opt/mssql/...
#      This script reads FILELISTONLY and generates the MOVE clauses for you.
#   2. Restoring over a database that already exists needs WITH REPLACE.
#
# Prepared by Syed Taha, Multinet.

set -euo pipefail

BAK_PATH="${1:-}"
DB_NAME="${2:-InternDB}"

if [[ -z "$BAK_PATH" ]]; then
  echo "usage: $0 <path-to-.bak> [DatabaseName]" >&2
  exit 64
fi
if [[ ! -f "$BAK_PATH" ]]; then
  echo "error: backup file not found: $BAK_PATH" >&2
  exit 66
fi

# ── Locate the container and how to talk to it ────────────────────────────────
CONTAINER="${MSSQL_CONTAINER:-}"
if [[ -z "$CONTAINER" ]]; then
  for candidate in rainmaker-mssql multinet-db; do
    if docker ps --format '{{.Names}}' | grep -qx "$candidate"; then
      CONTAINER="$candidate"
      break
    fi
  done
fi
if [[ -z "$CONTAINER" ]]; then
  echo "error: no running SQL Server container found." >&2
  echo "       start one:  docker compose -f db/docker-compose.yml up -d" >&2
  echo "       or set:     export MSSQL_CONTAINER=<name>" >&2
  exit 69
fi

SA_PASSWORD="${MSSQL_SA_PASSWORD:-}"
if [[ -z "$SA_PASSWORD" ]]; then
  # Fall back to the password the container was started with.
  SA_PASSWORD="$(docker inspect "$CONTAINER" \
    --format '{{range .Config.Env}}{{println .}}{{end}}' \
    | sed -n 's/^MSSQL_SA_PASSWORD=//p' | head -1)"
fi
if [[ -z "$SA_PASSWORD" ]]; then
  echo "error: could not determine the sa password. export MSSQL_SA_PASSWORD=..." >&2
  exit 78
fi

# sqlcmd on the host (brew install sqlcmd). -N disable: the container's
# self-signed certificate has a negative serial number, which Go's strict x509
# parser rejects; this is a loopback dev connection so plaintext is acceptable.
SQL() { sqlcmd -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -N disable -b "$@"; }
if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "error: sqlcmd not found. install it:  brew install sqlcmd" >&2
  exit 69
fi

# ── Copy the backup into the container ───────────────────────────────────────
BAK_FILE="$(basename "$BAK_PATH")"
echo "▸ copying $BAK_FILE into $CONTAINER:/backups/"
docker exec "$CONTAINER" mkdir -p /backups
docker cp "$BAK_PATH" "$CONTAINER:/backups/$BAK_FILE"

# ── Read the logical file names out of the backup ─────────────────────────────
echo "▸ reading file list from the backup"
FILELIST="$(SQL -h -1 -W -s '|' -Q \
  "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'/backups/$BAK_FILE';")"

MOVE_CLAUSES=""
while IFS='|' read -r logical physical type rest; do
  [[ -z "${logical// }" ]] && continue
  case "$type" in
    D) ext="mdf" ;;
    L) ext="ldf" ;;
    *) continue ;;
  esac
  target="/var/opt/mssql/data/${logical// /_}.${ext}"
  MOVE_CLAUSES+=", MOVE N'${logical}' TO N'${target}'"
  echo "    ${type}  ${logical}  →  ${target}"
done <<< "$FILELIST"

if [[ -z "$MOVE_CLAUSES" ]]; then
  echo "error: could not parse the backup file list. Raw output:" >&2
  echo "$FILELIST" >&2
  exit 65
fi

# ── Restore ──────────────────────────────────────────────────────────────────
echo "▸ restoring as [$DB_NAME] (WITH REPLACE)"
SQL -Q "
IF DB_ID('$DB_NAME') IS NOT NULL
    ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$DB_NAME]
    FROM DISK = N'/backups/$BAK_FILE'
    WITH REPLACE, RECOVERY, STATS = 10${MOVE_CLAUSES};
IF DB_ID('$DB_NAME') IS NOT NULL
    ALTER DATABASE [$DB_NAME] SET MULTI_USER;
"

# ── Report what landed ───────────────────────────────────────────────────────
echo "▸ verifying"
SQL -Q "
USE [$DB_NAME];
SELECT
    (SELECT COUNT(*) FROM sys.tables)                                AS [tables],
    (SELECT COUNT(*) FROM sys.procedures)                            AS [procedures],
    (SELECT COUNT(*) FROM sys.views)                                 AS [views],
    (SELECT COUNT(*) FROM sys.objects WHERE type = 'FN')             AS [functions];
SELECT TOP 15 name AS [recruitment_objects]
FROM sys.objects
WHERE name LIKE '%Ruc%' OR name LIKE '%Recruitment%'
ORDER BY name;
"

echo
echo "✅ [$DB_NAME] restored into container '$CONTAINER'."
echo "   Point the API at it via appsettings.Development.json."
