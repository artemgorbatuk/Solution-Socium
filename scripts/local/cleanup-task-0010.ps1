# One-off cleanup for task-0010 in the local Socium database:
# deletes messages without a sender and chats without participants
# (created before task-0010). Rooms are not touched.
#
# Run from repo root after migration Task0010 is applied:
#   powershell -ExecutionPolicy Bypass -File scripts/local/cleanup-task-0010.ps1

$ErrorActionPreference = 'Stop'

$container = 'socium-dev-postgres'
$database = 'Socium'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'docker not found.'
}

$sql = @'
BEGIN;
DELETE FROM "Messages" m
WHERE NOT EXISTS (SELECT 1 FROM "Senders" s WHERE s."MessageId" = m."Id");
DELETE FROM "Chats" c
WHERE NOT EXISTS (SELECT 1 FROM "Participants" p WHERE p."ChatId" = c."Id");
COMMIT;
SELECT
    (SELECT count(*) FROM "Rooms") AS rooms,
    (SELECT count(*) FROM "Chats") AS chats,
    (SELECT count(*) FROM "Messages") AS messages;
'@

$sql | docker exec -i $container psql -U postgres -d $database -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) {
    throw "Cleanup failed with exit code $LASTEXITCODE."
}

Write-Host 'Done.'
